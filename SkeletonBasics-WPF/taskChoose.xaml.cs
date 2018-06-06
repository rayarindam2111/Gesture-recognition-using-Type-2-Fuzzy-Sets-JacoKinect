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

    public enum MODE { DATASET_TRAIN, JACO_TASK_PLANNING, RUN_SYSTEM, PAUSE };


    /// <summary>
    /// Interaction logic for taskChoose.xaml
    /// </summary>
    public partial class taskChoose : Window
    {
        Log Logger;
        MainWindow mainWindow;
        MODE state;
        public taskChoose()
        {
            InitializeComponent();
            DoTasks();
        }

        private void DoTasks()
        {
            state = MODE.PAUSE;
            Logger = new Log();
            mainWindow = new MainWindow(Logger);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Logger.Show();
            Logger.addLog("Mode Select Window Loaded");
            mainWindow.Show();
            setMode();
        }

        private void btnModeApply_Click(object sender, RoutedEventArgs e)
        {
            setMode();
        }

        private void setMode()
        {
            if (rdoDataset.IsChecked == true)
            {
                state = MODE.DATASET_TRAIN;
                Logger.addLog("==========================");
                Logger.addLog("ENTERED Dataset Train MODE");
                Logger.addLog("==========================");
                mainWindow.updateMODE(state);
            }
            else if (rdoJacoTrain.IsChecked == true)
            {
                state = MODE.JACO_TASK_PLANNING;
                Logger.addLog("==========================");
                Logger.addLog("ENTERED Jaco Task Planning MODE");
                Logger.addLog("==========================");
                mainWindow.updateMODE(state);
            }
            else if (rdorunSystem.IsChecked == true)
            {
                state = MODE.RUN_SYSTEM;
                Logger.addLog("==========================");
                Logger.addLog("ENTERED System Run MODE");
                Logger.addLog("==========================");
                mainWindow.updateMODE(state);
            }
            else if (rdoPause.IsChecked == true)
            {
                state = MODE.PAUSE;
                Logger.addLog("==========================");
                Logger.addLog("ENTERED Pause MODE");
                Logger.addLog("==========================");
                mainWindow.updateMODE(state);
            }
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            Logger.destroywindow = true;
            mainWindow.destroywindow = true;
            mainWindow.Close();
            Logger.Close();
        }
    }
}
