using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using System.Reflection;

namespace HotkeyApp
{
    public partial class HotkeyApp : Form
    {
        public struct HotkeyData {
            // 1: remap, 2: functional, 3: break
            public int type;
            // functional delay
            public int delaySec;
            // remap
            public string keyString;
            // keybind info  ex: Ctrl+Alt+Shift+0
            public string keyBind;
            // determines if hotkey can stop current loop
            public bool bCanStopLoop { get; set; }

            public HotkeyData(string bind, int t, int d = 0, string key = "")
            {
                type = t;
                delaySec = d;
                keyString = key;
                keyBind = bind;
                bCanStopLoop = false;
            }

            public void setStopFlag()
            {
                bCanStopLoop = true;
            }
        }

        public struct ThreadParam
        {
            public int delaySec;
            public HotkeyApp myClass;

            public ThreadParam(int d, HotkeyApp obj)
            {
                delaySec = d;
                myClass = obj;
            }

        }

        int hotkeyID = 0;
        int vdCount = 0; // Virtual Desktop Count
        public static Thread loopThread = null;
        public static bool isBreak = false;

        public static List<int> switchList = new List<int>();
        Dictionary<int, HotkeyData> hotkeyDict = new Dictionary<int, HotkeyData>();        

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);

        public bool RegisterHotkey(string hotkeyString, int type) 
        {
            string[] subs = hotkeyString.Split(',');
            string keyBind = subs[0];
            string keyOccur = type < 3 ? subs[1] : "";
            int key = 0;
            int modifier = 0;
            int delaySec = 0;
            subs = keyBind.Split('+');

            foreach (string sub in subs)
            {
                string s = sub.ToLower();

                if (s.CompareTo("alt") == 0)
                    modifier |= 1;
                else if (s.CompareTo("ctrl") == 0)
                    modifier |= 2;
                else if (s.CompareTo("shift") == 0)
                    modifier |= 4;
                else if (s.Length == 1)
                {
                    if (s[0] >= '0' && s[0] <= '9')
                        key = (int)(Keys.D0 + s[0] - '0');
                    if (s[0] >= 'a' && s[0] <= 'z')
                        key = (int)(Keys.A + s[0] - 'a');
                }
            }

            if (type == 2)
            {
                delaySec = Int32.Parse(keyOccur);
            }

            bool bRet = false;
            
            if (type == 1)
            {
                bRet = RegisterHotKey(this.Handle, hotkeyID, modifier | 0x4000, key);
                hotkeyDict.Add(hotkeyID++, new HotkeyData(keyBind, 1, 0, keyOccur));                
            }
            else if (type == 2)
            {
                bRet = RegisterHotKey(this.Handle, hotkeyID, modifier | 0x4000, key);
                hotkeyDict.Add(hotkeyID++, new HotkeyData(keyBind, 2, delaySec));                
            }
            else if (type == 3)
            {
                int i;
                bRet = true;
                for (i = 0; i < hotkeyDict.Count; i++)
                {
                    if (hotkeyDict[i].keyBind.Equals(keyBind))
                    {
                        HotkeyData temp = hotkeyDict[i];
                        temp.bCanStopLoop = true;
                        hotkeyDict[i] = temp;
                        break;
                    }
                }

                if (i == hotkeyDict.Count)
                {
                    bRet = RegisterHotKey(this.Handle, hotkeyID, modifier | 0x4000, key);
                    hotkeyDict.Add(hotkeyID++, new HotkeyData(keyBind, 3));
                }
            }
            return bRet;
        }

        public int GetCurrentDesktopID()
        {
            int nRet = -1;
            try
            {
                using (Process myProcess = new Process())
                {
                    myProcess.StartInfo.FileName = "vd.exe";
                    myProcess.StartInfo.Arguments = "-GetCurrentDesktop";
                    myProcess.StartInfo.UseShellExecute = false;
                    myProcess.StartInfo.CreateNoWindow = true;
                    myProcess.StartInfo.RedirectStandardOutput = true;
                    myProcess.Start();
                    myProcess.WaitForExit();
                    string output = myProcess.StandardOutput.ReadToEnd();

                    if (output.Contains("(desktop number "))
                    {
                        int pos = output.IndexOf("(desktop number ") + "(desktop number ".Length;
                        string temp = output.Substring(pos);

                        nRet = Int32.Parse(temp.Substring(0, temp.Length - 3));
                    }
                }
            }
            catch (Exception ee)
            {
                Console.WriteLine(ee.Message);
            }

            return nRet;
        }

        public void InitTrayIconMenu()
        {
            string modulePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            notifyIcon1.ContextMenuStrip = contextMenuStrip1;
            notifyIcon1.BalloonTipText = "HotkeyProgram is running.";
            notifyIcon1.BalloonTipTitle = "Hotkey App";
            notifyIcon1.Icon = new Icon(modulePath + "\\icons\\" + (GetCurrentDesktopID() + 1) + ".ico");
            notifyIcon1.ShowBalloonTip(1);
        }

