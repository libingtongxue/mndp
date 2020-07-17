using System;
using System.Threading;
using System.Collections.Generic;

namespace mndp
{
    class Program
    {
        static readonly Timer Timer = new Timer(Timer_Callback,null,Timeout.Infinite,Timeout.Infinite);
        static readonly MKmndp mndp = new MKmndp();
        static void Main(string[] args)
        {
            bool PortFlag = true;
            while (PortFlag)
            {
                Console.Clear();
                Console.SetCursorPosition(0, 0);
                if (mndp.GetPortStatus())
                {
                    Console.Write("Port Is Not Available");
                    for (int i = 0; i < 6; i++)
                    {
                        Console.Write(".");
                        Thread.Sleep(50);
                    }
                    Thread.Sleep(1000);
                }
                else
                {
                    PortFlag = false;
                }
            }
            mndp.Start();
            Timer.Change(0,10000);
            while (!Console.KeyAvailable)
            {
                Thread.Sleep(300);
            }
            Timer.Change(Timeout.Infinite,Timeout.Infinite);
            mndp.Stop();
        }
        static void Timer_Callback(object state)
        {
            List<MKInfo> mikroTikInfos = mndp.GetMikroTikInfos;
            foreach(MKInfo m in mikroTikInfos)
            { 
                Console.WriteLine("IPAddr:{0},MacAddr:{1},Identity:{2},Version:{3},Platform:{4},Uptime:{5},Board:{6}", m.IPAddr, m.MacAddr, m.Identity, m.Version, m.Platform, m.Uptime, m.Board);
            }
        }
    }
}