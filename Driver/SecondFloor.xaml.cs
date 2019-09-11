using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace DemoDriver
{
    /// <summary>
    /// SecondFloor.xaml 的交互逻辑
    /// </summary>
    public partial class SecondFloor : UserControl
    {
        //DAService da;
        public SecondFloor()
        {
            InitializeComponent();
            Init();
        }

        public void Init()
        {
            //this.controlCollect.Visibility = Visibility.Collapsed;
            this.controlCollect.Visibility = Visibility.Visible;
            //da = new DAService();
            //this.topText.SetBinding(TextBox.TextProperty, new Binding("FloatTag") { Source = da["test4"], Mode = BindingMode.OneWay });
            //this.rightText.SetBinding(TextBox.TextProperty, new Binding("BoolTag") { Source = da["test2"], Mode = BindingMode.OneWay });
            //this.testButton.SetBinding(Button.ContentProperty, new Binding(".") { Source = da["test4"].Description, Mode=BindingMode.OneTime});
        }
    }
}
