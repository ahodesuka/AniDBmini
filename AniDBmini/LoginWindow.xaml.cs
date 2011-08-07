
#region Using Statements

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

#endregion

namespace AniDBmini
{
    public partial class LoginWindow : Window
    {

        #region Fields

        private AniDBAPI aniDB;
        private ConfigFile config;
        private string configPath = @".\data\config.ini";

        #endregion Fields

        #region Constructor

        public LoginWindow()
        {
            InitializeComponent();
        }

        #endregion Constructor

        #region Events

        private void Window_Initialized(object sender, EventArgs e)
        {
            config = new ConfigFile(configPath, MainWindow.m_AppName);

            if (config.Read("autoLogin").ToBoolean() &&
                (aniDB = new AniDBAPI(config.Read("server").ToString(), config.Read("port").ToInt32(), config.Read("localPort").ToInt32())) != null)
#if !OFFLINE
                if (aniDB.Login(config.Read("username").ToString(), config.Read("password").ToString()))
#endif
            {
                    var main = new MainWindow(aniDB);
                    main.Show();

                    this.Close();
                }
        }

        private void loginButton_Click(object sender, RoutedEventArgs e)
        {
			if (usernameTextBox.Text == string.Empty ||
                !System.Text.RegularExpressions.Regex.IsMatch(usernameTextBox.Text, @"^[A-z0-9_-]+$") ||
                passwordPasswordBox.Password == string.Empty)
			{
				MessageBox.Show("Enter a valid username and password!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

            loginButton.IsEnabled = false;
            string[] server = serverComboBox.SelectedValue.ToString().Split(':');

            if ((aniDB = new AniDBAPI(server[0], int.Parse(server[1]), config.Read("localPort").ToInt32())) != null)
                if (!aniDB.Login(usernameTextBox.Text, passwordPasswordBox.Password))
                    loginButton.IsEnabled = true;
                else
                {
                    if (autoLoginCheckBox.IsChecked == true)
                    {
                        config.Write("autoLogin", "true");
                        config.Write("username", usernameTextBox.Text);
                        config.Write("password", passwordPasswordBox.Password);
                        config.Write("server", server[0]);
                        config.Write("port", server[1]);
                    }

                    var main = new MainWindow(aniDB);
                    main.Show();

                    this.Close();
                }
        }

		private void loginTextBox_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
				loginButton_Click(sender, null);
		}

        #endregion

    }
}
