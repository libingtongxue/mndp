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
            while (true)
            {
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                Byte[] receiveBytes = udpClient.Receive(ref RemoteIpEndPoint);
                //ReceiveData:0000；
                if (receiveBytes.Length > 4)
                {
                    using MemoryStream memoryStream = new MemoryStream(receiveBytes);
                    using BinaryReader binaryReader = new BinaryReader(memoryStream);
                    MikroTikInfo mikroTikInfo = new MikroTikInfo();
                    mikroTikInfo.IPAddr = RemoteIpEndPoint.Address.ToString();
                    string IPAddr = RemoteIpEndPoint.Address.ToString();
                    //TLV格式的数据指针偏移4
                    binaryReader.BaseStream.Position = 4;
                    //开始读取TLV格式的数据
                    byte[] mac_bytes = binaryReader.ReadBytes(2);
                    Array.Reverse(mac_bytes);
                    byte[] mac_length = binaryReader.ReadBytes(2);
                    Array.Reverse(mac_length);
                    ushort b_len = BitConverter.ToUInt16(mac_length, 0);
                    byte[] mac_value = binaryReader.ReadBytes(b_len);
                    string MacAddr = BitConverter.ToString(mac_value).Replace("-", ":");
                    if (BitConverter.ToUInt16(mac_bytes, 0) == TlvTypeMacAddr)
                    {
                        mikroTikInfo.MacAddr = BitConverter.ToString(mac_value).Replace("-", ":");
                    }
                    byte[] identity_type = binaryReader.ReadBytes(2);
                    Array.Reverse(identity_type);
                    byte[] identity_length = binaryReader.ReadBytes(2);
                    Array.Reverse(identity_length);
                    ushort identity_len = BitConverter.ToUInt16(identity_length, 0);
                    byte[] identity_value = binaryReader.ReadBytes(identity_len);
                    string Identity = Encoding.Default.GetString(identity_value);
                    //Console.WriteLine(Identity);
                    if (BitConverter.ToUInt16(identity_type, 0) == TlvTypeIdentity)
                    {
                        mikroTikInfo.Identity = Encoding.Default.GetString(identity_value);
                    }
                    byte[] version_type = binaryReader.ReadBytes(2);
                    Array.Reverse(version_type);
                    byte[] version_length = binaryReader.ReadBytes(2);
                    Array.Reverse(version_length);
                    ushort version_len = BitConverter.ToUInt16(version_length, 0);
                    byte[] version_value = binaryReader.ReadBytes(version_len);
                    string Version = Encoding.Default.GetString(version_value);
                    if (BitConverter.ToUInt16(version_type, 0) == TlvTypeVersion)
                    {
                        mikroTikInfo.Version = Encoding.Default.GetString(version_value);
                    }
                    byte[] platform_type = binaryReader.ReadBytes(2);
                    Array.Reverse(platform_type);
                    byte[] platform_length = binaryReader.ReadBytes(2);
                    Array.Reverse(platform_length);
                    ushort platform_len = BitConverter.ToUInt16(platform_length, 0);
                    byte[] platform_value = binaryReader.ReadBytes(platform_len);
                    string Platform = Encoding.Default.GetString(platform_value);
                    if (BitConverter.ToUInt16(platform_type, 0) == TlvTypePlatform)
                    {
                        mikroTikInfo.Platform = Encoding.Default.GetString(platform_value);
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
        public void Stop()
        {
            Timer.Change(Timeout.Infinite, Timeout.Infinite);
            if (threadReceive.ThreadState == ThreadState.Running)
            {
                threadSend.Abort();
            }
            if (threadSend.ThreadState == ThreadState.Running)
            {
                threadSend.Abort();
            }
        }
    }
}
