using System;
using System.Data;
using System.Data.SQLite;

namespace TempHumidityMonitor.Services
{
    public class DatabaseService
    {
        private readonly string connectionString;

        public DatabaseService(string dbPath)
        {
            connectionString = "Data Source=" + dbPath + ";Version=3;";
        }

        public void SaveReading(float t, float h, float p, int mode, bool isSim, string alarmMsg)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO sensor_data (timestamp, temperature, humidity, pressure, read_mode, is_simulated, is_alarm, alarm_msg)
                                       VALUES (@ts, @t, @h, @p, @mode, @sim, @alarm, @msg)";
                    cmd.Parameters.AddWithValue("@ts", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@t", t);
                    cmd.Parameters.AddWithValue("@h", h);
                    cmd.Parameters.AddWithValue("@p", p);
                    cmd.Parameters.AddWithValue("@mode", mode.ToString());
                    cmd.Parameters.AddWithValue("@sim", isSim ? 1 : 0);
                    cmd.Parameters.AddWithValue("@alarm", string.IsNullOrEmpty(alarmMsg) ? 0 : 1);
                    cmd.Parameters.AddWithValue("@msg", alarmMsg);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public DataTable QueryHistory(DateTime start, DateTime end, bool alarmOnly, int limit = 2000)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    string whereAlarm = alarmOnly ? " AND is_alarm=1" : "";
                    cmd.CommandText = @"SELECT timestamp as 时间, temperature as 温度, humidity as 湿度,
                        pressure as 气压, CASE WHEN is_simulated=1 THEN '模拟' ELSE '实时' END as 来源,
                        CASE WHEN is_alarm=1 THEN alarm_msg ELSE '' END as 报警信息
                        FROM sensor_data WHERE timestamp BETWEEN @s AND @e" + whereAlarm + " ORDER BY timestamp DESC LIMIT " + limit;
                    cmd.Parameters.AddWithValue("@s", start.ToString("yyyy-MM-dd") + " 00:00:00");
                    cmd.Parameters.AddWithValue("@e", end.ToString("yyyy-MM-dd") + " 23:59:59");
                    var dt = new DataTable();
                    using (var adapter = new SQLiteDataAdapter(cmd))
                        adapter.Fill(dt);
                    return dt;
                }
            }
        }

        public void CleanOldData(int retainDays)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM sensor_data WHERE timestamp < @cutoff";
                    cmd.Parameters.AddWithValue("@cutoff", DateTime.Now.AddDays(-retainDays).ToString("yyyy-MM-dd"));
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
