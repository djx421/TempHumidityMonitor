using System.Configuration;
using System.ComponentModel;

namespace TempHumidityMonitor.Properties
{
    internal sealed partial class Settings : ApplicationSettingsBase
    {
        private static Settings defaultInstance = ((Settings)(Synchronized(new Settings())));

        public static Settings Default
        {
            get { return defaultInstance; }
        }

        [UserScopedSetting]
        [DefaultSettingValue("9600")]
        public int BaudRate
        {
            get { return ((int)(this["BaudRate"])); }
            set { this["BaudRate"] = value; }
        }

        [UserScopedSetting]
        [DefaultSettingValue("8")]
        public int DataBits
        {
            get { return ((int)(this["DataBits"])); }
            set { this["DataBits"] = value; }
        }

        [UserScopedSetting]
        [DefaultSettingValue("One")]
        public string StopBits
        {
            get { return ((string)(this["StopBits"])); }
            set { this["StopBits"] = value; }
        }

        [UserScopedSetting]
        [DefaultSettingValue("None")]
        public string Parity
        {
            get { return ((string)(this["Parity"])); }
            set { this["Parity"] = value; }
        }

        [UserScopedSetting]
        [DefaultSettingValue("30")]
        public int MaxChartPoints
        {
            get { return ((int)(this["MaxChartPoints"])); }
            set { this["MaxChartPoints"] = value; }
        }

        [UserScopedSetting]
        [DefaultSettingValue("1000")]
        public int SampleInterval
        {
            get { return ((int)(this["SampleInterval"])); }
            set { this["SampleInterval"] = value; }
        }

        [UserScopedSetting]
        [DefaultSettingValue("0")]
        public int ReadMode
        {
            get { return ((int)(this["ReadMode"])); }
            set { this["ReadMode"] = value; }
        }

        [UserScopedSetting]
        [DefaultSettingValue("40")]
        public float TempHighAlarm
        {
            get { return ((float)(this["TempHighAlarm"])); }
            set { this["TempHighAlarm"] = value; }
        }

        [UserScopedSetting]
        [DefaultSettingValue("0")]
        public float TempLowAlarm
        {
            get { return ((float)(this["TempLowAlarm"])); }
            set { this["TempLowAlarm"] = value; }
        }

        [UserScopedSetting]
        [DefaultSettingValue("80")]
        public float HumiHighAlarm
        {
            get { return ((float)(this["HumiHighAlarm"])); }
            set { this["HumiHighAlarm"] = value; }
        }

        [UserScopedSetting]
        [DefaultSettingValue("20")]
        public float HumiLowAlarm
        {
            get { return ((float)(this["HumiLowAlarm"])); }
            set { this["HumiLowAlarm"] = value; }
        }

        [UserScopedSetting]
        [DefaultSettingValue("False")]
        public bool EnableAlarm
        {
            get { return ((bool)(this["EnableAlarm"])); }
            set { this["EnableAlarm"] = value; }
        }

        [UserScopedSetting]
        [DefaultSettingValue("True")]
        public bool EnableDataLog
        {
            get { return ((bool)(this["EnableDataLog"])); }
            set { this["EnableDataLog"] = value; }
        }
    }
}
