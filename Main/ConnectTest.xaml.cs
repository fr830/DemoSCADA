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

namespace Main
{
    /// <summary>
    /// ConnectTest.xaml 的交互逻辑
    /// </summary>
    public partial class ConnectTest : UserControl
    {
        public ConnectTest()
        {
            InitializeComponent();
            GetIP();
        }

        public static readonly RoutedCommand SendCommand;
        public static readonly DependencyProperty IsConnectedProperty;


        static ConnectTest()
        {
            IsConnectedProperty = DependencyProperty.Register("IsConnected", typeof(bool), typeof(ConnectTest), new PropertyMetadata((bool)false));
            SendCommand = new RoutedCommand(); 
        }

        public  bool IsConnected
        {
            get { return (bool)GetValue(IsConnectedProperty); }
            set { SetValue(IsConnectedProperty, value); }
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
                IsConnected = false;
            }
            else
            {
                IsConnected = true;
            }
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

        private  void Send_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            MessageBox.Show("Custom Command Executed");
        }

        private void GetIP()
        {
            List<string> ipList = new List<string>();
            string hostName = Dns.GetHostName();
            System.Net.IPAddress[] addressList = Dns.GetHostAddresses(hostName);
            foreach (IPAddress ip in addressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    ipList.Add(ip.ToString());
            }

            ipList.Add("127.0.0.1");

            cmbIpAddress.ItemsSource = ipList;
            cmbIpAddress.SelectedIndex = 0;
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
