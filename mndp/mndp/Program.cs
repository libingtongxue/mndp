using System;
using System.Threading;
using System.Collections.Generic;

namespace mndp
{
    class Program
    {
        static void Main(string[] args)
        {
            MKmndp mndp = new MKmndp();
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
                    //延时1S检查端口是否可用
                    Thread.Sleep(1000);
                }
                else
                {
                    PortFlag = false;
                }
            }
            mndp.Start();
            bool Flag = true;
            while (Flag)
            {
                Console.Clear();
                Console.SetCursorPosition(0, 0);
                if (Console.KeyAvailable)
                {
                    if (Console.ReadKey().Key == ConsoleKey.Enter)
                    {
                        Flag = false;
                    }
                }
                else
                {
                    Console.Write("Scanning");
                    for (int i = 0; i < mndp.GetMikroTikInfos.Count; i++)
                    {
                        Console.Write(".");
                        Thread.Sleep(50);
                    }
                    Thread.Sleep(300);
                }
            }
            mndp.Stop();
            List<MKInfo> mikroTikInfos = mndp.GetMikroTikInfos;
            mikroTikInfos.ForEach((m) => Console.WriteLine("IPAddr:{0},MacAddr:{1},Identity:{2},Version:{3},Platform:{4},Uptime:{5},Board:{6}", m.IPAddr, m.MacAddr, m.Identity, m.Version, m.Platform, m.Uptime, m.Board));
        }
    }
}