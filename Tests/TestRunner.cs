using System;

namespace TempHumidityMonitor.Tests
{
    class TestRunner
    {
        static int passed = 0, failed = 0;

        static void Assert(bool condition, string msg)
        {
            if (condition) { passed++; Console.WriteLine("  [PASS] " + msg); }
            else { failed++; Console.WriteLine("  [FAIL] " + msg); }
        }

        public static void Main()
        {
            Console.WriteLine("=== MODBUS CRC 测试 ===");
            Test_CRC_KnownVector();
            Test_CRC_StandardVector();
            Test_CheckCRC_Valid();
            Test_CheckCRC_Corrupted();
            Test_CheckData_InvalidHeader();
            Test_CheckData_WrongLength();
            Console.WriteLine("\n=== 命令生成测试 ===");
            Test_AllCommands_Valid();
            Console.WriteLine("\n=== 数据解析测试 ===");
            Test_ParseTemp_Float();
            Test_ParseTemp_Int();
            Console.WriteLine("\n=== 结果: {0} 通过, {1} 失败 ===", passed, failed);
            Environment.Exit(failed > 0 ? 1 : 0);
        }

        static byte[] CalcCRC(byte[] p)
        {
            uint crc = 0xffff;
            for (int i = 0; i < p.Length; i++)
            {
                crc ^= (uint)p[i] & 0x00ff;
                for (int j = 0; j < 8; j++)
                {
                    uint flag = crc & 0x01;
                    crc >>= 1;
                    if (flag != 0) crc ^= 0xA001;
                }
            }
            return BitConverter.GetBytes(crc);
        }

        static bool CheckCRC(byte[] buf)
        {
            if (buf.Length < 2) return false;
            byte[] d = new byte[buf.Length - 2];
            Array.Copy(buf, d, d.Length);
            byte[] c = CalcCRC(d);
            return c[0] == buf[buf.Length - 2] && c[1] == buf[buf.Length - 1];
        }

        static bool CheckData(byte[] buf)
        {
            if (buf == null || buf.Length < 5) return false;
            if (buf[0] != 0x01 || buf[1] != 0x03) return false;
            if (buf.Length != buf[2] + 5) return false;
            return CheckCRC(buf);
        }

        static void Test_CRC_KnownVector()
        {
            byte[] data = { 0x01, 0x03, 0x00, 0x00, 0x00, 0x04 };
            byte[] crc = CalcCRC(data);
            Assert(crc[0] == 0x44 && crc[1] == 0x09, "CRC-16: 01 03 00 00 00 04 → 44 09");
        }

        static void Test_CRC_StandardVector()
        {
            byte[] data = { 0x01, 0x03, 0x04, 0x00, 0x00, 0x00, 0x00 };
            byte[] crc = CalcCRC(data);
            Assert((crc[0] & 0xFF) == 0xFA && (crc[1] & 0xFF) == 0x33, "CRC-16 标准测试向量");
        }

        static void Test_CheckCRC_Valid()
        {
            byte[] frame = { 0x01, 0x03, 0x04, 0x41, 0xC8, 0x00, 0x00 };
            byte[] crc = CalcCRC(frame);
            byte[] full = new byte[frame.Length + 2];
            Array.Copy(frame, full, frame.Length);
            full[frame.Length] = crc[0]; full[frame.Length + 1] = crc[1];
            Assert(CheckCRC(full), "有效帧 CRC 验证通过");
        }

        static void Test_CheckCRC_Corrupted()
        {
            byte[] frame = { 0x01, 0x03, 0x04, 0x41, 0xC8, 0x00, 0x00, 0x00, 0x00 };
            Assert(!CheckCRC(frame), "损坏帧 CRC 验证拒绝");
        }

        static void Test_CheckData_InvalidHeader()
        {
            byte[] frame = { 0x02, 0x04, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            Assert(!CheckData(frame), "无效帧头被拒绝");
        }

        static void Test_CheckData_WrongLength()
        {
            byte[] frame = { 0x01, 0x03, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            Assert(!CheckData(frame), "错误长度被拒绝");
        }

        static void Test_AllCommands_Valid()
        {
            byte[][] cmds = new byte[][] {
                new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x04, 0x44, 0x09 },
                new byte[] { 0x01, 0x03, 0x00, 0x80, 0x00, 0x04, 0x45, 0xE1 },
                new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x02, 0xC4, 0x0B },
                new byte[] { 0x01, 0x03, 0x00, 0x02, 0x00, 0x02, 0x65, 0xCB },
                new byte[] { 0x01, 0x03, 0x00, 0x80, 0x00, 0x01, 0x85, 0xE2 },
                new byte[] { 0x01, 0x03, 0x00, 0x81, 0x00, 0x01, 0xD4, 0x22 },
                new byte[] { 0x01, 0x03, 0x00, 0x04, 0x00, 0x02, 0x85, 0xCA },
                new byte[] { 0x01, 0x03, 0x00, 0x82, 0x00, 0x01, 0x24, 0x22 },
                new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x06, 0xC5, 0xC8 },
                new byte[] { 0x01, 0x03, 0x00, 0x80, 0x00, 0x04, 0x45, 0xE1 },
            };
            for (int i = 0; i < cmds.Length; i++)
            {
                Assert(cmds[i].Length == 8, string.Format("命令{0}长度=8", i));
                Assert(CheckCRC(cmds[i]), string.Format("命令{0} CRC正确", i));
            }
        }

        static void Test_ParseTemp_Float()
        {
            // MODBUS 返回 25.0℃: 寄存器值 0x41C80000 → 字节 41 C8 00 00
            byte[] buf = { 0x01, 0x03, 0x04, 0x41, 0xC8, 0x00, 0x00, 0x00, 0x00 };
            byte[] a = { buf[6], buf[5], buf[4], buf[3] };
            float t = BitConverter.ToSingle(a, 0);
            Assert(Math.Abs(t - 25.0f) < 0.01f, string.Format("浮点温度 25.0 (实际: {0})", t));
        }

        static void Test_ParseTemp_Int()
        {
            int raw = 250;
            float t = raw / 10.0f;
            Assert(Math.Abs(t - 25.0f) < 0.01f, string.Format("整型温度 25.0 (实际: {0})", t));
        }
    }
}
