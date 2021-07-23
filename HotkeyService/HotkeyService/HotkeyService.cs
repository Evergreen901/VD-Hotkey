using murrayju.ProcessExtensions;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Timers;

namespace HotkeyService
{
    public partial class HotkeyService : ServiceBase
    {
        Timer timer = new Timer();
        public HotkeyService()
        {
            InitializeComponent();
        }

        public void runHotkeyApp()
        {
            string modulePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            Process[] pname = Process.GetProcessesByName("hotkeyapp");
            if (pname.Length == 0)
            {
                ProcessExtensions.StartProcessAsCurrentUser(modulePath + "\\HotkeyApp.exe");
                WriteToFile(modulePath + "\\HotkeyApp.exe");
            }
        }

        protected override void OnStart(string[] args)
        {
            WriteToFile("Service is started at " + DateTime.Now);

            runHotkeyApp();

            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            timer.Interval = 5000; //number in milisecinds  
            timer.Enabled = true;
        }

        protected override void OnStop()
        {
            WriteToFile("Service is stopped at " + DateTime.Now);
        }

        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            WriteToFile("Service is recall at " + DateTime.Now);
            runHotkeyApp();
        }
        public void WriteToFile(string Message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(filepath))
            {
                // Create a file to write to.   
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }
    }
}
