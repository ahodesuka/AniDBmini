
#region Using Statements

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

#endregion Using Statements

namespace AniDBmini
{
    public partial class OptionsWindow : Window
    {

        #region Fields

        private bool isInitialized, dlgResult;

        #endregion Fields

        #region Constructor

        public OptionsWindow()
        {
            InitializeComponent();
            LoadOptions();

            applyButton.IsEnabled = false;
            isInitialized = true;
        }

        #endregion Constructor

        #region Private Methods

        private void LoadOptions()
        {
            adbmUsernameTextBox.Text = ConfigFile.Read("username").ToString();
            adbmPasswordPasswordBox.Password = ConfigFile.Read("password").ToString();
            adbmLocalPortTextBox.Text = ConfigFile.Read("localPort").ToString();
            adbmAutoLoginCheckBox.IsChecked = ConfigFile.Read("autoLogin").ToBoolean();
            adbmRememberUserCheckBox.IsChecked = ConfigFile.Read("rememberUser").ToBoolean();

            MPCAPI.MPC_WATCHED mpcMarkedWatched = (MPCAPI.MPC_WATCHED)ConfigFile.Read("mpcMarkWatched").ToInt32();

            mpchcLocationTextBox.Text = ConfigFile.Read("mpcPath").ToString();

            if (mpcMarkedWatched == MPCAPI.MPC_WATCHED.AFTER_FINISHED)
                mpcMarkAfter.IsChecked = true;
            else if (mpcMarkedWatched == MPCAPI.MPC_WATCHED.DURING_TICKS)
                mpcMarkDuring.IsChecked = true;

            mpcWatchedPercSlider.Value = ConfigFile.Read("mpcMarkWatchedPerc").ToInt32();
            mpcShowFileInTitle.IsChecked = ConfigFile.Read("mpcShowTitle").ToBoolean();
            mpcShowWatchedOSD.IsChecked = ConfigFile.Read("mpcShowOSD").ToBoolean();
            mpcClose.IsChecked = ConfigFile.Read("mpcClose").ToBoolean();
            mpcOSDPos.SelectedIndex = ConfigFile.Read("mpcOSDPos").ToInt32() - 1;
            mpcOSDDurMS.SelectedIndex = ConfigFile.Read("mpcOSDDurMS").ToInt32() / 1000 - 1;
        }

        private void SaveOptions()
        {
            ConfigFile.Write("username", adbmUsernameTextBox.Text);
            ConfigFile.Write("password", adbmPasswordPasswordBox.Password);
            ConfigFile.Write("localPort", adbmLocalPortTextBox.Text);
            ConfigFile.Write("autoLogin", adbmAutoLoginCheckBox.IsChecked.ToString());
            ConfigFile.Write("rememberUser", adbmRememberUserCheckBox.IsChecked.ToString());

            ConfigFile.Write("mpcPath", mpchcLocationTextBox.Text);
            ConfigFile.Write("mpcMarkWatched", mpcMarkAfter.IsChecked == true ? "2" : (mpcMarkDuring.IsChecked == true ? "1" : "0"));
            ConfigFile.Write("mpcMarkWatchedPerc", mpcWatchedPerc.Text);
            ConfigFile.Write("mpcShowTitle", mpcShowFileInTitle.IsChecked.ToString());
            ConfigFile.Write("mpcShowOSD", mpcShowWatchedOSD.IsChecked.ToString());
            ConfigFile.Write("mpcClose", mpcClose.IsChecked.ToString());
            ConfigFile.Write("mpcOSDPos", (mpcOSDPos.SelectedIndex + 1).ToString());
            ConfigFile.Write("mpcOSDDurMS", ((mpcOSDDurMS.SelectedIndex + 1) * 1000).ToString());

            dlgResult = true;
            applyButton.IsEnabled = false;
            okButton.Focus();
        }

        #endregion Private Methods

        #region Events

        private void OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (isInitialized)
            {
                int oldIndex = int.Parse(((TreeViewItem)e.OldValue).Tag.ToString());
                OptionGirds.Children[oldIndex].Visibility = System.Windows.Visibility.Collapsed;

                int selectedIndex = int.Parse(((TreeViewItem)e.NewValue).Tag.ToString());
                OptionGirds.Children[selectedIndex].Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void MPCBrowseOnClick(object sender, RoutedEventArgs e)
        {
            string pFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "MPC-HC main executable|mpc-hc.exe;mpc-hc64.exe";
            dlg.InitialDirectory = !string.IsNullOrWhiteSpace(mpchcLocationTextBox.Text) ? mpchcLocationTextBox.Text :
                                   (System.IO.Directory.Exists(pFilesPath + @"\Media Player Classic - Home Cinema") ?
                                   pFilesPath + @"\Media Player Classic - Home Cinema" : pFilesPath);

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                if (System.IO.Path.GetFileNameWithoutExtension(dlg.FileName) == "mpc-hc64" && IntPtr.Size == 4)
                    MessageBox.Show(String.Format("Media Player Classic - Home Cinema 64bit will not work\nwith the 32bit version of {0}.\n\n" +
                                    "Please use the 64bit version of {0}.\nOr use the 32bit version of Media Player Classic - Home Cinema.",  MainWindow.m_AppName),
                                    "Alert!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                else
                    mpchcLocationTextBox.Text = dlg.FileName;
            }
        }

        private void enableApplyButton(object sender, EventArgs e)
        {
            if (isInitialized)
                applyButton.IsEnabled = true;
        }

        private void OKOnClick(object sender, RoutedEventArgs e)
        {
            if (applyButton.IsEnabled)
                SaveOptions();

            this.DialogResult = dlgResult;
        }

        private void CancelOnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ApplyOnClick(object sender, RoutedEventArgs e)
        {
            SaveOptions();            
        }

        #endregion Events

    }
}
