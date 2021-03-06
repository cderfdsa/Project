﻿using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DesktopWidget_WPF
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        [DllImport("user32.dll", SetLastError = true)]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpWindowClass, string lpWindowName);
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);
        const int GWL_HWNDPARENT = -8;
        [DllImport("user32.dll")]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        DispatcherTimer timer = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Init();
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Start();
            var handle = new WindowInteropHelper(Application.Current.MainWindow).Handle;
            IntPtr hprog = FindWindowEx(
                FindWindowEx(
                    FindWindow("Progman", "Program Manager"),
                    IntPtr.Zero, "SHELLDLL_DefView", ""
                ),
                IntPtr.Zero, "SysListView32", "FolderView"
            );
            SetWindowLong(handle, GWL_HWNDPARENT, hprog);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
        }

        const int SE_SHUTDOWN_PRIVILEGE = 0x13;
        const int WM_WINDOWPOSCHANGED = 0x47;
        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        bool inProc = false;
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_WINDOWPOSCHANGED)
            {
                if (inProc)
                    return IntPtr.Zero;
                inProc = true;
                var handle = new WindowInteropHelper(Application.Current.MainWindow).Handle;
                SetWindowPos(handle, 1, 0, 0, 0, 0, SE_SHUTDOWN_PRIVILEGE);
                inProc = false;
            }
            return IntPtr.Zero;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            Point position = e.GetPosition(this);
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (position.X >= 0 && position.X < this.ActualWidth && position.Y >= 0 && position.Y < this.ActualHeight)
                {
                    this.DragMove();
                }
            }
        }

        string[] GetTimeInfo()
        {
            var strArr = new string[3];
            var dtStart = DateTime.Now.AddMilliseconds(0 - Environment.TickCount);
            strArr[0] = dtStart.ToString("HH:mm:ss");
            var tsRemain = dtStart.AddHours(9.5) - DateTime.Now;
            strArr[1] = tsRemain.TotalMilliseconds > 0 ? tsRemain.ToString().Split('.')[0] : "00:00:00";
            strArr[2] = tsRemain.TotalMilliseconds < 0 ? tsRemain.ToString().Replace("-", "").Split('.')[0] : "00:00:00";
            return strArr;
        }

        void timer_Tick(object sender, EventArgs e)
        {
            bText = new FormattedText(string.Format(
                "StartTime {0}\r\n\r\nRemainingTime {1}\r\n\r\nOverTime {2}", GetTimeInfo()), CultureInfo.CurrentCulture,
                       FlowDirection.LeftToRight, this.bFont, 21, Brushes.Black);
            this.InvalidateVisual();
        }

        Brush bBrush;
        ImageSource imgSource;
        Typeface bFont;
        FormattedText bText;
        Point txtLocation;
        Rect rectBk, rectImg;

        void Init()
        {
            bBrush = new SolidColorBrush(Color.FromRgb(147, 174, 97));

            imgSource = new BitmapImage(new Uri("pack://application:,,,/Res/glass.png"));

            var font = this.FindResource("LEDFont") as FontFamily;

            this.bFont = new Typeface(font,
                new System.Windows.FontStyle(),
                new System.Windows.FontWeight(),
                new System.Windows.FontStretch());

            txtLocation = new Point(45, 60);
            rectBk = new Rect(32, 32, this.Width - 90, this.Height - 102);
            rectImg = new Rect(0, 0, this.Width, this.Height);
            this.Background = null;
        }

        protected override void OnRender(DrawingContext dc)
        {
            dc.DrawRectangle(bBrush, null, rectBk);
            dc.DrawText(bText, txtLocation);
            dc.DrawImage(imgSource, rectImg);
        }
    }
}
