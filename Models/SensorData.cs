using System;

namespace TempHumidityMonitor.Models
{
    public class SensorData
    {
        public float Temperature { get; set; }
        public float Humidity { get; set; }
        public float Pressure { get; set; }
        public DateTime Timestamp { get; set; }

        public SensorData() { }

        public SensorData(float t, float h, float p)
        {
            Temperature = t;
            Humidity = h;
            Pressure = p;
            Timestamp = DateTime.Now;
        }
    }
}
