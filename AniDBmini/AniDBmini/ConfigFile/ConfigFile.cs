
#region Using Statements

using System.Collections.Generic;
using System.IO;
using System.Text;

#endregion Using Statements

namespace AniDBmini
{
    /// <summary>
    /// Create a New INI file to store or load data
    /// </summary>
    public static class ConfigFile
    {

        #region Fields

        private static string configPath = @".\data\config.ini",
                              configSection = MainWindow.m_AppName;

        public static readonly Dictionary<string, string> Default = new Dictionary<string, string>
        {
            { "autoLogin", "false" },
            { "rememberUser", "true" },
            { "username", "" },
            { "password", "" },
            { "server", "api.anidb.net" },
            { "port", "9000" },
            { "localPort", "9001" },
            { "mpcPath", "" },
            { "mpcMarkWatched", "1" },
            { "mpcMarkWatchedPerc", "50" },
            { "mpcShowTitle", "true" },
            { "mpcShowOSD", "true" },
            { "mpcOSDPos", "1" },
            { "mpcOSDDurMS", "2000" },
        };

        #endregion Fields

        #region Private Methods

        /// <summary>
        /// Check if the config file exists, or if it is emtpy.
        /// If either, create a config file w/defaults.
        /// </summary>
        private static void CheckDefaultConfig()
        {
            if (!File.Exists(configPath) || string.IsNullOrWhiteSpace(File.ReadAllText(configPath)))
            {
                try
                {
                    using (StreamWriter sw = File.CreateText(configPath))
                        sw.WriteLine("[" + configSection + "]");
                }
                catch (DirectoryNotFoundException)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(configPath));
                }
                finally
                {
                    using (StreamWriter sw = File.CreateText(configPath))
                        sw.WriteLine("[" + configSection + "]");
                }

                foreach (KeyValuePair<string, string> kvp in Default)
                    Write(kvp.Key, kvp.Value);
            }
        }

        #endregion

        #region Reading/Writing

        /// <summary>
        /// Read Data Value From the ini File
        /// </summary>
        public static ConfigValue Read(string Key)
        {
            CheckDefaultConfig();

            StringBuilder temp = new StringBuilder(255);
            int i = WinAPI.GetPrivateProfileString(configSection, Key, null, temp, 255, configPath);

            if (string.IsNullOrWhiteSpace(temp.ToString()) && !string.IsNullOrWhiteSpace(Default[Key]))
            {
                Write(Key, Default[Key]);
                return new ConfigValue(Default[Key]);
            }
            else
                return new ConfigValue(temp.ToString());
        }

        /// <summary>
        /// Write Data to the ini File
        /// </summary>
        public static void Write(string Key, string Value)
        {
            CheckDefaultConfig();

            WinAPI.WritePrivateProfileString(configSection, Key, Value, configPath);
        }

        #endregion Reading/Writing

    }
}
