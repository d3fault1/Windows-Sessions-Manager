using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.IO;
using System.Reflection;

namespace WindowsService1
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }
        private void ProjectInstaller_AfterInstall(object sender, InstallEventArgs e)
        {
            if (!File.Exists(@"C:\\config.ini"))
            {
                string path = Assembly.GetExecutingAssembly().Location;
                FileStream fp = File.OpenWrite("C:\\config.ini");
                StreamWriter sw = new StreamWriter(fp);
                sw.AutoFlush = true;
                sw.WriteLine(path.Remove(path.LastIndexOf("\\")));
                sw.WriteLine(10);
                sw.Dispose();
                fp.Dispose();
                sw.Close();
                fp.Close();
            }
            Thread.Sleep(2000);
            ServiceController sc = new ServiceController(serviceInstaller1.ServiceName);
            sc.Start();
        }

        private void ProjectInstaller_BeforeUninstall(object sender, InstallEventArgs e)
        {
            ServiceController sc = new ServiceController(serviceInstaller1.ServiceName);
            if (sc.Status == ServiceControllerStatus.Running)
                sc.Stop();
        }
    }
}
