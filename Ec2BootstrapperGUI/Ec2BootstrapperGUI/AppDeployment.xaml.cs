﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Ec2Bootstrapperlib;
using System.Windows.Media.Animation;
using System.Threading;
using System.Windows.Forms;

namespace Ec2BootstrapperGUI
{
	/// <summary>
	/// Interaction logic for PasswordPrompt.xaml
	/// </summary>
	public partial class PasswordPrompt : Window
	{
        CEc2Instance _instance;
        string _password;
        string _msiPath;
        bool succeed = true;
        Thread oThread = null;

		public PasswordPrompt()
		{
			this.InitializeComponent();
            ProgBar.Visibility = Visibility.Hidden; 
        }

        public CEc2Instance instance
        {
            set { _instance = value; }
        }

        private delegate void StopProgressbarCallback();
        private delegate void SetStatusDone();

        private void installRemotely()
        {
            try
            {
                _instance.uploadAndInstallMsi(_password, _msiPath);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
                succeed = false;
            }

            Dispatcher.Invoke(new StopProgressbarCallback(disableProgressBar));
            Dispatcher.Invoke(new SetStatusDone(setStatusDone));
            oThread = null;
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OkButton.IsEnabled = false;
                AdminPassword.IsEnabled = false;
                msiPath.IsEnabled = false;
                msiPathButton.IsEnabled = false;

                if (_instance == null)
                {
                    throw new Exception("no valid instance");
                }

                StatusBk.Text = ConstantString.ContactAmazon;

                //access from another thread
                _password = AdminPassword.Password;
                _msiPath = msiPath.Text;
                enableProgressBar();

                oThread = new Thread(new ThreadStart(installRemotely));
                oThread.SetApartmentState(ApartmentState.STA);
                oThread.Start();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
                OkButton.IsEnabled = true;
                AdminPassword.IsEnabled = true;
                msiPath.IsEnabled = true;
                msiPathButton.IsEnabled = true;
            }
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (oThread != null)
                oThread.Abort();

            this.Close();
        }

        private void enableProgressBar()
        {
            ProgBar.Visibility = Visibility.Visible;
            ProgBar.IsIndeterminate = true;
            Duration duration = new Duration(TimeSpan.FromSeconds(10));
            DoubleAnimation doubleanimation = new DoubleAnimation(200.0, duration);
            ProgBar.BeginAnimation(System.Windows.Controls.ProgressBar.ValueProperty, doubleanimation);
        }

        private void disableProgressBar()
        {
            ProgBar.IsIndeterminate = false;
            ProgBar.BeginAnimation(System.Windows.Controls.ProgressBar.ValueProperty, null);
            ProgBar.Visibility = Visibility.Hidden;
        }

        private void setStatusDone()
        {
            if (succeed == true)
            {
                if (netInstall.IsChecked == true)
                {
                    System.Diagnostics.Process.Start("iexplore", "http://" + _instance.publicDns);
                }
                else
                {
                    System.Windows.MessageBox.Show("You have successfully deployed your program to your instance.", "Deploy Result", MessageBoxButton.OK, MessageBoxImage.Information);
                    //StatusBk.Text = ConstantString.Done;
                    //launch ie: assume the site deployed to the default root virtual directory.
                }
                this.Close();
            }
            else
            {
                StatusBk.Text = ConstantString.DeployFailed;
            }

            OkButton.IsEnabled = true;
            AdminPassword.IsEnabled = true;
            msiPath.IsEnabled = true;
            msiPathButton.IsEnabled = true;
        }

        private void msiPathButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "MSI files (*.msi)|*.msi";
            if (System.Windows.Forms.DialogResult.OK != ofd.ShowDialog())
            {
                throw new Exception("Error: open file dialog failed");
            }
            msiPath.Focus();
            msiPath.Text = ofd.FileName;
        }

        private void textBox_LostFocus(object sender, RoutedEventArgs e)
        {
            CueBannerTextBox tb = (CueBannerTextBox)sender;
            if (tb.Text.Length == 0 || tb.Text == tb.PromptText)
            {
                tb.UsePrompt = true;
                tb.Text = tb.PromptText;
            }
        }

        private void textBox_GotFocus(object sender, RoutedEventArgs e)
        {
            CueBannerTextBox tb = (CueBannerTextBox)sender;
            if (tb.UsePrompt)
            {
                tb.UsePrompt = false;
                tb.Text = string.Empty;
            }
        }
	}
}