        public void InitVirtualDesktops()
        {
            try
            {
                using (Process myProcess = new Process())
                {
                    myProcess.StartInfo.FileName = "vd.exe";
                    myProcess.StartInfo.Arguments = "/Count";
                    myProcess.StartInfo.UseShellExecute = false;
                    myProcess.StartInfo.CreateNoWindow = true;
                    myProcess.StartInfo.RedirectStandardOutput = true;
                    myProcess.Start();
                    myProcess.WaitForExit();
                    string output = myProcess.StandardOutput.ReadToEnd();

                    if (output.Contains("Count of desktops:"))
                    {
                        int currentVDCount = Int32.Parse(output.Substring("Count of desktops:".Length));

                        for (int i = 0; i < vdCount - currentVDCount; i++)
                        {
                            myProcess.StartInfo.FileName = "vd.exe";
                            myProcess.StartInfo.Arguments = "-new";
                            myProcess.StartInfo.UseShellExecute = false;
                            myProcess.StartInfo.CreateNoWindow = true;
                            myProcess.Start();
                        }

                        if (vdCount > currentVDCount)
                        {
                            textBox1.AppendText("Just " + (vdCount - currentVDCount) + " new virtual desktops added.\r\n");
                        }
                    }
                }
            }
            catch (Exception ee)
            {
                Console.WriteLine(ee.Message);
            }
        }
        public HotkeyApp()
        {
            InitializeComponent();

            string modulePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string path = modulePath + "\\config.ini";

            if (!File.Exists(path))
            {
                MessageBox.Show("Failed to read config file.");
                this.Close();
            }

            string[] readText = File.ReadAllLines(path);
            foreach (string s in readText)
            {
                string typeString = "";
                int type = -1;
                if (s.Contains("[VD_NUMBER]"))
                {
                    typeString = "[VD_NUMBER]";
                    type = 0;
                }
                else if (s.Contains("[REMAP]"))
                {
                    typeString = "[REMAP]";
                    type = 1;
                }
                else if (s.Contains("[FUNCTION]"))
                {
                    typeString = "[FUNCTION]";
                    type = 2;
                }
                else if (s.Contains("[BREAK]"))
                {
                    typeString = "[BREAK]";
                    type = 3;
                }
                else if (s.Contains("[SWITCH]"))
                {
                    switchList.Add(Int32.Parse(s.Substring("[SWITCH]Win+".Length)));
                }


                if (type < 0) continue;
                if (type > 0)
                {
                    if (RegisterHotkey(s.Substring(typeString.Length), type))
                        textBox1.AppendText(s + " added\r\n");
                    else
                        textBox1.AppendText(s + " failed\r\n");
                } 
                else
                {
                    vdCount = Int32.Parse(s.Substring(typeString.Length));
                }
            }

            textBox1.AppendText("\r\n");
            InitTrayIconMenu();
            InitVirtualDesktops();
        }

        public void SwitchDesktop(int index)
        {
            try
            {
                using (Process myProcess = new Process())
                {
                    myProcess.StartInfo.FileName = "vd.exe";
                    myProcess.StartInfo.Arguments = "\"-Switch: " + (index - 1) + "\"";
                    myProcess.StartInfo.UseShellExecute = false;
                    myProcess.StartInfo.CreateNoWindow = true;
                    myProcess.StartInfo.RedirectStandardOutput = true;
                    myProcess.Start();

                    string modulePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

                    if (index < 21)
                    {
                        notifyIcon1.Icon = new Icon(modulePath + "\\icons\\" + index + ".ico");
                    }
                    else
                    {
                        notifyIcon1.Icon = new Icon(modulePath + "\\icons\\20plus.ico");
                    }
                }
            }
            catch (Exception ee)
            {
                Console.WriteLine(ee.Message);
            }
        }

        public void printLog(string key)
        {
            string modulePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string log;
            log = DateTime.Now.ToString("yyyyMMdd HH:mm:ss.fff") + ": " + key + "\r\n";
            
            File.AppendAllText(modulePath + "\\hotkey.log", log);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0312)
            {
                int id = m.WParam.ToInt32();
                HotkeyData hkData = hotkeyDict[id];

                textBox1.AppendText(hkData.keyBind + " called\r\n");
                printLog(hkData.keyBind);

                if (hkData.type < 3)
                {
                    if (loopThread != null && hkData.bCanStopLoop)
                    {
                        textBox1.AppendText("Previous loop has aborted\r\n");
                        loopThread.Abort();
                        loopThread = null;
                    }
                }

                if (hkData.type == 1)
                {
                    string keyString = hkData.keyString;
                    string[] subs = keyString.Split('+');

                    if (subs[0].CompareTo("Win") == 0)
                    {
                        int vdID = Int32.Parse(subs[1]);
                        SwitchDesktop(vdID);
                    }
                }
                else if (hkData.type == 2)
                {
                    loopThread = new Thread(HotkeyApp.ThreadProc);
                    loopThread.Start(new ThreadParam(hkData.delaySec, this));
                }
                else if (hkData.type == 3)
                {
                    loopThread.Abort();
                }
            }

            base.WndProc(ref m);
        }

        public static void ThreadProc(object data)
        {
            ThreadParam param = (ThreadParam)data;
            int delaySec = param.delaySec;
            int i = 0;
            while (true)
            {
                param.myClass.SwitchDesktop(switchList[i]);
                Thread.Sleep(delaySec * 1000);

                if (++i == switchList.Count)
                    i = 0;
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void showHideToolStripMenuItem_Click(object sender, EventArgs e)
        {

            if (contextMenuStrip1.Items[0].Text.CompareTo("Hide") == 0)
            {
                contextMenuStrip1.Items[0].Text = "Show";
                Hide();
            }
            else
            {
                contextMenuStrip1.Items[0].Text = "Hide";
                Show();
            }
        }

        private void HotkeyApp_Load(object sender, EventArgs e)
        {
            Hide();
        }
    }
}
