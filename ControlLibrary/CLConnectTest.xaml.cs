﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

namespace ControlLibrary
{
    /// <summary>
    /// CLConnectTest.xaml 的交互逻辑
    /// </summary>
    public partial class CLConnectTest : UserControl
    {
        public CLConnectTest()
        {
            InitializeComponent();
            Init();
            GetIP();
        }

        public static readonly RoutedCommand SendCommand;
        public static readonly DependencyProperty IsConnectedProperty;
        IConnect iConnectType;
        private ObservableCollection<DataForm> rcvSendData = new ObservableCollection<DataForm>();

        private string _localIPAddress;
        public string LocalIPAddress
        {
            get { return _localIPAddress; }
            set { _localIPAddress = value; }
        }

        static CLConnectTest()
        {
            IsConnectedProperty = DependencyProperty.Register("IsConnected", typeof(bool), typeof(CLConnectTest), new PropertyMetadata((bool)false));
            SendCommand = new RoutedCommand();
        }

        public bool IsConnected
        {
            get { return (bool)GetValue(IsConnectedProperty); } 
            set { SetValue(IsConnectedProperty, value); }
        }

        private void Init()
        {
            listDisplayData.ItemsSource = rcvSendData;
        }

        private void Btn_clcTxtSend(object sender, RoutedEventArgs e)
        {
            this.txtSend.Text = "";
        }

        private void Btn_clcTxtRcv(object sender, RoutedEventArgs e)
        {
            rcvSendData.Clear();
        }

        private void Btn_openConnect(object sender, RoutedEventArgs e)
        {
            if (IsConnected)
            {
                if (iConnectType != null)
                    iConnectType.CloseConnect();
                IsConnected = false;
                return;
            }

            string strProtocol = cmbProtocolType.Text;
            string strIPAddress = cmbIpAddress.SelectedItem.ToString();

            int iPort;
            int.TryParse(hostPort.Text, out iPort);

            switch (strProtocol)
            {
                case "UDP":
                    iConnectType = new UDPConnect(strIPAddress, iPort);
                    break;
                default:
                    iConnectType = new UDPConnect(strIPAddress, iPort);
                    break;
            }

            if (iConnectType.OpenConnect())
            {
                IsConnected = true;
            }
            else
            {
                IsConnected = false;
                return;
            }

            iConnectType.GetRcvBufferEvent += DisPlayDataAsync;
        }

        private void DisPlayDataAsync(DataForm dt)
        {
            this.Dispatcher.BeginInvoke(new Action(() => rcvSendData.Add(new DataForm { Buffer = dt.Buffer, Length = dt.Length, IPPort ="["+dt.DTime+"]# RECV FROM " + dt.IPPort, DTime = dt.DTime, IsRS = dt.IsRS })));
        }

        private void PartStringToIPPort(string s,out string ip, out int port)
        {
            string[] sArray = s.Split(new char[2] { ':','：' });
            ip = sArray[0];
            int.TryParse(sArray[1],out port);
        }

        private void Send_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (txtSend.Text != "")
            {
                e.CanExecute = true;
            }
            else
            {
                e.CanExecute = false;
            }

        }

        private void Send_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            if (IsConnected)
            {
                if (cmbProtocolType.Text == "UDP")
                {
                    string strTemp = cmbRemoteHost.Text;
                    string strRemoteIP;
                    int iRemotePort;
                    PartStringToIPPort(strTemp, out strRemoteIP, out iRemotePort);

                    iConnectType.RemoteIPAddress = strRemoteIP;
                    iConnectType.RemotePort = iRemotePort;
                }

                iConnectType.SendData(Encoding.UTF8.GetBytes(txtSend.Text));
                rcvSendData.Add(new DataForm { Buffer =txtSend.Text, Length = txtSend.Text.Length, IPPort = "[" + DateTime.Now + "]# SEND TO " + cmbRemoteHost.Text, DTime =DateTime.Now, IsRS = true });
            }
        }

        private void GetIP()
        {
            List<string> ipList = new List<string>();
            List<string> remoteIPList = new List<string>();
            string hostName = Dns.GetHostName();
            System.Net.IPAddress[] addressList = Dns.GetHostAddresses(hostName);
            foreach (IPAddress ip in addressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ipList.Add(ip.ToString());
                    _localIPAddress = ip.ToString();
                    remoteIPList.Add(ip.ToString() + " :8081");
                }

            }

            ipList.Add("127.0.0.1");
            remoteIPList.Add("127.0.0.1 :8081");
            cmbIpAddress.ItemsSource = ipList;
            cmbIpAddress.SelectedIndex = 0;
            cmbRemoteHost.ItemsSource = remoteIPList;
        }
    }

    public class CmbSelectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return string.Empty;
            }

            ComboBoxItem cbi = (ComboBoxItem)value;

            return cbi.Content.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ListBoxWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return string.Empty;
            }

           return double.Parse(string.Format("{0}", value)) -10;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IsRcvSendToAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return string.Empty;
            }

            bool bIsRcvSend = (bool)value;

            if (bIsRcvSend)
            {
                return "Left";
            }

            return "Right";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IsRcvSendToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return string.Empty;
            }

            bool bIsRcvSend = (bool)value;

            if (bIsRcvSend)
            {
                return "#0000FF";
            }

            return "#008000";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
