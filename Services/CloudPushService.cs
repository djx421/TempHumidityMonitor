using System;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace TempHumidityMonitor.Services
{
    /// <summary>
    /// 云端数据推送服务 — 定时将传感器数据 POST 到云服务器。
    /// 推送间隔默认为3秒，与传感器采集频率保持一致。
    /// </summary>
    public class CloudPushService
    {
        private static readonly HttpClient _client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        private static Timer _timer;
        private static string _cloudUrl;
        private static bool _running;
        private static readonly object _lock = new object();

        /// <summary>云端推送间隔（秒）</summary>
        public static int PushIntervalSec = 3;

        /// <summary>是否启用云端推送</summary>
        public static bool Enabled = false;

        /// <summary>最近一次推送是否成功</summary>
        public static bool LastPushSuccess = false;

        /// <summary>最近错误信息</summary>
        public static string LastError = "";

        /// <summary>
        /// 启动云端推送服务
        /// </summary>
        /// <param name="cloudServerUrl">云服务器 URL，如 http://47.116.50.90:5000</param>
        public static void Start(string cloudServerUrl)
        {
            lock (_lock)
            {
                if (_running) return;
                _cloudUrl = cloudServerUrl.TrimEnd('/') + "/api/data";
                _running = true;
                Enabled = true;

                _timer = new Timer(_ => PushData(), null, 1000, PushIntervalSec * 1000);
                LogService.Info($"[CloudPush] 已启动, 目标: {_cloudUrl}, 间隔: {PushIntervalSec}s");
            }
        }

        /// <summary>
        /// 停止云端推送
        /// </summary>
        public static void Stop()
        {
            lock (_lock)
            {
                _running = false;
                Enabled = false;
                _timer?.Dispose();
                _timer = null;
                LogService.Info("[CloudPush] 已停止");
            }
        }

        /// <summary>
        /// 立即推送一次（手动触发）
        /// </summary>
        public static void PushOnce()
        {
            PushData();
        }

        private static async void PushData()
        {
            if (!_running || string.IsNullOrEmpty(_cloudUrl)) return;

            try
            {
                // 从 ApiService 获取线程安全的数据快照
                var snap = ApiService.GetSnapshot();

                // 如果没有有效数据则跳过
                if (snap.Temp == 0 && snap.Humi == 0 && snap.Timestamp == DateTime.MinValue) return;

                string timestamp = snap.Timestamp == DateTime.MinValue
                    ? DateTime.Now.ToString("o")
                    : snap.Timestamp.ToString("o");

                string json = string.Format(
                    "{{\"temperature\":{0},\"humidity\":{1},\"pressure\":{2}," +
                    "\"timestamp\":{3},\"isSimulated\":{4},\"isReading\":{5}," +
                    "\"mode\":{6},\"statusText\":{7}," +
                    "\"sendCount\":{8},\"recvCount\":{9},\"errorCount\":{10}}}",
                    snap.Temp.ToString("F1"), snap.Humi.ToString("F1"), snap.Pressure.ToString("F1"),
                    ApiService.JsonEscape(timestamp),
                    snap.IsSim ? "true" : "false",
                    snap.IsReading ? "true" : "false",
                    ApiService.JsonEscape(snap.IsSim ? "simulation" : (snap.IsReconnecting ? "reconnecting" : "hardware")),
                    ApiService.JsonEscape(snap.StatusText),
                    snap.SendCount, snap.RecvCount, snap.ErrorCount
                );

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _client.PostAsync(_cloudUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    LastPushSuccess = true;
                    LastError = "";
                }
                else
                {
                    LastPushSuccess = false;
                    LastError = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
                    LogService.Warn($"[CloudPush] 推送失败: {LastError}");
                }
            }
            catch (Exception ex)
            {
                LastPushSuccess = false;
                LastError = ex.Message;
                LogService.Warn($"[CloudPush] 推送异常: {ex.Message}");
            }
        }
    }
}
