
#region Using Statements

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

#endregion Using Statements

namespace AniDBmini
{
    public static class ConfigFile
    {

        #region Fields

        private static string configPath = @".\data\config.ini",
                              configSection = MainWindow.m_AppName;

        private static string[] configLangs = { "romaji", "nihongo", "english" };

        public static readonly Dictionary<string, string> Default = new Dictionary<string, string>
        {
            /* AniDBmini Defaults */
            { "autoLogin", "False" },
            { "rememberUser", "True" },
            { "username", "" },
            { "password", "" },
            { "server", "api.anidb.net" },
            { "port", "9000" },
            { "localPort", "9001" },
            { "minimizeTray", "True" },
            { "closeTray", "False" },
            { "constTray", "False" },
            { "singleTrayClick", "False" },

            /* Mylist Defaults */
            { "aLang", "romaji" },
            { "eLang", "english" },

            /* MPC Defaults */
            { "mpcPath", "" },
            { "mpcMarkWatched", "1" },
            { "mpcMarkWatchedPerc", "50" },
            { "mpcShowTitle", "True" },
            { "mpcShowOSD", "True" },
            { "mpcOSDPos", "1" },
            { "mpcOSDDurMS", "2000" },
            { "mpcClose", "False" }
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
        /// Read data value from the ini file
        /// </summary>
        public static ConfigValue Read(string Key)
        {
            CheckDefaultConfig();

            StringBuilder temp = new StringBuilder(255);
            WinAPI.GetPrivateProfileString(configSection, Key, null, temp, 255, configPath);

            if ((string.IsNullOrWhiteSpace(temp.ToString()) && !string.IsNullOrWhiteSpace(Default[Key])) ||
                ((Key == "aLang" || Key == "eLang") && !configLangs.Contains(temp.ToString())))
            {
                Write(Key, Default[Key]);
                return new ConfigValue(Default[Key]);
            }
            else
                return new ConfigValue(temp.ToString());
        }

        /// <summary>
        /// Write data to the ini file
        /// </summary>
        public static void Write(string Key, string Value)
        {
            CheckDefaultConfig();
            WinAPI.WritePrivateProfileString(configSection, Key, Value, configPath);
        }

        #endregion Reading/Writing

    }
}
