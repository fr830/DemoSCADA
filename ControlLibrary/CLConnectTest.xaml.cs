using System;
using System.Collections.Generic;
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
        private List<DataForm> txtData = new List<DataForm>();
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
            
        }

        private void Btn_clcTxtSend(object sender, RoutedEventArgs e)
        {
            this.txtSend.Text = "";
        }

        private void Btn_clcTxtRcv(object sender, RoutedEventArgs e)
        {
            this.txtRcv.Text = "";
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

            iConnectType.GetRcvBufferEvent += DisPlayData;

            if (iConnectType.OpenConnect())
            {
                IsConnected = true;
            }
        }

        private void DisPlayData(DataForm dt)
        {
            txtData.Add(dt);
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
}
