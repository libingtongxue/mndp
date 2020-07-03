using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

namespace mndp
{
    class Program
    {
        static void Main(string[] args)
        {
            MKMndp mkMndp = new MKMndp();
            mkMndp.Start();
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
                    for(int i = 0; i < mkMndp.GetMikroTikInfos.Count; i++)
                    {
                        Console.Write("-");
                    }
                    Thread.Sleep(300);
                }
            }
            mkMndp.Stop();
            List<MikroTikInfo> mikroTikInfos = mkMndp.GetMikroTikInfos;
            mikroTikInfos.ForEach((m) => Console.WriteLine("IPAddr:{0},MacAddr:{1},Identity:{2},Version:{3},Platform:{4},Uptime:{5},Board:{6}", m.IPAddr, m.MacAddr, m.Identity, m.Version, m.Platform, m.Uptime, m.Board));
        }
    }
    class MKMndp
    {
        static readonly ushort TlvTypeMacAddr = 1;
        static readonly ushort TlvTypeIdentity = 5;
        static readonly ushort TlvTypeVersion = 7;
        static readonly ushort TlvTypePlatform = 8;
        static readonly ushort TlvTypeUptime = 10;
        static readonly ushort TlvTypeSoftwareID = 11;
        static readonly ushort TlvTypeBoard = 12;
        static readonly int Port = 5678;
        static readonly Byte[] sendBytes = new Byte[] { 0x00, 0x00, 0x00, 0x00 };
        static readonly UdpClient udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, Port));
        static IPEndPoint IPBroadcast;
        readonly Thread threadSend;
        readonly Thread threadReceive;
        static readonly List<MikroTikInfo> mikroTikInfos = new List<MikroTikInfo>();
        static bool sendFlag = true;
        static bool receiveFlag = true;
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
                if(threadSend.ThreadState == ThreadState.Running)
                {
                    //Console.WriteLine("ThreadSend Is Running");
                }
            }
            //ReceiveMsgThread
            if (threadReceive.ThreadState != ThreadState.Running)
            {
                threadReceive.Start();
                if(threadReceive.ThreadState == ThreadState.Running)
                {
                    //Console.WriteLine("ThreadReceive Is Running");
                }
            }
        }
        private void SendMsg()
        {
            while (sendFlag)
            {
                udpClient.Send(sendBytes, sendBytes.Length, IPBroadcast);
                Thread.Sleep(1000);
            }
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
                    //可能返回0.0.0.0未配置的设备。过滤掉。
                    if (IPAddress.Any.ToString() != RemoteIpEndPoint.Address.ToString())
                    {
                        using MemoryStream memoryStream = new MemoryStream(receiveBytes);
                        using BinaryReader binaryReader = new BinaryReader(memoryStream);
                        MikroTikInfo mikroTikInfo = new MikroTikInfo()
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
                        byte[] Tlv_Uptime_Type = binaryReader.ReadBytes(2);
                        Array.Reverse(Tlv_Uptime_Type);
                        byte[] Tlv_Uptime_Length = binaryReader.ReadBytes(2);
                        Array.Reverse(Tlv_Uptime_Length);
                        ushort Tlv_Uptime_Length_Value = BitConverter.ToUInt16(Tlv_Uptime_Length, 0);
                        byte[] Tlv_Uptime_Value = binaryReader.ReadBytes(Tlv_Uptime_Length_Value);
                        string Uptime = TimeSpan.FromSeconds(BitConverter.ToUInt32(Tlv_Uptime_Value, 0)).ToString().Replace(".", "d");
                        if (BitConverter.ToUInt16(Tlv_Uptime_Type, 0) == TlvTypeUptime)
                        {
                            mikroTikInfo.Uptime = Uptime;
                        }
                        byte[] Tlv_SoftwareID_Type = binaryReader.ReadBytes(2);
                        Array.Reverse(Tlv_SoftwareID_Type);
                        byte[] Tlv_SoftwareID_Length = binaryReader.ReadBytes(2);
                        Array.Reverse(Tlv_SoftwareID_Length);
                        ushort Tlv_SoftwareID_Length_Value = BitConverter.ToUInt16(Tlv_SoftwareID_Length, 0);
                        byte[] Tlv_SoftwareID_Value = binaryReader.ReadBytes(Tlv_SoftwareID_Length_Value);
                        string SoftwareID = Encoding.Default.GetString(Tlv_SoftwareID_Value);
                        if (BitConverter.ToUInt16(Tlv_SoftwareID_Type, 0) == TlvTypeSoftwareID)
                        {
                            mikroTikInfo.SoftwareID = SoftwareID;
                        }
                        byte[] Tlv_Board_Type = binaryReader.ReadBytes(2);
                        Array.Reverse(Tlv_Board_Type);
                        byte[] Tlv_Board_Lenght = binaryReader.ReadBytes(2);
                        Array.Reverse(Tlv_Board_Lenght);
                        ushort Tlv_Board_Length_Value = BitConverter.ToUInt16(Tlv_Board_Lenght, 0);
                        byte[] Tlv_Board_Value = binaryReader.ReadBytes(Tlv_Board_Length_Value);
                        string Board = Encoding.Default.GetString(Tlv_Board_Value);
                        if (BitConverter.ToUInt16(Tlv_Board_Type, 0) == TlvTypeBoard)
                        {
                            mikroTikInfo.Board = Board;
                        }
                        bool flag = false;
                        foreach (MikroTikInfo t in mikroTikInfos)
                        {
                            if (t.MacAddr == MacAddr)
                            {
                                flag = true;
                                break;
                            }
                        }
                        if (!flag)
                        {
                            mikroTikInfos.Add(mikroTikInfo);
                        }
                    }
                }
            }
        }
        public List<MikroTikInfo> GetMikroTikInfos
        {
            get
            {
                return mikroTikInfos;
            }
        }
        public void Stop()
        {            
            if (threadReceive.ThreadState != ThreadState.Aborted)
            {
                receiveFlag = false;
                Thread.Sleep(1000);
                if (threadSend.ThreadState != ThreadState.Aborted)
                {
                    sendFlag = false;
                }
            }
        }
    }
    class MikroTikInfo
    {
        public string IPAddr { get; set; }
        public string MacAddr { get; set; }
        public string Identity { get; set; }
        public string Version { get; set; }
        public string Platform { get; set; }
        public string Uptime { get; set; }
        public string SoftwareID { get; set; }
        public string Board { get; set; }
    }
}
