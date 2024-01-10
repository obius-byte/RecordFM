using System.Diagnostics;
using System.Windows;

namespace Record
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var currentProcess = Process.GetCurrentProcess();
            var isAlreadyRunning = Process.GetProcessesByName(currentProcess.ProcessName)
                .Any(process => process.Id != currentProcess.Id && process.ProcessName == currentProcess.ProcessName);

            if (isAlreadyRunning)
            {
                MessageBox.Show("Application is already running!");
                Current.Shutdown();
            }
        }
    }
}
