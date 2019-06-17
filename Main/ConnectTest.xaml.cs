using System;
using System.Collections.Generic;
using System.Linq;
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
        }

        public static readonly DependencyProperty IsConnectedProperty;

        static ConnectTest()
        {
            IsConnectedProperty = DependencyProperty.Register("IsConnected", typeof(bool), typeof(ConnectTest), new PropertyMetadata((bool)false));
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
    }
}
