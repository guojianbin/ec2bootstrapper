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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using Ec2Bootstrapperlib;
using System.Windows.Media.Animation;
using System.IO;

namespace Ec2BootstrapperGUI
{
	/// <summary>
	/// Interaction logic for Launch.xaml
	/// </summary>
	public partial class InstanceLauncher
    {
        List<string> _keyPairs;
        List<string> _securityGroups;
        List<string> _zones;
        AmiPicker _amipicker;
        string _amiId;
        string _selectedKeyPair;
        string _selectedSecurityGroups;
        string _selectedZone;
        Dashboard _dashboard;
        Thread launchThread = null;

        public InstanceLauncher()
        {
            this.InitializeComponent();
            _keyPairs = new List<string>();
            _securityGroups = new List<string>();
            _zones = new List<string>();
            LaunchProgBar.Visibility = Visibility.Hidden;
            KeyPairComb.IsEnabled = false;
            SecurityGroupComb.IsEnabled = false;
            ZoneComb.IsEnabled = false;
        }

        public List<string> keyPairs
        {
            get { return _keyPairs; }
        }

        private void fetchInformationFromAms()
        {
            if (_securityGroups.Count == 0 ||
                _keyPairs.Count == 0 ||
                _zones.Count == 0)
            {
                enableProgressBar();
                Thread oThread = new Thread(new ThreadStart(fetchInforThread));
                oThread.Start();
            }
        }

        private delegate void StopProgressbarCallback();
        private delegate void DisableLaunchButton();

        private void disableLaunchButton()
        {
            LaunchButton.IsEnabled = false;
        }

        private void fetchInforThread()
        {
            try
            {
                CEc2Service serv = new CEc2Service(_dashboard.awsConfig);
                if (_securityGroups.Count == 0)
                {
                    List<string> sgs = serv.descrbibeSecurityGroups();
                    foreach (string sg in sgs)
                        _securityGroups.Add(sg);

                }
                if (_keyPairs.Count == 0)
                {
                    List<string> kps = serv.descrbibeKeyPairs();
                    foreach (string kp in kps)
                        _keyPairs.Add(kp);
                }
                if (_zones.Count == 0)
                {
                    List<string> zs = serv.descrbibeZones();
                    foreach (string z in zs)
                        _zones.Add(z);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Dispatcher.Invoke(new DisableLaunchButton(disableLaunchButton));
            }

            Dispatcher.Invoke(new StopProgressbarCallback(disableProgressBar));
        }

        public List<string> securityGroups
        {
            get { return _securityGroups; }
        }

        public List<string> zones
        {
            get { return _zones; }
        }

        public AmiPicker amiPicker
        {
            set { _amipicker = value; }
        }

        public string amiId
        {
            set { AmiIdLb.Content = value; }
        }

        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (launchThread != null)
                {
                    launchThread.Abort();
                    launchThread.Join();
                }
            }
            catch (Exception)
            {
            }

            this.Close();
        }

        private void small_Click(object sender, RoutedEventArgs e)
        {
            Medium.IsChecked = !Small.IsChecked;
        }

        private void medium_Click(object sender, RoutedEventArgs e)
        {
            Small.IsChecked = !Medium.IsChecked;
        }

        public Dashboard dashboard
        {
            set
            {
                _dashboard = value;
                //once dashboard set, we have access to config info
                //hence we can fetch information from ams
                fetchInformationFromAms();
            }
        }

        private void enableProgressBar()
        {
            LaunchButton.IsEnabled = false;
            StatusDesc.Content = ConstantString.Launching;
            LaunchProgBar.Visibility = Visibility.Visible;
            LaunchProgBar.IsIndeterminate = true;
            Duration duration = new Duration(TimeSpan.FromSeconds(10));
            DoubleAnimation doubleanimation = new DoubleAnimation(200.0, duration);
            LaunchProgBar.BeginAnimation(System.Windows.Controls.ProgressBar.ValueProperty, doubleanimation);
        }

        private void disableProgressBar()
        {
            LaunchProgBar.IsIndeterminate = false;
            LaunchProgBar.BeginAnimation(System.Windows.Controls.ProgressBar.ValueProperty, null);
            LaunchProgBar.Visibility = Visibility.Hidden;
            StatusDesc.Content = ConstantString.Done;
            LaunchButton.IsEnabled = true;
            KeyPairComb.IsEnabled = true;
            SecurityGroupComb.IsEnabled = true;
            ZoneComb.IsEnabled = true;
        }

        private void launch()
        {
            try
            {
                CEc2Instance inst = new CEc2Instance(_dashboard.awsConfig);
                inst.imageId = _amiId;
                if(string.IsNullOrEmpty(_selectedKeyPair) == false)
                    inst.keyPairName = _selectedKeyPair;
                if(string.IsNullOrEmpty(_selectedSecurityGroups) == false)
                    inst.securityGroups = _selectedSecurityGroups;

                inst.launch();
            }
            catch (ThreadAbortException)
            {
                //don't do anything 
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            Dispatcher.Invoke(new StopProgressbarCallback(disableProgressBar));
            launchThread = null;
        }

        private void launchButton_Click(object sender, RoutedEventArgs e)
        {
            //other thread will access these
            _amiId = AmiIdLb.Content.ToString();
            if (KeyPairComb.SelectedValue != null)
            {
                _selectedKeyPair = KeyPairComb.SelectedValue.ToString();
                if (string.IsNullOrEmpty(_selectedKeyPair) == false)
                {
                    string keyFile = _dashboard.awsConfig.getKeyFilePath(_selectedKeyPair);
                    if (string.IsNullOrEmpty(keyFile) == true ||
                        File.Exists(keyFile) == false)
                    {
                        KeyFileInputDlg kfInput = new KeyFileInputDlg(_selectedKeyPair, _dashboard);
                        kfInput.ShowDialog();
                    }
                    keyFile = _dashboard.awsConfig.getKeyFilePath(_selectedKeyPair);
                    if (string.IsNullOrEmpty(keyFile) == true ||
                        File.Exists(keyFile) == false)
                    {
                        //cannot continue. we need the key path
                        return;
                    }
                }
            }
            if(SecurityGroupComb.SelectedValue != null)
                _selectedSecurityGroups = SecurityGroupComb.SelectedValue.ToString();
            if(ZoneComb.SelectedValue != null)
                _selectedZone = ZoneComb.SelectedValue.ToString(); ;

            launchThread = new Thread(new ThreadStart(launch));
            launchThread.SetApartmentState(ApartmentState.STA);
            launchThread.Start();
            enableProgressBar();
        }

        private void KeyPairComb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            if (cb != null)
            {
                if (cb.SelectedItem != null)
                {
                    string keyFile = _dashboard.awsConfig.getKeyFilePath(cb.SelectedItem.ToString());
                    if (string.IsNullOrEmpty(keyFile) == true ||
                        File.Exists(keyFile) == false)
                    {
                        KeyFileInputDlg kfInput = new KeyFileInputDlg(cb.SelectedItem.ToString(), _dashboard);
                        kfInput.ShowDialog();
                    }
                }
            }
        }
    }
}