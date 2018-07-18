using BluePexEPLib.suiteapi.Client;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Management;

namespace BluePexEPLib
{
    public static class Settings
    {
        #region Private Variables
        private static string CompanyName = @"Bluepex Security Solutions";
        private static int DefaultInterval = 120000;
        private static string p_MessageQueueServerAddress = @"mq.bluepex.com.br";

        /// <summary>
        /// Caminho para o diretório ProgramData\Bluepex
        /// </summary>
        private static string _BluepexAppDataDir;

        private static RegistryKey _Registro;
        #endregion
        public static string DefaultServerAddress = @"mq.bluepex.com.br";

        #region Main Settings Methods
        private static RegistryKey GetRegistro()
        {
            if (_Registro == null)
            {
                var SoftwareKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
                var AppNameKey = SoftwareKey.CreateSubKey(@"Software\\" + CompanyName);
                _Registro = AppNameKey.CreateSubKey(@"SimAgent");
            }

            return _Registro;
        }

        public static void ReadString(string key, ref string output)
        {
            try
            {
                var key_value = GetRegistro().GetValue(key)?.ToString();
                if (!string.IsNullOrEmpty(key_value))
                    output = key_value;
            }
            catch (Exception)
            {
                try
                {
                    GetRegistro().SetValue(key, output);
                }
                catch (Exception ex)
                {
                    BpxLogger.LogException(ex);
                }
            }
        }

        public static void SaveString(string name, string value)
        {
            try
            {
                GetRegistro().SetValue(name, value);
            }
            catch (Exception ex)
            {
                BpxLogger.LogException(ex);
            }
        }
        #endregion

        #region Comunication/Network Methods
        public static int GetInterval()
        {
            var interval = 0;
            try
            {
                interval = Convert.ToInt32(GetRegistro().GetValue(nameof(interval)));
            }
            catch (Exception)
            {
                // se der erro ajusta o default
                interval = DefaultInterval;
            }

            // Não será aceita um valor igual a 0
            if (interval == 0)
            {
                interval = DefaultInterval;
                // modifica no registro para o valor padrão
                try
                {
                    GetRegistro().SetValue(nameof(interval), interval);
                }
                catch (Exception ex)
                {
                    BpxLogger.LogException(ex);
                }
            }

            return interval;
        }

        public static string GetMQAddress()
        {
            try
            {
                p_MessageQueueServerAddress = GetRegistro().GetValue("mq_address")?.ToString();
            }
            catch (Exception ex)
            {
                BpxLogger.LogException(ex);
            }

            if (string.IsNullOrEmpty(p_MessageQueueServerAddress))
            {
                GetRegistro().SetValue("mq_address", DefaultServerAddress);
                p_MessageQueueServerAddress = DefaultServerAddress;
            }

            return p_MessageQueueServerAddress;
        }

        public static string GetAPIAddress()
        {
            var output = Configuration.API_URL;
            ReadString(nameof(Configuration.API_URL).ToLower(), ref output);
            if (!output.EndsWith(@"/"))
                output += @"/";

            if (!(output.StartsWith(@"http://") || output.StartsWith(@"https://")))
                output = output.Insert(0, @"https://");
            return output;
        }
        #endregion

        #region Device Configuration
        public static string GetSerial()
        {
            var serial = string.Empty;
            try
            {
                ReadString(nameof(serial), ref serial);
                //serial = GetRegistro()?.GetValue(nameof(serial))?.ToString();
                if (serial.Equals(null))
                    serial = string.Empty;
                // Se o número de série está vazio, tenta ler a chave 64 Bits caso os OS seja 64bit
                // útil no caso de upgrade de versão (da 0.6.4 para 0.6.5)
                var validator = new SerialValidator();
                bool serial_valid;
                try
                {
                    validator.ValidateSerial(serial);
                    serial_valid = validator.IsValid;
                }
                catch (Exception)
                {
                    serial_valid = false;
                }
                if (!serial_valid && Environment.Is64BitOperatingSystem)
                {
                    var SoftwareKey64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                    var AppNameKey64 = SoftwareKey64.CreateSubKey("Software\\" + CompanyName);
                    var Registro64 = AppNameKey64.CreateSubKey("SimAgent");
                    serial = Registro64?.GetValue(nameof(serial))?.ToString();
                    if (!string.IsNullOrEmpty(serial))
                        GetRegistro()?.SetValue(nameof(serial), serial);
                }
            }
            catch (Exception ex)
            {
                BpxLogger.LogException(ex);
                if (!string.IsNullOrEmpty(serial))
                    SaveString(nameof(serial), serial);
            }
            if (string.IsNullOrEmpty(serial))
                serial = string.Empty;
            return serial;
        }

        /// <summary>
        /// Retorna o DeviceID do computador
        /// </summary>
        /// <returns>O DeviceID ou uma string vazia</returns>
        public static string GetDeviceID()
        {
            try
            {
                var softwarekey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                var appnamekey = softwarekey.CreateSubKey("Software\\Microsoft\\Cryptography");
                return appnamekey.GetValue("MachineGUID").ToString();
            }
            catch (Exception ex)
            {
                BpxLogger.LogException(ex);
            }
            return string.Empty;
        }

        /// <summary>
        /// Lê o registro do Windows para saber se é para armazenar logs de Debug
        /// </summary>
        public static bool GetDebug()
        {
            var t = 0;
            try
            {
                t = Convert.ToInt32(GetRegistro()?.GetValue("debug"));
            }
            catch (Exception)
            {
                t = 0;
            }
            return t == 1;
        }

        public static void SetDebug(bool enable)
        {
            if (enable)
                GetRegistro()?.SetValue("debug", 1);
            else
                GetRegistro()?.SetValue("debug", 0);
        }
        #endregion

        #region Operational System
        public static string GetPlatform()
        {
            const string query_item = @"Win32_Processor";
            const string property_name = @"AddressWidth";
            const string default_value = @"32";

            var mgmtScope = new ManagementScope(@"\\localhost\root\cimv2");
            mgmtScope.Connect();
            var select_query = new SelectQuery(query_item, "", new string[] { property_name });
            using (var mgmtSrchr = new ManagementObjectSearcher(mgmtScope, select_query))
            {
                var property_value = mgmtSrchr.Get()
                    .Cast<ManagementBaseObject>()
                    .First().GetPropertyValue(property_name)?.ToString();

                if (string.IsNullOrEmpty(property_value))
                    property_value = default_value;

                return property_value;
            }
        }

        public static string GetAppDataDir()
        {
            try
            {
                if (!string.IsNullOrEmpty(_BluepexAppDataDir) && Directory.Exists(_BluepexAppDataDir))
                    return _BluepexAppDataDir;

                _BluepexAppDataDir = $@"{Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)}\Bluepex\";
                if (!Directory.Exists(_BluepexAppDataDir))
                    Directory.CreateDirectory(_BluepexAppDataDir);
            }
            catch (Exception ex)
            {
                BpxLogger.LogException(ex);
                return string.Empty;
            }

            return _BluepexAppDataDir;
        }
        #endregion

    }
}