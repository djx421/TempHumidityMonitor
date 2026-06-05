using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TempHumidityMonitor.Models;

namespace TempHumidityMonitor.Services
{
    public static class ModbusService
    {
        public static Dictionary<int, byte[]> Commands { get; private set; }

        static ModbusService()
        {
            Commands = new Dictionary<int, byte[]>();
        }

        public static void LoadConfig(string configPath)
        {
            Commands = new Dictionary<int, byte[]>();
            try
            {
                if (File.Exists(configPath))
                {
                    string[] lines = File.ReadAllLines(configPath, Encoding.UTF8);
                    int idx = -1;
                    foreach (string line in lines)
                    {
                        if (line.Contains("\"index\""))
                            int.TryParse(System.Text.RegularExpressions.Regex.Match(line, @"\d+").Value, out idx);
                        else if (idx >= 0 && line.Contains("\"bytes\""))
                        {
                            string hex = System.Text.RegularExpressions.Regex.Match(line, @"[0-9A-Fa-f, ]+").Value;
                            var bytes = hex.Split(',').Select(s => byte.Parse(s.Trim(), System.Globalization.NumberStyles.HexNumber)).ToArray();
                            Commands[idx] = bytes;
                            idx = -1;
                        }
                    }
                }
            }
            catch { }
            if (Commands.Count == 0)
                FillDefaults();
        }

        public static void FillDefaults()
        {
            Commands[0] = new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x04, 0x44, 0x09 };
            Commands[1] = new byte[] { 0x01, 0x03, 0x00, 0x80, 0x00, 0x04, 0x45, 0xE1 };
            Commands[2] = new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x02, 0xC4, 0x0B };
            Commands[3] = new byte[] { 0x01, 0x03, 0x00, 0x02, 0x00, 0x02, 0x65, 0xCB };
            Commands[4] = new byte[] { 0x01, 0x03, 0x00, 0x80, 0x00, 0x01, 0x85, 0xE2 };
            Commands[5] = new byte[] { 0x01, 0x03, 0x00, 0x81, 0x00, 0x01, 0xD4, 0x22 };
            Commands[6] = new byte[] { 0x01, 0x03, 0x00, 0x04, 0x00, 0x02, 0x85, 0xCA };
            Commands[7] = new byte[] { 0x01, 0x03, 0x00, 0x82, 0x00, 0x01, 0x24, 0x22 };
            Commands[8] = new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x06, 0xC5, 0xC8 };
            Commands[9] = new byte[] { 0x01, 0x03, 0x00, 0x80, 0x00, 0x04, 0x45, 0xE1 };
        }

        public static byte[] GetCommand(int modeIndex)
        {
            if (Commands.TryGetValue(modeIndex, out var cmd)) return cmd;
            return Commands[0];
        }

        public static byte[] CalcCRC(byte[] data)
        {
            uint crc = 0xffff;
            for (int i = 0; i < data.Length; i++)
            {
                crc ^= (uint)data[i] & 0x00ff;
                for (int j = 0; j < 8; j++)
                {
                    uint flag = crc & 0x01;
                    crc >>= 1;
                    if (flag != 0) crc ^= 0xA001;
                }
            }
            return BitConverter.GetBytes(crc);
        }

        public static bool CheckCRC(byte[] frame)
        {
            if (frame.Length < 2) return false;
            byte[] d = new byte[frame.Length - 2];
            Array.Copy(frame, d, d.Length);
            byte[] c = CalcCRC(d);
            return c[0] == frame[frame.Length - 2] && c[1] == frame[frame.Length - 1];
        }

        public static bool CheckFrame(byte[] frame)
        {
            if (frame == null || frame.Length < 5) return false;
            if (frame[0] != 0x01 || frame[1] != 0x03) return false;
            if (frame.Length != frame[2] + 5) return false;
            return CheckCRC(frame);
        }

        public static SensorData ParseSensorData(byte[] frame, int modeIndex, float lastT, float lastH, float lastP)
        {
            float t = lastT, h = lastH, p = lastP;
            switch (modeIndex)
            {
                case 0:
                    { byte[] a = { frame[6], frame[5], frame[4], frame[3] }, b = { frame[10], frame[9], frame[8], frame[7] }; t = BitConverter.ToSingle(a, 0); h = BitConverter.ToSingle(b, 0); }
                    break;
                case 1:
                    { t = ((frame[3] << 8) | frame[4]) / 10.0f; h = ((frame[5] << 8) | frame[6]) / 10.0f; }
                    break;
                case 2:
                    { byte[] a = { frame[6], frame[5], frame[4], frame[3] }; t = BitConverter.ToSingle(a, 0); }
                    break;
                case 3:
                    { byte[] b = { frame[6], frame[5], frame[4], frame[3] }; h = BitConverter.ToSingle(b, 0); }
                    break;
                case 4:
                    { t = ((frame[3] << 8) | frame[4]) / 10.0f; }
                    break;
                case 5:
                    { h = ((frame[3] << 8) | frame[4]) / 10.0f; }
                    break;
                case 6:
                    { byte[] c = { frame[6], frame[5], frame[4], frame[3] }; p = BitConverter.ToSingle(c, 0); }
                    break;
                case 7:
                    { p = ((frame[3] << 8) | frame[4]); }
                    break;
                case 8:
                    { byte[] a = { frame[6], frame[5], frame[4], frame[3] }, b = { frame[10], frame[9], frame[8], frame[7] }, c = { frame[14], frame[13], frame[12], frame[11] }; t = BitConverter.ToSingle(a, 0); h = BitConverter.ToSingle(b, 0); p = BitConverter.ToSingle(c, 0); }
                    break;
                case 9:
                    { t = ((frame[3] << 8) | frame[4]) / 10.0f; h = ((frame[5] << 8) | frame[6]) / 10.0f; p = ((frame[7] << 8) | frame[8]); }
                    break;
            }
            return new SensorData(t, h, p);
        }
    }
}
