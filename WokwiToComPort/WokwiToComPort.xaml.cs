using CefSharp.DevTools.Page;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using HtmlAgilityPack;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using System.Xml;
using CefSharp;
using CefSharp.DevTools.CSS;
using CefSharp.DevTools.Accessibility;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using System.IO.Ports;
using System.ComponentModel;
using LiveCharts;
using LiveCharts.Defaults;
using System.Globalization;
using LiveCharts.Wpf.Charts.Base;
using LiveCharts.Wpf;

namespace WokwiToComPort
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.StateChanged += MainWindow_StateChanged;

            dataGetTimer = new DispatcherTimer();
            dataGetTimer.Interval = TimeSpan.FromMilliseconds(100);
            dataGetTimer.Tick += DataGetTimer_Tick;

            cleanMonitorTimer = new DispatcherTimer();
            cleanMonitorTimer.Interval = TimeSpan.FromMilliseconds(1000);
            cleanMonitorTimer.Tick += CleanMonitorTimer_Tick;

            try
            {
                OpenSerialPort(SerialPort.GetPortNames()[0], 9600);
                serialPort.Open();
            }
            catch (Exception) { }


            string[] serialPortNames = SerialPort.GetPortNames();
            foreach (var item in serialPortNames)
                ComPortChoose.Items.Add(item);
            ComPortChoose.SelectedIndex = 0;


            grafPointsList.AddRange(Enumerable.Repeat(0, 100));
        }



        private SerialPort serialPort;

        private List<int> grafPointsList = new List<int>();

        private DispatcherTimer dataGetTimer;
        private DispatcherTimer cleanMonitorTimer;


        private async void AddDataGraf(int value)
        {
            grafPointsList.Add(value);
            grafPointsList.RemoveAt(0);

            ChartValues<ObservablePoint> grafPoints = new ChartValues<ObservablePoint>();

            for (int i = 0; i < grafPointsList.Count; i++)
                grafPoints.Add(new ObservablePoint(i + 1, grafPointsList[i]));

            DataContext = new { grafPoints };
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                MainBorder.Margin = new Thickness(6, 6, 6, 36);
                MainBorder.CornerRadius = new CornerRadius(0);
            }
            else
            {
                MainBorder.Margin = new Thickness(0, 0, 0, 0);
                MainBorder.CornerRadius = (CornerRadius)FindResource("BorderCorner");
            }
        }

        private void WindowControlHighlighte(object sender, MouseEventArgs e)
        {
            Brush defaultColor = (SolidColorBrush)Application.Current.Resources["MainButtonColor"];
            Brush exitButton = (SolidColorBrush)Application.Current.Resources["HighlightedExitButton"];
            Brush othersButton = (SolidColorBrush)Application.Current.Resources["HighlightedOthersButton"];


            Rectangle currentRectangle;

            switch (((Button)sender).Name)
            {
                case "CollapseButton":
                    currentRectangle = (Rectangle)FindName("CollapseButtonLabel");
                    currentRectangle.Fill = currentRectangle.Fill == defaultColor ?
                            othersButton : defaultColor;
                    break;
                case "ExpandButton":
                    currentRectangle = (Rectangle)FindName("ExpandButtonLabel");
                    currentRectangle.Stroke = currentRectangle.Stroke == defaultColor ?
                            othersButton : defaultColor;
                    break;
                case "ExitButton":
                    ExitButtonLabel.Fill = ExitButtonLabel.Fill == defaultColor ?
                            exitButton : defaultColor;
                    break;
                default:
                    throw new Exception("WindowControlsClick run to default");
            }
        }

        private void WindowControlClick(object sender, RoutedEventArgs e)
        {
            switch (((Button)sender).Name)
            {
                case "CollapseButton":
                    this.WindowState = WindowState.Minimized;
                    break;
                case "ExpandButton":
                    this.WindowState = this.WindowState == WindowState.Maximized ?
                        WindowState.Normal : WindowState.Maximized;
                    break;
                case "ExitButton":
                    this.Close();
                    break;
                default:
                    throw new Exception("WindowControlsClick run to default");
            }
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }


        private HtmlDocument GetSiteCode()
        {
            string html = "null";

            abb.GetSourceAsync().ContinueWith(taskHtml =>
            {
                if (!taskHtml.IsFaulted)
                    html = taskHtml.Result;
            }).Wait();

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);

            return document;
        }

        private string GetMonitorText()
        {
            HtmlDocument document = GetSiteCode();

            HtmlNode preNode = document.DocumentNode.SelectSingleNode("//pre[@class='notranslate']");

            string value = "null";

            if (preNode != null)
                value = preNode.InnerHtml;

            return value;
        }

        private async void CleanMonitor()
        {
            string script = @"var buttons = document.querySelectorAll('.serial-monitor_serialControls__KSZx8 button'); 
                            if (buttons.length >= 3) buttons[2].click();";

            await abb.EvaluateScriptAsync(script);
        }

        private void AnimateMenuGrid(double from, double to, double time)
        {
            DoubleAnimation animation = new DoubleAnimation();
            animation.From = from;
            animation.To = to;
            animation.Duration = TimeSpan.FromSeconds(time);


            TranslateTransform translateTransform = new TranslateTransform();
            InfoMenu.RenderTransform = translateTransform;

            // Встановлюємо анімацію для властивості Y TranslateTransform
            translateTransform.BeginAnimation(TranslateTransform.YProperty, animation);
        }

        bool isActiveMenu = false;

        private void DataGetTimer_Tick(object sender, EventArgs e)
        {
            if (serialPort.IsOpen)
            {
                int a = 1 + 4;
            }

            try
            {
                string[] values = GetMonitorText().Split('\n');

                //SerialPort serialPort = new SerialPort();
                string value = values[values.Length - 2];
                mainButton.Content = value;
                serialPort.WriteLine(value);
            }
            catch (Exception exception)
            {
                //MessageBox.Show(exception.Message);
            }
        }

        private void CleanMonitorTimer_Tick(object sender, EventArgs e)
        {
            CleanMonitor();

            try
            {
                AddDataGraf((int)double.Parse(mainButton.Content.ToString().Split()[0], CultureInfo.InvariantCulture));
            }
            catch (Exception) { }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dataGetTimer.Start();
            cleanMonitorTimer.Start();
        }

        private void OpenSerialPort(string serialPortName, int baudRate)
        {
            serialPort = new SerialPort(serialPortName, baudRate);
            serialPort.Open();
        }

        private void ComPortChoose_DropDownOpened(object sender, EventArgs e)
        {
            ComPortChoose.Items.Clear();

            string[] serialPortNames = SerialPort.GetPortNames();

            foreach (var item in serialPortNames)
                ComPortChoose.Items.Add(item);
        }

        private void ComPortChoose_DropDownClosed(object sender, EventArgs e)
        {
            if (serialPort != null && serialPort.IsOpen)
                serialPort.Close();

            try
            {
                OpenSerialPort(ComPortChoose.SelectedItem.ToString(), int.Parse(BaudrateChange.Text));
            }
            catch (Exception) { }


            //datat.Values = new ChartValues<double> { 1, 2, 3, 4, 5 };
        }

        private void BaudrateChange_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (serialPort != null && serialPort.IsOpen)
                serialPort.Close();

            try
            {
                OpenSerialPort(ComPortChoose.SelectedItem.ToString(), int.Parse(BaudrateChange.Text));
            }
            catch (Exception) { }
        }

        private void TurnGraf_Click(object sender, RoutedEventArgs e)
        {
            if (TestBorder.Height == 0)
            {
                TestBorder.Height = 260;
                ((Button)sender).Content = "Вимкнути графік";
            }
            else
            {
                TestBorder.Height = 0;
                ((Button)sender).Content = "Ввімкнути графік";
            }
        }
    }
}