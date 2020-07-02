using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace mndp
{
    class Program
    {
        static void Main(string[] args)
        {
            MKMndp mkMndp = new MKMndp();
            mkMndp.Start();
            while (!Console.KeyAvailable)
            {
                Thread.Sleep(100);
            }
            mkMndp.Stop();
        }
    }
    class MKMndp
    {
        private static readonly ushort TlvTypeMacAddr = 1;
        private static readonly ushort TlvTypeIdentity = 5;
        private static readonly ushort TlvTypeVersion = 7;
        private static readonly ushort TlvTypePlatform = 8;
        static readonly int Port = 5678;
        static readonly Byte[] sendBytes = new Byte[] { 0x00, 0x00, 0x00, 0x00 };
        static readonly UdpClient udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, Port));
        static IPEndPoint IPBroadcast;
        readonly Thread threadSend;
        readonly Thread threadReceive;
        static List<MikroTikInfo> mikroTikInfos = new List<MikroTikInfo>();
        static readonly Timer Timer = new Timer(Timer_Callback, null, Timeout.Infinite, Timeout.Infinite);
        static readonly int DueTime = 0;
        static readonly int Period = 1000;
        static bool receiveFlag = true;
        struct MikroTikInfo
        {
            public string IPAddr;
            public string MacAddr;
            public string Identity;
            public string Version;
            public string Platform;
        }
        public MKMndp()
        {
            IPBroadcast = new IPEndPoint(IPAddress.Broadcast, Port);
            threadSend = new Thread(new ThreadStart(SendMsg))
            {
                Name = "Send"
            };
            threadReceive = new Thread(new ThreadStart(ReceiveMsg))
            {
                Name = "Receive"
            };
        }
        public void Start()
        {
            //SendMsgThread
            if (threadSend.ThreadState != ThreadState.Running)
            {
                threadSend.Start();
            }
            //ReceiveMsgThread
            if (threadReceive.ThreadState != ThreadState.Running)
            {
                threadReceive.Start();
            }
        }
        private void SendMsg()
        {
            Timer.Change(DueTime, Period);
        }
        private static void Timer_Callback(object state)
        {
            udpClient.Send(sendBytes, sendBytes.Length, IPBroadcast);
        }
        private void ReceiveMsg()
        {
            while (receiveFlag)
            {
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                Byte[] receiveBytes = udpClient.Receive(ref RemoteIpEndPoint);
                //ReceiveData:0000；
                if (receiveBytes.Length > 4)
                {
                    if (IPAddress.Any.ToString() != RemoteIpEndPoint.Address.ToString())
                    {
                        using MemoryStream memoryStream = new MemoryStream(receiveBytes);
                        using BinaryReader binaryReader = new BinaryReader(memoryStream);
                        MikroTikInfo mikroTikInfo = new MikroTikInfo
                        {
                            IPAddr = RemoteIpEndPoint.Address.ToString()
                        };
                        string IPAddr = RemoteIpEndPoint.Address.ToString();
                        //TLV格式的数据指针偏移4
                        binaryReader.BaseStream.Position = 4;
                        //开始读取TLV格式的数据
                        byte[] Mac_Type = binaryReader.ReadBytes(2);
                        Array.Reverse(Mac_Type);
                        byte[] Mac_Length = binaryReader.ReadBytes(2);
                        Array.Reverse(Mac_Length);
                        ushort Mac_Length_Valule = BitConverter.ToUInt16(Mac_Length, 0);
                        byte[] Mac_Value = binaryReader.ReadBytes(Mac_Length_Valule);
                        string MacAddr = BitConverter.ToString(Mac_Value).Replace("-", ":");
                        if (BitConverter.ToUInt16(Mac_Type, 0) == TlvTypeMacAddr)
                        {
                            mikroTikInfo.MacAddr = MacAddr;
                        }
                        byte[] Identity_Type = binaryReader.ReadBytes(2);
                        Array.Reverse(Identity_Type);
                        byte[] Identity_Length = binaryReader.ReadBytes(2);
                        Array.Reverse(Identity_Length);
                        ushort Identity_Length_Value = BitConverter.ToUInt16(Identity_Length, 0);
                        byte[] Identity_Value = binaryReader.ReadBytes(Identity_Length_Value);
                        string Identity = Encoding.Default.GetString(Identity_Value);
                        if (BitConverter.ToUInt16(Identity_Type, 0) == TlvTypeIdentity)
                        {
                            mikroTikInfo.Identity = Identity;
                        }
                        byte[] Version_Type = binaryReader.ReadBytes(2);
                        Array.Reverse(Version_Type);
                        byte[] Version_Length = binaryReader.ReadBytes(2);
                        Array.Reverse(Version_Length);
                        ushort Version_Length_Value = BitConverter.ToUInt16(Version_Length, 0);
                        byte[] Version_Value_Length = binaryReader.ReadBytes(Version_Length_Value);
                        string Version = Encoding.Default.GetString(Version_Value_Length);
                        if (BitConverter.ToUInt16(Version_Type, 0) == TlvTypeVersion)
                        {
                            mikroTikInfo.Version = Version;
                        }
                        byte[] Platform_Type = binaryReader.ReadBytes(2);
                        Array.Reverse(Platform_Type);
                        byte[] Platform_Length = binaryReader.ReadBytes(2);
                        Array.Reverse(Platform_Length);
                        ushort Platform_Length_Value = BitConverter.ToUInt16(Platform_Length, 0);
                        byte[] Platform_Value = binaryReader.ReadBytes(Platform_Length_Value);
                        string Platform = Encoding.Default.GetString(Platform_Value);
                        if (BitConverter.ToUInt16(Platform_Type, 0) == TlvTypePlatform)
                        {
                            mikroTikInfo.Platform = Platform;
                        }
                        bool flag = false;
                        foreach (MikroTikInfo t in mikroTikInfos)
                        {
                            if (t.IPAddr == RemoteIpEndPoint.Address.ToString())
                            {
                                flag = true;
                                break;
                            }
                        }
                        if (!flag)
                        {
                            mikroTikInfos.Add(mikroTikInfo);
                            Console.WriteLine("IPAddr:{0},MacAddr:{1},Identify:{2},Version{3},Platform:{4}", IPAddr, MacAddr, Identity, Version, Platform);
                        }
                    }
                }
            }
        }
        public void Stop()
        {            
            if (threadReceive.ThreadState != ThreadState.Aborted)
            {
                receiveFlag = false;
                if (threadSend.ThreadState != ThreadState.Aborted)
                {
                    Timer.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }
        }
    }
}
