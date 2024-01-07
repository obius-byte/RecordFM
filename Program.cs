using System.Diagnostics;

namespace Record
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var currentProcess = Process.GetCurrentProcess();
            var isAlreadyRunning = Process.GetProcessesByName(currentProcess.ProcessName)
                .Any(p => p.Id != currentProcess.Id && p.MainModule.FileName == currentProcess.MainModule.FileName);

            if (isAlreadyRunning)
            {
                MessageBox.Show("Application is already running!", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}