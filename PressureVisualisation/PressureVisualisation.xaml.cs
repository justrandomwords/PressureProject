using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Reflection.Emit;
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
using System.Drawing;
using System.Globalization;

namespace PressureVisualisation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private void InitializeSerialPort(string portName, int baudRate)
        {
            serialPort = new SerialPort(portName, baudRate);

            serialPort.Parity = Parity.None;
            serialPort.DataBits = 8;
            serialPort.StopBits = StopBits.One;
            serialPort.Handshake = Handshake.None;

            serialPort.DataReceived += SerialPort_DataReceived;
        }

        public MainWindow()
        {
            InitializeComponent();

            foreach (var item in SerialPort.GetPortNames())
                NameSerialport.Items.Add(item);

            NameSerialport.SelectedIndex = 0;

            NameSerialport.DropDownOpened += NameSerialport_DropDownOpened;
            NameSerialport.DropDownClosed += NameSerialport_DropDownClosed;

            try { InitializeSerialPort(NameSerialport.Text, int.Parse(ValueBaudrate.Text)); }
            catch (Exception) 
            { NotFoundPorts(); }
            


            ValueBaudrate.TextChanged += NameSerialport_TextChanged;
        }


        SerialPort serialPort;
        string selectedPort;

        bool isLightTheme = true;
        bool isNewton = true;


        private void ChangeUnits(string value)
        {
            isNewton = isNewton ? false : true;

            StringBuilder rezult = new StringBuilder();
            string[] splitValue = value.Split();

            try
            {
                rezult.Append(Math.Round(double.Parse(splitValue[0]) / 9.8, 2).ToString());
            }
            catch (Exception)
            {
                rezult.Append(splitValue[0]);
            }

            if (isNewton)
                rezult.Append(" N");
            else
                rezult.Append(" kg");

            outputLabel.Content = rezult;
        }

        private void NotFoundPorts()
        {
            NameSerialport.Items.Insert(0, "not found");
            NameSerialport.SelectedIndex = 0;
            NameSerialport.IsTextSearchEnabled = false;
        }

        private void SetDefaultOutputLabel(string value)
        {
            if (isNewton)
                outputLabel.Content = $"{value} N";
            else
                outputLabel.Content = $"{value} kg";
        }

        private void OpenPort(SerialPort port)
        {
            startStopButton.Content = "Stop";

            SetDefaultOutputLabel("0.00");
            outputLabel.Foreground = (SolidColorBrush)Application.Current.Resources["StartOutputText"];
            outputLabel.BorderBrush = (SolidColorBrush)Application.Current.Resources["StartOutputText"];
            port.Open();
        }

        private void ClosePort(SerialPort port)
        {
            startStopButton.Content = "Start";

            SetDefaultOutputLabel("XX");
            outputLabel.Foreground = (SolidColorBrush)Application.Current.Resources["MainText"];
            outputLabel.BorderBrush = (SolidColorBrush)Application.Current.Resources["MainText"];
            port.Close();
        }

        private void NameSerialport_DropDownOpened(object sender, EventArgs e)
        {
            if(NameSerialport.Text != "" && NameSerialport.Text != "not found")
                selectedPort = NameSerialport.Text;

            NameSerialport.Items.Clear();

            foreach (var item in SerialPort.GetPortNames())
                NameSerialport.Items.Add(item);
        }

        private void NameSerialport_DropDownClosed(object sender, EventArgs e)
        {
            if (serialPort != null && serialPort.IsOpen)
                ClosePort(serialPort);

            if (NameSerialport.Items.Count == 0)
            {
                NotFoundPorts();
            }
            else
            {
                if (NameSerialport.SelectedItem == null)
                    NameSerialport.SelectedIndex = int.Parse(selectedPort[3].ToString()) - 1;

                InitializeSerialPort(NameSerialport.Text, int.Parse(ValueBaudrate.Text));
            }
        }

        private void NameSerialport_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (serialPort != null && serialPort.IsOpen)
                ClosePort(serialPort);

            InitializeSerialPort(NameSerialport.Text, int.Parse(ValueBaudrate.Text));
        }


        public SolidColorBrush CalculateIntermediateColor(Color color1, Color color2, double number)
        {
            number /= 255;

            byte red = (byte)(color1.R + (color2.R - color1.R) * number);
            byte green = (byte)(color1.G + (color2.G - color1.G) * number);
            byte blue = (byte)(color1.B + (color2.B - color1.B) * number);
            byte alpha = (byte)(color1.A + (color2.A - color1.A) * number);

            return new SolidColorBrush(Color.FromArgb(alpha, red, green, blue));
        }

        private SolidColorBrush ChangeColor(double number, int max, bool isGlow = false)
        {
            number /= max / 255.0;
            number = number >= 255 ? 254 : number;

            SolidColorBrush currentColor;

            if (!isGlow)
                currentColor = CalculateIntermediateColor(
                ((SolidColorBrush)Application.Current.Resources["StartOutputText"]).Color,
                ((SolidColorBrush)Application.Current.Resources["EndOutputText"]).Color,
                number);
            else
                currentColor = CalculateIntermediateColor(
                ((SolidColorBrush)Application.Current.Resources["StartOutputText"]).Color,
                ((SolidColorBrush)Application.Current.Resources["EndOutputTextGlow"]).Color,
                number);


            return currentColor;
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string dirtyData = serialPort.ReadExisting();
            string data = dirtyData.Split('\n')[0];

            Dispatcher.Invoke(() => 
            {
                if (isNewton)
                    outputLabel.Content = Math.Round(double.Parse(data.TrimEnd('\n'), CultureInfo.InvariantCulture) * 9.8, 2).ToString("F2").Replace(',', '.') + " N";
                else
                    outputLabel.Content = data.TrimEnd('\n') + " kg";

                outputLabel.Foreground = ChangeColor(double.Parse(data, CultureInfo.InvariantCulture), 5);
                outputLabel.BorderBrush = ChangeColor(double.Parse(data, CultureInfo.InvariantCulture), 5, true);
            });
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (serialPort.IsOpen)
                ClosePort(serialPort);
            else
                OpenPort(serialPort);
        }

        private void WindowControl_Highlighte(object sender, MouseEventArgs e)
        {
            Brush defaultButons = (SolidColorBrush)Application.Current.Resources["MainText"];
            Brush exitButton = (SolidColorBrush)Application.Current.Resources["HighlightedExitButton"];
            Brush othersButton = (SolidColorBrush)Application.Current.Resources["HighlightedOthersButton"];


            Rectangle currentRectangle;

            switch (((Button)sender).Name)
            {
                case "CollapseButton":
                    currentRectangle = (Rectangle)FindName("CollapseButtonLabel");
                    currentRectangle.Fill = currentRectangle.Fill == defaultButons ?
                            othersButton : defaultButons;
                    break;
                case "ExpandButton":
                    currentRectangle = (Rectangle)FindName("ExpandButtonLabel");
                    currentRectangle.Stroke = currentRectangle.Stroke == defaultButons ?
                            othersButton : defaultButons;
                    break;
                case "ExitButton":
                    ExitButtonLabel.Fill = ExitButtonLabel.Fill == defaultButons ?
                            exitButton : defaultButons;
                    break;
            }
        }

        private void WindowControl_Click(object sender, RoutedEventArgs e)
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

        private void ChangeTheme(string url)
        {
            ResourceDictionary newTheme = new ResourceDictionary();
            newTheme.Source = new Uri(url, UriKind.Relative);
            Application.Current.Resources.MergedDictionaries[0] = newTheme;

            try 
            {
                int value = int.Parse(outputLabel.Content.ToString().Split(' ')[0]);

                outputLabel.Foreground = ChangeColor(value, 1024); 
                outputLabel.BorderBrush = ChangeColor(value, 1024, true);
            }
            catch (Exception) 
            { 
                outputLabel.Foreground = (SolidColorBrush)Application.Current.Resources["MainText"];
                outputLabel.BorderBrush = (SolidColorBrush)Application.Current.Resources["MainText"];
            }

            CollapseButtonLabel.Fill = (SolidColorBrush)Application.Current.Resources["MainText"];
            ExitButtonLabel.Fill = (SolidColorBrush)Application.Current.Resources["MainText"];

            isLightTheme = !isLightTheme;
        }

        private void ChangeTheme(object sender, RoutedEventArgs e)
        {
            if (isLightTheme)
                ChangeTheme("Themes/MainDark.xaml");
            else
                ChangeTheme("Themes/MainLight.xaml");
        }

        private void ChangeUnits_ButtonClick(object sender, RoutedEventArgs e)
        {
            ChangeUnits(outputLabel.Content.ToString());
        }
    }
}
