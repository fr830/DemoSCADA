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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Globalization;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace ControlLibrary
{
    public partial class Animation : UserControl, INotifyPropertyChanged
    {
        List<AnimationButton> btnList = new List<AnimationButton>(200);
        private bool bLoaded = false;

        public Animation()
        {
            InitializeComponent();
            this.Loaded += UserControlLoaded;
        }

        private void UserControlLoaded(object sender, RoutedEventArgs e)
        {
            if (!bLoaded)
            {
                InitDistance();
                OnPropertyChanged(Binding.IndexerName);
                bLoaded = true;
            }

        }

        private void btnTest(object sender, RoutedEventArgs e)
        {

            AnimationButton bt = sender as AnimationButton;
            int iRet = HaveNeighborShow(bt);


            if (iRet == 0)
            {
                return;
            }

            if (iRet == 2)
            {
                DoubleAnimation daX = new DoubleAnimation();
                DoubleAnimation daY = new DoubleAnimation();
                daX.To = -bt.DiffDistance.X;
                daY.To = -bt.DiffDistance.Y;
                Duration duration = new Duration(TimeSpan.FromMilliseconds(500));
                daX.Duration = duration;
                daY.Duration = duration;
                this.tt.BeginAnimation(TranslateTransform.XProperty, daX);
                this.tt.BeginAnimation(TranslateTransform.YProperty, daY);
            }
            else
            {
                if (bt != null && bt.IsVisible && (bt.Name != "RobotCar"))
                {
                    DoubleAnimationUsingKeyFrames dakX = new DoubleAnimationUsingKeyFrames();
                    DoubleAnimationUsingKeyFrames dakY = new DoubleAnimationUsingKeyFrames();

                    LinearDoubleKeyFrame x_kf_1 = new LinearDoubleKeyFrame();
                    LinearDoubleKeyFrame x_kf_2 = new LinearDoubleKeyFrame();
                    LinearDoubleKeyFrame x_kf_3 = new LinearDoubleKeyFrame();

                    LinearDoubleKeyFrame y_kf_1 = new LinearDoubleKeyFrame();
                    LinearDoubleKeyFrame y_kf_2 = new LinearDoubleKeyFrame();
                    LinearDoubleKeyFrame y_kf_3 = new LinearDoubleKeyFrame();

                    dakX.Duration = new Duration(TimeSpan.FromMilliseconds(2000));
                    dakY.Duration = new Duration(TimeSpan.FromMilliseconds(2000));

                    x_kf_1.KeyTime = KeyTime.FromPercent(0.33);
                    x_kf_1.Value = 0D;

                    x_kf_2.KeyTime = KeyTime.FromPercent(0.66);
                    x_kf_2.Value = 0D;

                    x_kf_3.KeyTime = KeyTime.FromPercent(1.0);
                    x_kf_3.Value = -bt.DiffDistance.X;

                    dakX.KeyFrames.Add(x_kf_1);
                    dakX.KeyFrames.Add(x_kf_2);
                    dakX.KeyFrames.Add(x_kf_3);

                    y_kf_1.KeyTime = KeyTime.FromPercent(0.33);
                    y_kf_1.Value = -RobotCar.DiffDistance.Y;

                    y_kf_2.KeyTime = KeyTime.FromPercent(0.66);
                    y_kf_2.Value = -bt.DiffDistance.Y;

                    y_kf_3.KeyTime = KeyTime.FromPercent(1.0);
                    y_kf_3.Value = -bt.DiffDistance.Y;

                    dakY.KeyFrames.Add(y_kf_1);
                    dakY.KeyFrames.Add(y_kf_2);
                    dakY.KeyFrames.Add(y_kf_3);

                    this.tt.BeginAnimation(TranslateTransform.XProperty, dakX);
                    this.tt.BeginAnimation(TranslateTransform.YProperty, dakY);
                }
            }

            bt.Visibility = Visibility.Hidden;
            RobotCar.DiffDistance = bt.DiffDistance;

            OnPropertyChanged(Binding.IndexerName);
        }

        public void InitDistance()
        {
            foreach (AnimationButton bt in FindVisualChildren<AnimationButton>(this.container))
            {
                bt.Click += btnTest;
                Point p = RobotCar.TranslatePoint(new Point(), bt);
                bt.DiffDistance = p;
                btnList.Add(bt);
            }
        }

        private int HaveNeighborShow(AnimationButton _bt)
        {
            if (!_bt.IsVisible)
            {
                return 0;
            }

            if (_bt.Name.Contains("_1L") || _bt.Name.Contains("_1R") || _bt.Name.Contains("_1T") || _bt.Name.Contains("_1Z"))
            {
                return 1;
            }

            if ((Math.Abs(_bt.DiffDistance.X - RobotCar.DiffDistance.X) <= 36) && (_bt.DiffDistance.Y == RobotCar.DiffDistance.Y))
            {
                return 2;//返回2 代表旁边的就是可以移过去的，路径不要改变；
            }

            string pattern = @"(\d*)_(\d*)";
            string nextName = _bt.Name;
            int index;
            foreach (Match match in Regex.Matches(_bt.Name, pattern))
            {
                string[] newStrs = Regex.Split(match.Value, "_");
                int.TryParse(newStrs[1], out index);
                nextName = nextName.Replace(match.Value, newStrs[0] + "_" + (index - 1).ToString());
            }

            AnimationButton btNext = btnList.Find(e => (e.Name == nextName));

            if (btNext.IsVisible)
            {
                return 0;
            }

            return 1;
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        public int this[int index]
        {
            get
            {
                if (index <= 0 || index > 20)
                {
                    return -1;
                }
                else
                {
                    string line = "index" + index.ToString() + "_";
                    IEnumerable<AnimationButton> lineList = btnList.Where(e => (e.Name.Contains(line) && e.IsVisible));
                    return lineList.Count();
                }
            }
        }

        public int this[string index]
        {
            get
            {
                if (string.IsNullOrEmpty(index))
                {
                    return -1;
                }
                else
                {
                    IEnumerable<AnimationButton> lineList = btnList.Where(e => (e.Name.Contains(index.ToUpper()) && e.IsVisible));
                    return lineList.Count();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
