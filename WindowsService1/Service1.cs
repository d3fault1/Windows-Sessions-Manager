using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.IO;
using System.Threading;
using System.Security.Cryptography;
using System.Globalization;
using System.IO.Pipes;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace WindowsService1
{
    public partial class Service1 : ServiceBase
    {
        [DllImport("wtsapi32.dll", SetLastError = true)]
        static extern bool WTSLogoffSession(IntPtr hServer, int SessionId, bool bWait);
        public int flag_0,delay,flag_1;
        public string workdir,data_read,processloglevel;
        List<int> blacklist;
        public NamedPipeServerStream st;
        public StreamWriter sw;
        public StreamReader sr;
        public FileStream Logger;
        public StreamWriter lr;
        public struct SESSION
        {
            public string name, domain, user, pass, state, type;
            public int id, width, height, scale, pid;

            public SESSION(string name, string domain, string user, string pass, string state, string type, int id, int width, int height, int scale, int pid)
            {
                this.name = name;
                this.domain = domain;
                this.user = user;
                this.pass = pass;
                this.state = state;
                this.type = type;
                this.id = id;
                this.width = width;
                this.height = height;
                this.scale = scale;
                this.pid = pid;
            }
        };
        public Thread thread,th;
        public List<SESSION> sessions;
        public SESSION temporary;
        public Service1()
        {
            InitializeComponent();
            sessions = new List<SESSION>();
            temporary = new SESSION();
            blacklist = new List<int>();
        }
        protected override void OnStart(string[] args)
        {
            if (args.Length == 0) processloglevel = @"Debug";
            else processloglevel = args[0];
            Logger = File.OpenWrite(@"C:\\Log.txt");
            lr = new StreamWriter(Logger);
            lr.AutoFlush = true;
            try
            {
                flag_0 = 0;
                flag_1 = 0;
                foreach (SESSION info in getSessionInfo())
                {
                    blacklist.Add(info.id);
                }
                th = new Thread(coms_server);
                th.Name = "Server Thread";
                thread = new Thread(getdata);
                thread.Name = "Main Function Thread";
                if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Info] Starting Server Thread");
                th.Start();
                if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Info] Starting Main Function Thread");
                thread.Start();
            }
            catch (Exception h)
            {
                if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Error] " + h.Message);
            }
        }
        public void coms_server()
        {
            try
            {
                int cnt = 0;
                st = new NamedPipeServerStream(@"SessManagingPipe");
                if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Info] Starting Listener...");
                st.WaitForConnection();
                sr = new StreamReader(st);
                sw = new StreamWriter(st);
                sw.AutoFlush = true;
                while (st.IsConnected)
                {
                    data_read = sr.ReadLine();
                    if (!st.IsConnected) break;
                    if (cnt == 0)
                    {
                        temporary.domain = data_read;
                        if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Info] Read Data: " + temporary.domain);
                    }
                    if (cnt == 1)
                    {
                        temporary.user = data_read;
                        if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Info] Read Data: " + temporary.user);
                    }
                    if (cnt == 2)
                    {
                        temporary.pass = data_read;
                        if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Info] Read Data: " + temporary.pass);
                    }
                    if (cnt == 3)
                    {
                        temporary.width = Convert.ToInt32(data_read);
                        if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Info] Read Data: " + temporary.width.ToString());
                    }
                    if (cnt == 4)
                    {
                        temporary.height = Convert.ToInt32(data_read);
                        if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Info] Read Data: " + temporary.height.ToString());
                    }
                    if (cnt == 5)
                    {
                        temporary.scale = Convert.ToInt32(data_read);
                        if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Info] Read Data: " + temporary.scale.ToString());
                    }
                    cnt++;
                }
                server_cleanup();
                if (flag_1 == 1) return;
                temporary.id = -1;
                temporary.pid = -1;
                if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Info] Adding Session....");
                sessions.Add(temporary);
                if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Info] Restarting Listener...");
                coms_server();
            }
            catch (Exception h)
            {
                if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Error] " + h.Message);
            }
        }
        public void server_cleanup()
        {
            if (st.IsConnected) st.Disconnect();
            st.Close();
            st.Dispose();
        }
        public void getdata()
        {
            try
            {
                int count = 0;
                if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Info] Reading Data from Config File...");
                foreach (string line in File.ReadLines(@"C:\\config.ini"))
                {
                    if (count == 0)
                    {
                        workdir = line;
                        if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Info] Got Data: " + workdir);
                    }
                    if (count == 1)
                    {
                        delay = Convert.ToInt32(line);
                        if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Info] Got Data: " + delay.ToString());
                    }
                    if (count == 2)
                    {
                        temporary.domain = line;
                        if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Info] Got Data: " + temporary.domain);
                    }
                    if (count == 3)
                    {
                        temporary.user = decryptor(line);
                        if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Info] Got Data: " + temporary.user);
                    }
                    if (count == 4)
                    {
                        temporary.pass = decryptor(line);
                        if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Info] Got Data: " + temporary.pass);
                    }
                    if (count == 5)
                    {
                        temporary.width = Convert.ToInt32(line);
                        if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Info] Got Data: " + temporary.width.ToString());
                    }
                    if (count == 6)
                    {
                        temporary.height = Convert.ToInt32(line);
                        if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Info] Got Data: " + temporary.height.ToString());
                    }
                    if (count == 7)
                    {
                        temporary.scale = Convert.ToInt32(line);
                        if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Info] Got Data: " + temporary.scale.ToString());
                    }
                    count++;
                }
                temporary.id = -1;
                temporary.pid = -1;
                if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Info] Adding Session to the List");
                sessions.Add(temporary);
                if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Info] Running MainFunction......");
                mainfunc();
            }
            catch (Exception h)
            {
                if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Error] " + h.Message);
            }
        }
        public void mainfunc()
        {
            Directory.SetCurrentDirectory(workdir);
            Thread.Sleep(delay*1000);
            while (flag_0 == 0)
            {
                for (int t = 0; t < sessions.Count; t++)
                {
                    if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Debug] Session List Member Count:{0}, Session Index:{1}", sessions.Count, t);
                    if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Debug] Comparative Value Check: Session User is Empty " + String.IsNullOrEmpty(sessions[t].user) + ", Session ID:" + sessions[t].id);
                    if (!String.IsNullOrEmpty(sessions[t].user) && sessions[t].id == -1)
                    {
                        Process p = new Process();
                        p.StartInfo.FileName = "wfreerdp.exe";
                        p.StartInfo.Arguments = "/v:127.0.0.2 /u:" + sessions[t].domain + "\\" + sessions[t].user + " /p:" + sessions[t].pass + " /w:" + sessions[t].width.ToString() + " /h:" + sessions[t].height.ToString() + " /scale-desktop:" + sessions[t].scale.ToString() + " /log-level:" + processloglevel + " /cert-ignore";
                        p.StartInfo.UseShellExecute = false;
                        p.StartInfo.RedirectStandardError = true;
                        p.StartInfo.RedirectStandardOutput = true;
                        if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Info] Spawning Process for a New Session....");
                        if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Info] wfreerdp.exe " + p.StartInfo.Arguments);
                        p.Start();
                        p.OutputDataReceived += (s, e) => 
                        { 
                            if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Process Info] " + e.Data); 
                        };
                        p.ErrorDataReceived += (s, e) => 
                        { 
                            if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Process Error] " + e.Data);
                        };
                        p.BeginOutputReadLine();
                        p.BeginErrorReadLine();
                        Thread.Sleep(7000);
                        setQueryEntries(t, getNewSessionId(), p.Id);
                        string sta = getStatsbyId(sessions[t].id);
                        if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Debug] Session Status Check for Session ID {0}: " + sta, sessions[t].id);
                        if (sta.Contains("Conn"))
                        {
                            if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Debug] Corrupted Session Found! Trying To Remove...");
                            try
                            {
                                Process.GetProcessById(sessions[t].id).Kill();
                            }
                            catch (Exception kex)
                            {
                                if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Error] " + kex.Message);
                            }
                            if (!WTSLogoffSession(IntPtr.Zero, sessions[t].id, true))
                            {
                                if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Error] Critical Error Failed to Remove Corrupted Session.... Aborting Service... Restart is Urgent");
                                OnStop();
                            }
                        }
                        continue;
                    }
                    else if (getStatsbyId(sessions[t].id).Contains("Conn"))
                    {
                        if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Debug] Corrupted Session Found! Trying To Remove...");
                        try
                        {
                            Process.GetProcessById(sessions[t].id).Kill();
                        }
                        catch (Exception kex)
                        {
                            if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Error] " + kex.Message);
                        }
                        if (!WTSLogoffSession(IntPtr.Zero, sessions[t].id, true))
                        {
                            if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Error] Critical Error Failed to Remove Corrupted Session.... Aborting Service... Restart is Urgent");
                            OnStop();
                        }
                    }
                    else if (getStatsbyId(sessions[t].id).Contains("Disc"))
                    {
                        Process pro = new Process();
                        pro.StartInfo.FileName = "wfreerdp.exe";
                        pro.StartInfo.Arguments = "/v:127.0.0.2 /u:" + sessions[t].domain + "\\" + sessions[t].user + " /p:" + sessions[t].pass + " /w:" + sessions[t].width.ToString() + " /h:" + sessions[t].height.ToString() + " /scale-desktop:" + sessions[t].scale.ToString() + " /log-level:" + processloglevel + " /cert-ignore";
                        pro.StartInfo.UseShellExecute = false;
                        pro.StartInfo.RedirectStandardError = true;
                        pro.StartInfo.RedirectStandardOutput = true;
                        if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Info] Spawning Process to Reactivate Session {0}: ", sessions[t].id);
                        if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Info] wfreerdp.exe " + pro.StartInfo.Arguments);
                        pro.Start();
                        pro.OutputDataReceived += (se, ea) =>
                        {
                            if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Process Info] " + ea.Data);
                        };
                        pro.ErrorDataReceived += (se, ea) =>
                        {
                            if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Process Error] " + ea.Data);
                        };
                        pro.BeginOutputReadLine();
                        pro.BeginErrorReadLine();
                        Thread.Sleep(2000);
                        setQueryEntries(t, sessions[t].id, pro.Id);
                        continue;
                    }
                }
                Thread.Sleep(5000);
            }
        }

        public string getStatsbyId(int ID)
        {
            foreach (SESSION info in getSessionInfo())
            {
                if (info.id == ID) return info.state;
            }
            return String.Empty;
        }

        public int getNewSessionId()
        {
            foreach (SESSION info in getSessionInfo())
            {
                if (!blacklist.Contains(info.id))
                {
                    blacklist.Add(info.id);
                    return info.id;
                }
            }
            return -1;
        }

        public void setQueryEntries(int index, int ID, int PID)
        {
            if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Debug] Setting New Session ID {0}, PID {1}", ID, PID);
            foreach (SESSION info in getSessionInfo())
            {
                if (info.id == ID)
                {
                    if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Debug] Found Query for Session ID {0}",ID);
                    if (processloglevel.CompareTo("Off") != 0) lr.WriteLine("[Debug] Inserting New Session Data:" + info.name + " " + sessions[index].domain + " " + sessions[index].user + " " + sessions[index].pass + " " + info.state + " " + info.type+ " " + ID.ToString() + " " + sessions[index].width.ToString() + " " + sessions[index].height.ToString() + " " + PID.ToString());
                    sessions[index] = new SESSION(info.name, sessions[index].domain, sessions[index].user, sessions[index].pass, info.state, info.type, ID, sessions[index].width, sessions[index].height, sessions[index].scale, PID);
                    return;
                }
            }
        }

        public SESSION[] getSessionInfo()
        {
            string output, temp;
            string[] lines;
            Process ap = new Process();
            ap.StartInfo.FileName = "cmd.exe";
            ap.StartInfo.WorkingDirectory = "C:\\Windows\\System32";
            ap.StartInfo.Arguments = "/c query session";
            ap.StartInfo.UseShellExecute = false;
            ap.StartInfo.RedirectStandardOutput = true;
            ap.StartInfo.CreateNoWindow = true;
            ap.Start();
            output = ap.StandardOutput.ReadToEnd();
            lines = output.Split('\n');
            SESSION[] worker = new SESSION[lines.Length - 2];
            temp = null;
            int session_pos = lines[0].IndexOf("SESSIONNAME");
            int user_pos = lines[0].IndexOf("USERNAME");
            int id_pos = lines[0].IndexOf("ID") + 1;
            int state_pos = lines[0].IndexOf("STATE");
            int type_pos = lines[0].IndexOf("TYPE");
            int dev_pos = lines[0].IndexOf("DEVICE");
            for (int i = 0; i < lines.Length - 2; i++)
            {
                worker[i].name = lines[i + 1].Substring(session_pos, user_pos - session_pos).Trim();
                temp = lines[i + 1].Substring(user_pos, id_pos - user_pos + 1);
                worker[i].user = temp.Substring(0, temp.LastIndexOf(" ")).Trim();
                worker[i].id = Convert.ToInt32(temp.Substring(temp.LastIndexOf(" ") + 1, temp.Length - 1 - temp.LastIndexOf(" ")).Trim());
                worker[i].state = lines[i + 1].Substring(state_pos, type_pos - state_pos + 1).Trim();
                worker[i].type = lines[i + 1].Substring(type_pos, dev_pos - type_pos + 1).Trim();
            }
            return worker;
        }

        public string decryptor(string input)
        {
            MemoryStream memoryStream = new MemoryStream(Convert.FromBase64String(input.Trim()));
            ICryptoTransform transform = new RijndaelManaged
            {
                Key = XOR_Single(parse2byte("81E9BFDE8CC9B94E014CF49AD0E21A92"), 147),
                IV = XOR_Single(parse2byte("E5D6B080D11A8784E5D6B080D11A8784"), 147),
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            }.CreateDecryptor();
            CryptoStream cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Read);
            StreamReader streamReader = new StreamReader(cryptoStream);
            string result = streamReader.ReadLine();
            streamReader.Close();
            cryptoStream.Close();
            memoryStream.Close();
            return result;
        }

        public byte[] parse2byte(string string_0)
        {
            string text = string_0.Trim().Replace(" ", string.Empty);
            int num = text.Length / 2;
            byte[] array = new byte[num];
            for (int i = 0; i < num; i++)
            {
                string s = text.Substring(2 * i, 2);
                array[i] = byte.Parse(s, NumberStyles.HexNumber);
            }
            return array;
        }

        public byte[] XOR_Single(byte[] byte_0, byte byte_1)
        {
            byte[] array = new byte[byte_0.Length];
            for (int i = 0; i < byte_0.Length; i++)
            {
                array[i] = Convert.ToByte((int)(byte_0[i] ^ byte_1));
            }
            return array;
        }

        protected override void OnStop()
        {
            flag_0 = 1;
            flag_1 = 1;
            NamedPipeClientStream temp = new NamedPipeClientStream(@"SessManagingPipe");
            temp.Connect();
            temp.Close();
            temp.Dispose();

            //////////

            thread.Abort();
            th.Abort();
            Process[] p = Process.GetProcessesByName("wfreerdp");
            foreach (Process s in p)
            {
                s.Kill();
            }
            foreach (SESSION f in sessions)
            {
                Process k = new Process();
                k.StartInfo.WorkingDirectory = @"C:\Windows\System32";
                k.StartInfo.FileName = "logoff.exe";
                k.StartInfo.Arguments = f.id.ToString();
                k.StartInfo.UseShellExecute = false;
                k.StartInfo.RedirectStandardError = true;
                k.StartInfo.RedirectStandardOutput = true;
                k.OutputDataReceived += (s,e) =>
                {
                    lr.WriteLine("[Process Output] " + e.Data);
                };
                k.ErrorDataReceived += (s, e) =>
                {
                    lr.WriteLine("[Process Error] " + e.Data);
                };
                k.Start();
                k.BeginErrorReadLine();
                k.BeginOutputReadLine();
                k.WaitForExit(20000);
                lr.WriteLine("[Info] Session Closed... ID {0}", f.id);
            }
            Logger.Close();
        }
    }
}
