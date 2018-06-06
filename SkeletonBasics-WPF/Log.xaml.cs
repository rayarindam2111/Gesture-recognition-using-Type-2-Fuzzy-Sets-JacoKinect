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
using System.Windows.Shapes;

namespace Microsoft.Samples.Kinect.SkeletonBasics
{
    /// <summary>
    /// Log window
    /// </summary>
    public partial class Log : Window
    {
        public bool destroywindow { get; set; }
        public Log()
        {
            InitializeComponent();
            destroywindow = false;
        }
       
        public void addLog(string text,bool error = false)
        {
            ListBoxItem k = new ListBoxItem();
            k.Content = text;
            if(error)
                k.Foreground = new SolidColorBrush(Color.FromRgb(0xBC,0x00,0x00));
            else
                k.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0x5F, 0x00));
            lstLog.Items.Add(k);
            lstLog.SelectedIndex = lstLog.Items.Count - 1;
            lstLog.ScrollIntoView(lstLog.SelectedItem);
        }

        private void logWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            lstLog.Width = logWindow.Width - 40;
            lstLog.Height = logWindow.Height - 50;
        }
        
        private void logWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = !destroywindow;
        }

        private void logWindow_Loaded(object sender, RoutedEventArgs e)
        {
            addLog("START OF EXECUTION");
            addLog("-------------------------");
            addLog("Log Window Loaded");
        }
    }
}
