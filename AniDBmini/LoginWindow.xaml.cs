
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
            if (ConfigFile.Read("autoLogin").ToBoolean() &&
                (aniDB = new AniDBAPI(ConfigFile.Read("server").ToString(), ConfigFile.Read("port").ToInt32(), ConfigFile.Read("localPort").ToInt32())) != null)
            {
#if !OFFLINE
                if (aniDB.Login(ConfigFile.Read("username").ToString(), ConfigFile.Read("password").ToString()))
                {
#endif
                    var main = new MainWindow(aniDB);
                    main.Show();

                    this.Close();
#if !OFFLINE
                }
#endif
            }
            else if (ConfigFile.Read("rememberUser").ToBoolean())
            {
                rememberUserCheckBox.IsChecked = true;
                usernameTextBox.Text = ConfigFile.Read("username").ToString();
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

            if ((aniDB = new AniDBAPI(server[0], int.Parse(server[1]), ConfigFile.Read("localPort").ToInt32())) != null)
                if (!aniDB.Login(usernameTextBox.Text, passwordPasswordBox.Password))
                    loginButton.IsEnabled = true;
                else
                {
                    if (autoLoginCheckBox.IsChecked == true)
                    {
                        ConfigFile.Write("autoLogin", "true");
                        ConfigFile.Write("username", usernameTextBox.Text);
                        ConfigFile.Write("password", passwordPasswordBox.Password);
                        ConfigFile.Write("server", server[0]);
                        ConfigFile.Write("port", server[1]);
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

        private void autoLoginCheckBox_Click(object sender, RoutedEventArgs e)
        {
            bool isChecked = (bool)autoLoginCheckBox.IsChecked;
            rememberUserCheckBox.IsChecked = isChecked;
            rememberUserCheckBox.IsEnabled = !isChecked;
        }

    }
}
