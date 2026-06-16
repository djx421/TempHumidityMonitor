using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace TempHumidityMonitor.Services
{
    /// <summary>
    /// 嵌入式 HTTP API 服务，提供 REST 接口 + SSE 实时数据推送。
    /// 运行在后台线程，与 MainForm 共享内存读取最新传感器数据。
    /// </summary>
    public class ApiService
    {
        // ==================== 共享状态（MainForm 写入，ApiService 读取） ====================
        private static readonly object _lock = new object();

        public static float LatestTemp = 0;
        public static float LatestHumi = 0;
        public static float LatestPressure = 101.3f;
        public static DateTime LatestTimestamp = DateTime.MinValue;
        public static bool IsSimMode = false;
        public static bool IsComOpen = false;
        public static bool IsReading = false;
        public static bool IsReconnecting = false;
        public static string StatusText = "就绪";
        public static string LastAlarmMsg = "";
        public static int AlarmCount = 0;

        // 报警阈值
        public static float AlarmTempH = 40, AlarmTempL = 0;
        public static float AlarmHumiH = 80, AlarmHumiL = 20;
        public static float AlarmPressH = 110, AlarmPressL = 90;
        public static bool AlarmEnabled = false;

        // 串口通讯计数
        public static int SendCount = 0;
        public static int RecvCount = 0;
        public static int ErrorCount = 0;

        // 统计数据（MainForm 推送）
        public static int DataCount = 0;
        public static float TempMin = float.MaxValue, TempMax = float.MinValue, TempSum = 0;
        public static float HumiMin = float.MaxValue, HumiMax = float.MinValue, HumiSum = 0;
        public static float PressMin = float.MaxValue, PressMax = float.MinValue, PressSum = 0;

        // ==================== 实例字段 ====================
        private HttpListener _listener;
        private Thread _serverThread;
        private int _port;
        private string _webRootPath;
        private DatabaseService _dbService;
        private volatile bool _running;

        // SSE 客户端管理
        private readonly List<SSEClient> _sseClients = new List<SSEClient>();
        private Timer _heartbeatTimer;

        private class SSEClient
        {
            public StreamWriter Writer;
            public HttpListenerResponse Response;
        }

        // ==================== JSON 工具（无外部依赖） ====================
        internal static string JsonEscape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "\"\"";
            var sb = new StringBuilder(s.Length + 2);
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(c); break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        private static string F(float v) => v.ToString("F1");

        // ==================== 构造 & 生命周期 ====================
        public ApiService(int port, string webRootPath, DatabaseService dbService)
        {
            _port = port;
            _webRootPath = webRootPath;
            _dbService = dbService;
        }

        public void Start()
        {
            if (_running) return;
            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add(string.Format("http://localhost:{0}/", _port));
                _listener.Start();
                _running = true;

                // 订阅数据推送事件
                OnDataUpdated += BroadcastToSSEClients;

                _serverThread = new Thread(ServerLoop)
                {
                    IsBackground = true,
                    Name = "ApiService"
                };
                _serverThread.Start();

                // SSE 心跳：每 15 秒发送一次
                _heartbeatTimer = new Timer(_ => SendHeartbeat(), null, 15000, 15000);
            }
            catch (Exception)
            {
                _running = false;
                throw; // 传播到 MainForm，由 UI 显示错误
            }
        }

        public void Shutdown()
        {
            _running = false;
            try
            {
                // 取消事件订阅
                OnDataUpdated -= BroadcastToSSEClients;

                _heartbeatTimer?.Dispose();

                // 关闭所有 SSE 连接
                lock (_sseClients)
                {
                    foreach (var c in _sseClients)
                    {
                        try { c.Writer.Close(); } catch { }
                    }
                    _sseClients.Clear();
                }
                _listener?.Stop();
                _listener?.Close();
            }
            catch { }
        }

        // ==================== 主循环 ====================
        private void ServerLoop()
        {
            while (_running)
            {
                try
                {
                    var context = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => HandleRequest(context));
                }
                catch (HttpListenerException)
                {
                    break; // 监听器已停止
                }
                catch
                {
                    // 忽略其他异常，保持服务器运行
                }
            }
        }

        // ==================== 请求路由 ====================
        private void HandleRequest(HttpListenerContext ctx)
        {
            try
            {
                string path = ctx.Request.Url.AbsolutePath;
                string method = ctx.Request.HttpMethod;

                if (method == "OPTIONS")
                {
                    SetCORS(ctx.Response);
                    ctx.Response.StatusCode = 204;
                    ctx.Response.Close();
                    return;
                }

                SetCORS(ctx.Response);

                if (path == "/" || path == "/index.html")
                    ServeStaticFile(ctx);
                else if (path == "/api/current")
                    HandleCurrent(ctx);
                else if (path == "/api/history")
                    HandleHistory(ctx);
                else if (path == "/api/alarms")
                    HandleAlarms(ctx);
                else if (path == "/api/status")
                    HandleStatus(ctx);
                else if (path == "/api/stats")
                    HandleStats(ctx);
                else if (path == "/api/thresholds")
                    HandleThresholds(ctx);
                else if (path == "/api/stream")
                    HandleSSE(ctx);
                else
                {
                    ctx.Response.StatusCode = 404;
                    RespondJson(ctx, "{\"error\":\"Not Found\"}");
                }
            }
            catch
            {
                try { ctx.Response.StatusCode = 500; RespondJson(ctx, "{\"error\":\"Internal Server Error\"}"); }
                catch { }
            }
        }

        private void SetCORS(HttpListenerResponse resp)
        {
            resp.Headers.Add("Access-Control-Allow-Origin", "*");
            resp.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            resp.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
        }

        // ==================== 路由处理 ====================

        private void ServeStaticFile(HttpListenerContext ctx)
        {
            string filePath = Path.GetFullPath(Path.Combine(_webRootPath, "index.html"));
            if (!File.Exists(filePath))
            {
                // 开发回退：从 bin/Debug 向上找项目源码目录
                string alt = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\web\\index.html"));
                if (File.Exists(alt)) filePath = alt;
            }
            if (!File.Exists(filePath))
            {
                ctx.Response.StatusCode = 404;
                var sb = new StringBuilder("<h1>404 - web/index.html not found</h1>");
                sb.Append("<p>webRoot: ").Append(_webRootPath).Append("</p>");
                sb.Append("<p>resolved: ").Append(Path.GetFullPath(Path.Combine(_webRootPath, "index.html"))).Append("</p>");
                RespondText(ctx, sb.ToString(), "text/html; charset=utf-8");
                return;
            }
            string html = File.ReadAllText(filePath, Encoding.UTF8);
            RespondText(ctx, html, "text/html; charset=utf-8");
        }

        private void HandleCurrent(HttpListenerContext ctx)
        {
            float t, h, p;
            bool sim, reading, recon;
            string status;
            DateTime ts;
            lock (_lock)
            {
                t = LatestTemp; h = LatestHumi; p = LatestPressure;
                ts = LatestTimestamp;
                sim = IsSimMode; reading = IsReading; recon = IsReconnecting;
                status = StatusText;
            }

            string mode = sim ? "simulation" : (recon ? "reconnecting" : "hardware");
            string json = string.Format(
                "{{\"temperature\":{0},\"humidity\":{1},\"pressure\":{2},\"timestamp\":{3},\"isSimulated\":{4},\"isReading\":{5},\"mode\":{6},\"statusText\":{7}}}",
                F(t), F(h), F(p),
                JsonEscape(ts == DateTime.MinValue ? DateTime.Now.ToString("o") : ts.ToString("o")),
                sim ? "true" : "false",
                reading ? "true" : "false",
                JsonEscape(mode),
                JsonEscape(status)
            );
            RespondJson(ctx, json);
        }

        private void HandleHistory(HttpListenerContext ctx)
        {
            var qs = ctx.Request.QueryString;
            string startStr = qs["start"];
            string endStr = qs["end"];
            string alarmOnlyStr = qs["alarmOnly"];

            DateTime start, end;
            if (!DateTime.TryParse(startStr, out start)) start = DateTime.Today;
            if (!DateTime.TryParse(endStr, out end)) end = DateTime.Now;
            bool alarmOnly = alarmOnlyStr == "1" || alarmOnlyStr == "true";

            var dt = _dbService.QueryHistory(start, end, alarmOnly, 5000);

            var sb = new StringBuilder();
            sb.Append("[");
            bool first = true;
            foreach (System.Data.DataRow row in dt.Rows)
            {
                if (!first) sb.Append(",");
                first = false;

                string time = row[0].ToString();
                string temp = row[1].ToString();
                string humi = row[2].ToString();
                string press = row[3].ToString();
                string source = row[4].ToString();
                string alarm = row[5].ToString();

                sb.Append("{");
                sb.Append("\"timestamp\":"); sb.Append(JsonEscape(time));
                sb.Append(",\"temperature\":"); sb.Append(temp);
                sb.Append(",\"humidity\":"); sb.Append(humi);
                sb.Append(",\"pressure\":"); sb.Append(press);
                sb.Append(",\"source\":"); sb.Append(JsonEscape(source));
                sb.Append(",\"alarmMsg\":"); sb.Append(JsonEscape(alarm ?? ""));
                sb.Append(",\"isAlarm\":"); sb.Append(string.IsNullOrEmpty(alarm) ? "false" : "true");
                sb.Append("}");
            }
            sb.Append("]");
            RespondJson(ctx, sb.ToString());
        }

        private void HandleAlarms(HttpListenerContext ctx)
        {
            float t, h, p;
            bool alarmEnabled;
            float th, tl, hh, hl, ph, pl;
            lock (_lock)
            {
                t = LatestTemp; h = LatestHumi; p = LatestPressure;
                alarmEnabled = AlarmEnabled;
                th = AlarmTempH; tl = AlarmTempL;
                hh = AlarmHumiH; hl = AlarmHumiL;
                ph = AlarmPressH; pl = AlarmPressL;
            }

            bool tempHigh = alarmEnabled && t > th;
            bool tempLow = alarmEnabled && t < tl;
            bool humiHigh = alarmEnabled && h > hh;
            bool humiLow = alarmEnabled && h < hl;
            bool pressHigh = alarmEnabled && p > ph;
            bool pressLow = alarmEnabled && p < pl;
            bool any = tempHigh || tempLow || humiHigh || humiLow || pressHigh || pressLow;

            string json = string.Format(
                "{{\"tempHigh\":{0},\"tempLow\":{1},\"humiHigh\":{2},\"humiLow\":{3},\"pressHigh\":{4},\"pressLow\":{5},\"any\":{6}}}",
                tempHigh ? "true" : "false", tempLow ? "true" : "false",
                humiHigh ? "true" : "false", humiLow ? "true" : "false",
                pressHigh ? "true" : "false", pressLow ? "true" : "false",
                any ? "true" : "false"
            );
            RespondJson(ctx, json);
        }

        private void HandleStatus(HttpListenerContext ctx)
        {
            bool comOpen, sim, reading, recon;
            string status;
            int alarmCount, send, recv, err;
            lock (_lock)
            {
                comOpen = IsComOpen; sim = IsSimMode;
                reading = IsReading; recon = IsReconnecting;
                status = StatusText; alarmCount = AlarmCount;
                send = SendCount; recv = RecvCount; err = ErrorCount;
            }

            string mode = sim ? "simulation" : (comOpen ? "online" : (recon ? "reconnecting" : "offline"));
            string json = string.Format(
                "{{\"connected\":{0},\"mode\":{1},\"isReading\":{2},\"isReconnecting\":{3},\"statusText\":{4},\"alarmCount\":{5},\"sendCount\":{6},\"recvCount\":{7},\"errorCount\":{8}}}",
                (comOpen || sim) ? "true" : "false",
                JsonEscape(mode),
                reading ? "true" : "false",
                recon ? "true" : "false",
                JsonEscape(status),
                alarmCount,
                send, recv, err
            );
            RespondJson(ctx, json);
        }

        private void HandleStats(HttpListenerContext ctx)
        {
            int count;
            float tMin, tMax, tSum, hMin, hMax, hSum, pMin, pMax, pSum;
            lock (_lock)
            {
                count = DataCount;
                tMin = TempMin; tMax = TempMax; tSum = TempSum;
                hMin = HumiMin; hMax = HumiMax; hSum = HumiSum;
                pMin = PressMin; pMax = PressMax; pSum = PressSum;
            }
            float tAvg = count > 0 ? tSum / count : 0;
            float hAvg = count > 0 ? hSum / count : 0;
            float pAvg = count > 0 ? pSum / count : 0;
            string json = string.Format(
                "{{\"count\":{0},\"tempMin\":{1},\"tempMax\":{2},\"tempAvg\":{3},\"humiMin\":{4},\"humiMax\":{5},\"humiAvg\":{6},\"pressMin\":{7},\"pressMax\":{8},\"pressAvg\":{9}}}",
                count, F(tMin == float.MaxValue ? 0 : tMin), F(tMax == float.MinValue ? 0 : tMax), F(tAvg),
                F(hMin == float.MaxValue ? 0 : hMin), F(hMax == float.MinValue ? 0 : hMax), F(hAvg),
                F(pMin == float.MaxValue ? 0 : pMin), F(pMax == float.MinValue ? 0 : pMax), F(pAvg)
            );
            RespondJson(ctx, json);
        }

        private void HandleThresholds(HttpListenerContext ctx)
        {
            float th, tl, hh, hl, ph, pl;
            bool alarmEnabled;
            lock (_lock)
            {
                th = AlarmTempH; tl = AlarmTempL;
                hh = AlarmHumiH; hl = AlarmHumiL;
                ph = AlarmPressH; pl = AlarmPressL;
                alarmEnabled = AlarmEnabled;
            }

            string json = string.Format(
                "{{\"tempHi\":{0},\"tempLo\":{1},\"humiHi\":{2},\"humiLo\":{3},\"pressHi\":{4},\"pressLo\":{5},\"alarmEnabled\":{6}}}",
                F(th), F(tl), F(hh), F(hl), F(ph), F(pl),
                alarmEnabled ? "true" : "false"
            );
            RespondJson(ctx, json);
        }

        // ==================== SSE（Server-Sent Events） ====================
        private void HandleSSE(HttpListenerContext ctx)
        {
            var resp = ctx.Response;
            resp.Headers.Add("Content-Type", "text/event-stream; charset=utf-8");
            resp.Headers.Add("Cache-Control", "no-cache");
            resp.Headers.Add("Connection", "keep-alive");
            resp.Headers.Add("Access-Control-Allow-Origin", "*");

            var client = new SSEClient
            {
                Response = resp,
                Writer = new StreamWriter(resp.OutputStream, Encoding.UTF8) { AutoFlush = true }
            };

            lock (_sseClients)
                _sseClients.Add(client);

            try
            {
                // 发送初始连接确认
                client.Writer.Write("event: connected\ndata: {\"status\":\"ok\"}\n\n");

                // 发送当前最新数据
                SendCurrentToClient(client);

                // 保持连接，直到客户端断开
                while (_running)
                {
                    try { client.Writer.Write(": keepalive\n\n"); }
                    catch { break; }
                    Thread.Sleep(15000);
                }
            }
            catch { }
            finally
            {
                lock (_sseClients)
                    _sseClients.Remove(client);
                try { client.Writer.Close(); } catch { }
                try { resp.Close(); } catch { }
            }
        }

        private void SendCurrentToClient(SSEClient client)
        {
            try
            {
                float t, h, p;
                DateTime ts;
                bool sim;
                lock (_lock)
                {
                    t = LatestTemp; h = LatestHumi; p = LatestPressure;
                    ts = LatestTimestamp; sim = IsSimMode;
                }
                string data = string.Format(
                    "{{\"temperature\":{0},\"humidity\":{1},\"pressure\":{2},\"timestamp\":{3},\"isSimulated\":{4}}}",
                    F(t), F(h), F(p),
                    JsonEscape(ts == DateTime.MinValue ? DateTime.Now.ToString("o") : ts.ToString("o")),
                    sim ? "true" : "false"
                );
                client.Writer.Write("event: data\ndata: " + data + "\n\n");
            }
            catch { }
        }

        private void SendHeartbeat()
        {
            string beat = ": heartbeat\n\n";
            lock (_sseClients)
            {
                for (int i = _sseClients.Count - 1; i >= 0; i--)
                {
                    try { _sseClients[i].Writer.Write(beat); }
                    catch
                    {
                        try { _sseClients[i].Writer.Close(); } catch { }
                        _sseClients.RemoveAt(i);
                    }
                }
            }
        }

        // ==================== 公共推送方法（MainForm 调用） ====================
        /// <summary>
        /// 当有新传感器数据时由 MainForm 调用，推送给所有 SSE 客户端。
        /// </summary>
        public static void BroadcastSensorData(float temp, float humi, float pressure, bool isSim)
        {
            lock (_lock)
            {
                LatestTemp = temp;
                LatestHumi = humi;
                LatestPressure = pressure;
                LatestTimestamp = DateTime.Now;
                IsSimMode = isSim;
            }
            OnDataUpdated?.Invoke(temp, humi, pressure, isSim);
        }

        public static void IncrementAlarmCount()
        {
            lock (_lock) { AlarmCount++; }
        }

        public static void UpdateStatus(bool comOpen, bool simMode, bool reading, bool reconnecting, string statusText)
        {
            lock (_lock)
            {
                IsComOpen = comOpen;
                IsSimMode = simMode;
                IsReading = reading;
                IsReconnecting = reconnecting;
                StatusText = statusText;
            }
        }

        public static void UpdateThresholds(float th, float tl, float hh, float hl, float ph, float pl, bool alarmEnabled)
        {
            lock (_lock)
            {
                AlarmTempH = th; AlarmTempL = tl;
                AlarmHumiH = hh; AlarmHumiL = hl;
                AlarmPressH = ph; AlarmPressL = pl;
                AlarmEnabled = alarmEnabled;
            }
        }

        public static void UpdateAlarmMsg(string msg)
        {
            lock (_lock) { LastAlarmMsg = msg; }
        }

        public static void UpdateCounters(int send, int recv, int err)
        {
            lock (_lock) { SendCount = send; RecvCount = recv; ErrorCount = err; }
        }

        public static void UpdateStats(
            int count, float tMin, float tMax, float tSum,
            float hMin, float hMax, float hSum,
            float pMin, float pMax, float pSum)
        {
            lock (_lock)
            {
                DataCount = count;
                TempMin = tMin; TempMax = tMax; TempSum = tSum;
                HumiMin = hMin; HumiMax = hMax; HumiSum = hSum;
                PressMin = pMin; PressMax = pMax; PressSum = pSum;
            }
        }

        // ==================== 静态事件（实例订阅以推送 SSE） ====================
        private static event Action<float, float, float, bool> OnDataUpdated;

        private void BroadcastToSSEClients(float temp, float humi, float pressure, bool isSim)
        {
            string data = string.Format(
                "{{\"temperature\":{0},\"humidity\":{1},\"pressure\":{2},\"timestamp\":{3},\"isSimulated\":{4}}}",
                F(temp), F(humi), F(pressure),
                JsonEscape(DateTime.Now.ToString("o")),
                isSim ? "true" : "false"
            );
            string payload = "event: data\ndata: " + data + "\n\n";

            lock (_sseClients)
            {
                for (int i = _sseClients.Count - 1; i >= 0; i--)
                {
                    try { _sseClients[i].Writer.Write(payload); }
                    catch
                    {
                        try { _sseClients[i].Writer.Close(); } catch { }
                        _sseClients.RemoveAt(i);
                    }
                }
            }
        }

        // ==================== 云端推送数据快照 ====================
        /// <summary>
        /// 供 CloudPushService 调用的线程安全数据快照
        /// </summary>
        public struct Snapshot
        {
            public float Temp, Humi, Pressure;
            public DateTime Timestamp;
            public bool IsSim, IsReading, IsReconnecting;
            public string StatusText;
            public int SendCount, RecvCount, ErrorCount;
        }

        public static Snapshot GetSnapshot()
        {
            lock (_lock)
            {
                return new Snapshot
                {
                    Temp = LatestTemp, Humi = LatestHumi, Pressure = LatestPressure,
                    Timestamp = LatestTimestamp,
                    IsSim = IsSimMode, IsReading = IsReading, IsReconnecting = IsReconnecting,
                    StatusText = StatusText ?? "",
                    SendCount = SendCount, RecvCount = RecvCount, ErrorCount = ErrorCount
                };
            }
        }

        // ==================== 响应工具 ====================
        private void RespondJson(HttpListenerContext ctx, string json)
        {
            ctx.Response.ContentType = "application/json; charset=utf-8";
            byte[] data = Encoding.UTF8.GetBytes(json);
            ctx.Response.ContentLength64 = data.Length;
            ctx.Response.OutputStream.Write(data, 0, data.Length);
            ctx.Response.Close();
        }

        private void RespondText(HttpListenerContext ctx, string text, string contentType)
        {
            ctx.Response.ContentType = contentType;
            byte[] data = Encoding.UTF8.GetBytes(text);
            ctx.Response.ContentLength64 = data.Length;
            ctx.Response.OutputStream.Write(data, 0, data.Length);
            ctx.Response.Close();
        }
    }
}
