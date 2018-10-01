using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Pulse_PLC_Tools_2
{
    public class PulsePLCv2Config
    {
        public ImpParams Imp1 { get; set; }
        public ImpParams Imp2 { get; set; }
        public DeviceMainParams Device { get; set; }
        public List<DataGridRow_PLC> TablePLC { get; set; }
    }

    public static class FileConfigManager
    {
        static string FileName { get; set; }

        public static T ParseEnum<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }

        static string GetConfigString(ImpParams imp)
        {
            string impParams =
                imp.IsEnable.ToString() + ";" +
                imp.Adrs_PLC.ToString() + ";" +
                imp.A.ToString() + ";" +
                imp.Perepoln.ToString() + ";" +
                imp.Ascue_adrs.ToString() + ";" +
                imp.Ascue_pass_View.ToString() + ";" +
                imp.Ascue_protocol.ToString() + ";" +
                imp.Max_Power.ToString() + ";" +
                imp.T_qty.ToString() + ";" +
                imp.T1_Time_1.Hours.ToString() + ";" +
                imp.T1_Time_1.Minutes.ToString() + ";" +
                imp.T3_Time_1.Hours.ToString() + ";" +
                imp.T3_Time_1.Minutes.ToString() + ";" +
                imp.T1_Time_2.Hours.ToString() + ";" +
                imp.T1_Time_2.Minutes.ToString() + ";" +
                imp.T3_Time_2.Hours.ToString() + ";" +
                imp.T3_Time_2.Minutes.ToString() + ";" +
                imp.T2_Time.Hours.ToString() + ";" +
                imp.T2_Time.Minutes.ToString() + ";";
            return impParams;
        }
        public static ImpParams GetImpParamsFromString(string paramString, string versionOfConfig)
        {
            if (versionOfConfig != "PulsePLCv2.0") return null;
            ImpParams imp = new ImpParams();
            string[] configStrings = paramString.Split(new[] { ';' });
            if (configStrings.Length < 19)
            {
                MessageBox.Show("Ошибка при попытке чтения конфигурации: Импульсные входы. Возможно поврежден файл конфигурации.");
                return null;
            }
            try
            {
                imp.IsEnable = Convert.ToByte(configStrings[0]);
                imp.Adrs_PLC = Convert.ToByte(configStrings[1]);
                imp.A = Convert.ToUInt16(configStrings[2]);
                imp.Perepoln = ParseEnum<ImpOverflowType>(configStrings[3]);
                imp.Ascue_adrs = Convert.ToUInt16(configStrings[4]);
                imp.Ascue_pass_View = configStrings[5];
                imp.Ascue_protocol = Convert.ToByte(configStrings[6]);
                imp.Max_Power = Convert.ToUInt16(configStrings[7]);
                imp.T_qty = ParseEnum<ImpNumOfTarifs>(configStrings[8]);
                imp.T1_Time_1.Hours = Convert.ToByte(configStrings[9]);
                imp.T1_Time_1.Minutes = Convert.ToByte(configStrings[10]);
                imp.T3_Time_1.Hours = Convert.ToByte(configStrings[11]);
                imp.T3_Time_1.Minutes = Convert.ToByte(configStrings[12]);
                imp.T1_Time_2.Hours = Convert.ToByte(configStrings[13]);
                imp.T1_Time_2.Minutes = Convert.ToByte(configStrings[14]);
                imp.T3_Time_2.Hours = Convert.ToByte(configStrings[15]);
                imp.T3_Time_2.Minutes = Convert.ToByte(configStrings[16]);
                imp.T2_Time.Hours = Convert.ToByte(configStrings[17]);
                imp.T2_Time.Minutes = Convert.ToByte(configStrings[18]);
                return imp;
            }
            catch
            {
                MessageBox.Show("Ошибка при попытке чтения конфигурации: Импульсные входы. Возможно поврежден файл конфигурации.");
                return null;
            }
        }
        static string GetConfigString(DeviceMainParams device)
        {
            string impParams =
                device.WorkMode.ToString() + ";" +
                device.BatteryMode.ToString() + ";" +
                device.RS485_WorkMode.ToString() + ";" +
                device.Bluetooth_WorkMode.ToString() + ";" +
                device.PassRead_View.ToString() + ";" +
                device.PassWrite_View.ToString() + ";";
            return impParams;
        }
        public static DeviceMainParams GetDeviceParamsFromString(string paramString, string versionOfConfig)
        {
            if (versionOfConfig != "PulsePLCv2.0") return null;
            DeviceMainParams device = new DeviceMainParams();
            string[] configStrings = paramString.Split(new[] { ';' });
            if (configStrings.Length < 6)
            {
                MessageBox.Show("Ошибка при попытке чтения конфигурации: Основные настройки. Возможно поврежден файл конфигурации.");
                return null;
            }
            try
            {
                device.WorkMode = ParseEnum<WorkMode>(configStrings[0]);
                device.BatteryMode = ParseEnum<BatteryMode>(configStrings[1]);
                device.RS485_WorkMode = ParseEnum<InterfaceMode>(configStrings[2]);
                device.Bluetooth_WorkMode = ParseEnum<InterfaceMode>(configStrings[3]);
                device.PassRead_View = configStrings[4];
                device.PassWrite_View = configStrings[5];
                return device;
            }
            catch
            {
                MessageBox.Show("Ошибка при попытке чтения конфигурации: Основные настройки. Возможно поврежден файл конфигурации.");
                return null;
            }
        }
        static string GetConfigString(DataGridRow_PLC row)
        {
            string rowParams = row.Adrs_PLC + ";";
            rowParams += row.IsEnable + ";";
            rowParams += row.Serial_View + ";";
            rowParams += row.N + ";";
            rowParams += row.S1 + ";";
            rowParams += row.S2 + ";";
            rowParams += row.S3 + ";";
            rowParams += row.S4 + ";";
            rowParams += row.S5 + ";";
            rowParams += (byte)row.Protocol_ASCUE + ";";
            rowParams += row.Adrs_ASCUE + ";";
            rowParams += row.Pass_ASCUE_View + ";";
            rowParams += row.TypePLC + ";";
            return rowParams;
        }
        static DataGridRow_PLC GetPLCRowFromString(string paramString, string versionOfConfig)
        {
            if (versionOfConfig != "PulsePLCv2.0") return null;
            DataGridRow_PLC row = new DataGridRow_PLC();
            string[] configStrings = paramString.Split(new[] { ';' });
            if (configStrings.Length < 13)
            {
                MessageBox.Show("Ошибка при попытке чтения конфигурации: Основные настройки. Возможно поврежден файл конфигурации.");
                return null;
            }
            try
            {
                row.Adrs_PLC = Convert.ToByte(configStrings[0]);
                row.IsEnable = Convert.ToBoolean(configStrings[1]);
                row.Serial_View = configStrings[2];
                row.N = Convert.ToByte(configStrings[3]);
                row.S1 = Convert.ToByte(configStrings[4]);
                row.S2 = Convert.ToByte(configStrings[5]);
                row.S3 = Convert.ToByte(configStrings[6]);
                row.S4 = Convert.ToByte(configStrings[7]);
                row.S5 = Convert.ToByte(configStrings[8]);
                row.Protocol_ASCUE = ParseEnum<ImpAscueProtocolType>(configStrings[9]);
                row.Adrs_ASCUE = Convert.ToUInt16(configStrings[10]);
                row.Pass_ASCUE_View = configStrings[11];
                row.TypePLC = ParseEnum<PLCProtocolType>(configStrings[12]);
                return row;
            }
            catch
            {
                MessageBox.Show("Ошибка при попытке чтения конфигурации: Таблица PLC. Возможно поврежден файл конфигурации.");
                return null;
            }
        }

        public static void SaveConfig(PulsePLCv2Config config)
        {
            string versionOfConfigFile = "PulsePLCv2.0" + Environment.NewLine;
            string ConfigImp1 = GetConfigString(config.Imp1) + Environment.NewLine;
            string ConfigImp2 = GetConfigString(config.Imp2) + Environment.NewLine;
            string ConfigDevice = GetConfigString(config.Device) + Environment.NewLine;
            string ConfigTablePLC = "";
            config.TablePLC.ForEach(item => 
            {
                ConfigTablePLC += GetConfigString(item) + Environment.NewLine;
            });

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Конфигурация Pulse PLCv2 (*.pplc)|*.pplc";
            saveFileDialog.FileName = "Config_" + config.Device.Serial_View;

            if (saveFileDialog.ShowDialog() == true)
                File.WriteAllText(saveFileDialog.FileName, versionOfConfigFile+ConfigImp1 + ConfigImp2 + ConfigDevice + ConfigTablePLC);
        }

        public static PulsePLCv2Config LoadConfig()
        {
            OpenFileDialog myDialog = new OpenFileDialog();
            myDialog.Filter = "Конфигурация PulsePLCv2(*.pplc)|*.PPLC" + "|Все файлы (*.*)|*.*";
            myDialog.CheckFileExists = true;
            myDialog.Multiselect = false;

            PulsePLCv2Config config = new PulsePLCv2Config();

            if (myDialog.ShowDialog() == true)
            {
                FileName = myDialog.FileName;
                if (File.Exists(FileName))
                {
                    //Try to read config lines
                    string[] lines = File.ReadLines(FileName).ToArray();
                    if(lines.Length < 254)
                    {
                        MessageBox.Show("Ошибка при попытке чтения конфигурации. В файле недостаточно данных.");
                        return null;
                    }
                    //If read lines is ok, copy
                    string versionOfConfigFile = lines[0];
                    string ConfigImp1 = lines[1];
                    string ConfigImp2 = lines[2];
                    string ConfigDevice = lines[3];
                    string[] ConfigTablePLC = new string[250];
                    for (int i = 0; i < 250; i++)
                    {
                        ConfigTablePLC[i] = lines[i + 4];
                    }

                    //try to get data from lines
                    config.Imp1 = GetImpParamsFromString(ConfigImp1, versionOfConfigFile);
                    if (config.Imp1 == null) return null;

                    config.Imp2 = GetImpParamsFromString(ConfigImp2, versionOfConfigFile);
                    if (config.Imp2 == null) return null;

                    config.Device = GetDeviceParamsFromString(ConfigDevice, versionOfConfigFile);
                    if (config.Device == null) return null;

                    config.TablePLC = new List<DataGridRow_PLC>();
                    foreach (var item in ConfigTablePLC)
                    {
                        DataGridRow_PLC row = GetPLCRowFromString(item, versionOfConfigFile);
                        if (row == null) return null;

                        config.TablePLC.Add(row);
                    }
                }
                else
                {
                    MessageBox.Show("Файла не существует");
                    return null;
                }
                    
            }

            return config;
        }
    }
}
