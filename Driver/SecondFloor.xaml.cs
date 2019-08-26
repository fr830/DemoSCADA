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

namespace DemoDriver
{
    /// <summary>
    /// SecondFloor.xaml 的交互逻辑
    /// </summary>
    public partial class SecondFloor : UserControl
    {
        DAService da;
        public SecondFloor()
        {
            InitializeComponent();
            Init();
        }

        public void Init()
        {
            da = new DAService();
            this.topText.SetBinding(TextBox.TextProperty, new Binding("FloatTag") {Source = da["test4"], Mode=BindingMode.OneWay});
            this.rightText.SetBinding(TextBox.TextProperty, new Binding("BoolTag") {Source = da["test2"], Mode=BindingMode.OneWay});
            this.testButton.SetBinding(Button.ContentProperty,new Binding("StringTag") { Source = da["test6"],Mode=BindingMode.OneWay});
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            byte[] temp = { 65, 66 };
            da.SendBytes(temp);
        }
    }
}
