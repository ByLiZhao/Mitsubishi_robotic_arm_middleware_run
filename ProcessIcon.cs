using System;
using System.Diagnostics;
using System.Windows.Forms;
using Middleware_Run.Properties;
using System.Collections.Generic;

namespace Middleware_Run
{
    /// <summary>
    /// 
    /// </summary>
    class ProcessIcon : IDisposable
    {
        /// <summary>
        /// The NotifyIcon object.
        /// </summary>
        private NotifyIcon notify_icon;

        private middleware middleware_ = new middleware();

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessIcon"/> class.
        /// </summary>
        public ProcessIcon()
        {
            // Instantiate the NotifyIcon object.
            notify_icon = new NotifyIcon();
            int x = middleware_.init_middleware_async().Result;
            x = middleware_.init_active_core_async().Result;
        }

        /// <summary>
        /// Displays the icon in the system tray.
        /// </summary>
        public void Display()
        {
            // Put the icon in the system tray and allow it react to mouse clicks.			
            notify_icon.MouseClick += new MouseEventHandler(on_MouseClick);
            notify_icon.Icon = Resources.Middleware_Run;
            notify_icon.Text = "Mitsubishi Robotics Middleware";
            notify_icon.Visible = true;

            // Attach a context menu.
            notify_icon.ContextMenuStrip = new ContextMenus().Create();
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        public void Dispose()
        {
            middleware_.Dispose();
            // When the application closes, this will remove the icon from the system tray immediately.
            notify_icon.Dispose();
        }

        /// <summary>
        /// Handles the MouseClick event of the ni control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Forms.MouseEventArgs"/> instance containing the event data.</param>
        void on_MouseClick(object sender, MouseEventArgs e)
        {
            // Handle mouse button clicks.
            if (e.Button == MouseButtons.Left)
            {
                DialogResult rest = MessageBox.Show(
                    "Shutdown all robots immediately?",
                    "Emergency stop",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                    );
                if (rest == DialogResult.Yes)
                {
                    int x = middleware_.Servo_off_async().Result;
                }
            }
        }
    }
}