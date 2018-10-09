using System;
using System.Windows.Forms;

namespace Middleware_Run
{

    static class Program
    {
        /// The main entry point for the application.
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Show the system tray icon.					
            using (ProcessIcon process_icon = new ProcessIcon())
            {
                process_icon.Display();
                // Make sure the application runs!
                Application.Run();
            }
        }
    }
}