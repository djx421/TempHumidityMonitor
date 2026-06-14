using System;
using System.IO;
using System.Text;

namespace TempHumidityMonitor.Services
{
    public class LogService
    {
        private readonly string logDir;
        private string currentLogFile;

        public LogService(string baseDir)
        {
            logDir = Path.Combine(baseDir, "DataLog");
            if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
            currentLogFile = Path.Combine(logDir, string.Format("data_{0:yyyyMMdd}.csv", DateTime.Now));
        }

        public void LogData(float t, float h, float p)
        {
            try
            {
                string f = Path.Combine(logDir, string.Format("data_{0:yyyyMMdd}.csv", DateTime.Now));
                bool newFile = f != currentLogFile;
                currentLogFile = f;
                using (StreamWriter sw = new StreamWriter(currentLogFile, true, Encoding.UTF8))
                {
                    if (newFile || new FileInfo(currentLogFile).Length == 0)
                        sw.WriteLine("时间,温度(℃),湿度(%),气压(kPa)");
                    sw.WriteLine(string.Format("{0:yyyy-MM-dd HH:mm:ss},{1:F1},{2:F1},{3:F1}", DateTime.Now, t, h, p));
                }
            }
            catch { }
        }

        public void LogAlarm(string msg)
        {
            try
            {
                File.AppendAllText(Path.Combine(logDir, string.Format("alarm_{0:yyyyMMdd}.log", DateTime.Now)),
                    string.Format("[{0:HH:mm:ss}] {1}\n", DateTime.Now, msg), Encoding.UTF8);
            }
            catch { }
        }

        public void LogError(string msg)
        {
            try
            {
                File.AppendAllText(Path.Combine(logDir, string.Format("error_{0:yyyyMMdd}.log", DateTime.Now)),
                    string.Format("[{0:HH:mm:ss}] {1}\n", DateTime.Now, msg), Encoding.UTF8);
            }
            catch { }
        }
    }
}
