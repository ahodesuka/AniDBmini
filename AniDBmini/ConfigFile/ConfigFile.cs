
#region Using Statements

using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

#endregion Using Statements

namespace AniDBmini
{
    /// <summary>
    /// Create a New INI file to store or load data
    /// </summary>
    public class ConfigFile
    {
        public string configPath, configSection;
        public Dictionary<string, string> Defaults = new Dictionary<string, string>();

        /// <summary>
        /// method for writing data to an ini file
        /// </summary>
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string lPAppName, string lpKeyName, string lpString, string lpFileName);

        /// <summary>
        /// method for reading from a specified section of an ini file
        /// </summary>
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, int nSize, string lpFilePath);

        /// <summary>
        /// ConfigFile Constructor.
        /// </summary>
        public ConfigFile(string path, string section)
        {
            LoadDefaultConfig();

            configPath = path;
            configSection = section;

            if (!File.Exists(path) || string.IsNullOrWhiteSpace(File.ReadAllText(path)))
                CreateDefaultConfig();
        }

        #region Private Methods

        /// <summary>
        /// Add the default values to the defaultConfig dictionary.
        /// </summary>
        private void LoadDefaultConfig()
        {
            Defaults.Add("autoLogin", "false");
            Defaults.Add("username", "");
            Defaults.Add("password", "");
            Defaults.Add("server", "api.anidb.net");
            Defaults.Add("port", "9000");
            Defaults.Add("localPort", "9001");
        }

        /// <summary>
        /// Creates a default config file.
        /// </summary>
        private void CreateDefaultConfig()
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

            foreach (KeyValuePair<string, string> kvp in Defaults)
                this.Write(kvp.Key, kvp.Value);
        }

        #endregion

        #region Reading/Writing

        /// <summary>
        /// Read Data Value From the ini File
        /// </summary>
        public ConfigValue Read(string Key)
        {
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(configSection, Key, null, temp, 255, configPath);

            if (string.IsNullOrWhiteSpace(temp.ToString()) && !string.IsNullOrWhiteSpace(Defaults[Key]))
            {
                Write(Key, Defaults[Key]);
                return new ConfigValue(Defaults[Key]);
            }
            else
                return new ConfigValue(temp.ToString());
        }

        /// <summary>
        /// Write Data to the ini File
        /// </summary>
        public void Write(string Key, string Value)
        {
            WritePrivateProfileString(configSection, Key, Value, configPath);
        }

        #endregion Reading/Writing

    }
}
