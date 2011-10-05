
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

        #region Private Methods

        private void StartMainWindow()
        {
            MainWindow main = new MainWindow(aniDB);            
            main.Show();

            aniDB.MainWindow = main;

            this.Close();
        }

        #endregion Private Methods

        #region Events

        private void OnInitialized(object sender, EventArgs e)
        {
            if (ConfigFile.Read("autoLogin").ToBoolean() &&
                (aniDB = new AniDBAPI(ConfigFile.Read("server").ToString(),
                                      ConfigFile.Read("port").ToInt32(),
                                      ConfigFile.Read("localPort").ToInt32())).isConnected &&
                aniDB.Login(ConfigFile.Read("username").ToString(), ConfigFile.Read("password").ToString()))
                StartMainWindow();
            else if (ConfigFile.Read("rememberUser").ToBoolean())
            {
                rememberUserCheckBox.IsChecked = true;
                usernameTextBox.Text = ConfigFile.Read("username").ToString();
            }
        }

        private void loginButton_Click(object sender, RoutedEventArgs e)
        {
			if (usernameTextBox.Text == String.Empty ||
                !System.Text.RegularExpressions.Regex.IsMatch(usernameTextBox.Text, @"^[A-z0-9_-]+$") ||
                passwordPasswordBox.Password == String.Empty)
			{
				MessageBox.Show("Enter a valid username and password!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

            loginButton.IsEnabled = false;
            string[] server = serverComboBox.SelectedValue.ToString().Split(':');

            if ((aniDB = new AniDBAPI(server[0], int.Parse(server[1]), ConfigFile.Read("localPort").ToInt32())).isConnected &&
                aniDB.Login(usernameTextBox.Text, passwordPasswordBox.Password))
            {
                if (autoLoginCheckBox.IsChecked == true)
                {
                    ConfigFile.Write("autoLogin", "True");
                    ConfigFile.Write("rememberUser", "True");
                    ConfigFile.Write("username", usernameTextBox.Text);
                    ConfigFile.Write("password", passwordPasswordBox.Password);
                    ConfigFile.Write("server", server[0]);
                    ConfigFile.Write("port", server[1]);
                }
                else if (rememberUserCheckBox.IsChecked == true)
                {
                    ConfigFile.Write("rememberUser", "True");
                    ConfigFile.Write("username", usernameTextBox.Text);
                }
                else
                {
                    ConfigFile.Write("autoLogin", "False");
                    ConfigFile.Write("rememberUser", "False");
                    ConfigFile.Write("username", string.Empty);
                    ConfigFile.Write("password", string.Empty);
                }

                StartMainWindow();
            }
            else
                loginButton.IsEnabled = true;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
				loginButton_Click(sender, null);
		}

        private void autoLoginCheckBox_Click(object sender, RoutedEventArgs e)
        {
            bool isChecked = (bool)autoLoginCheckBox.IsChecked;
            rememberUserCheckBox.IsEnabled = !isChecked;
        }

        #endregion

    }
}
