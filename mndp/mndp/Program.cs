using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace mndp
{
    class Program
    {
        static void Main(string[] args)
        {
            MKMndp mndp = new MKMndp();
            bool PortFlag = true;
            while(PortFlag)
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
                }
                else
                {
                    PortFlag = false;
                }
                Thread.Sleep(1000);
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
                    for(int i = 0; i < mndp.GetMikroTikInfos.Count; i++)
                    {
                        Console.Write(".");
                        Thread.Sleep(50);
                    }
                    Thread.Sleep(300);
                }
            }
            mndp.Stop();
            List<MikroTikInfo> mikroTikInfos = mndp.GetMikroTikInfos;
            mikroTikInfos.ForEach((m) => Console.WriteLine("IPAddr:{0},MacAddr:{1},Identity:{2},Version:{3},Platform:{4},Uptime:{5},Board:{6}", m.IPAddr, m.MacAddr, m.Identity, m.Version, m.Platform, m.Uptime, m.Board));
        }
    }
    class MKMndp
    {
        const ushort TlvTypeMacAddr = 1;
        const ushort TlvTypeIdentity = 5;
        const ushort TlvTypeVersion = 7;
        const ushort TlvTypePlatform = 8;
        const ushort TlvTypeUptime = 10;
        const ushort TlvTypeSoftwareID = 11;
        const ushort TlvTypeBoard = 12;
        const ushort TlvTypeUnpack = 14;
        const ushort TlvTypeIPv6Addr = 15;
        const ushort TlvTypeInterface = 16;
        const ushort TlvTypeUnknown = 17;
        const int Port = 5678;
        static readonly Byte[] sendBytes = new Byte[] { 0x00, 0x00, 0x00, 0x00 };
        static readonly UdpClient udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, Port));
        static IPEndPoint IPBroadcast;
        readonly Thread threadSend;
        readonly Thread threadReceive;
        static readonly List<MikroTikInfo> mikroTikInfos = new List<MikroTikInfo>();
        static bool sendFlag = true;
        static readonly string sendName = "Send";
        static bool receiveFlag = true;
        static readonly string receiveName = "Receive";
        public MKMndp()
        {
            IPBroadcast = new IPEndPoint(IPAddress.Broadcast, Port);
            threadSend = new Thread(new ThreadStart(SendMsg))
            {
                Name = sendName
            };
            threadReceive = new Thread(new ThreadStart(ReceiveMsg))
            {
                Name = receiveName
            };
        }
        public bool GetPortStatus()
        {
            bool portStatus = false;
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] endPoints = properties.GetActiveUdpListeners();
            foreach (IPEndPoint e in endPoints)
            {
                if(e.Address.ToString() != IPAddress.Any.ToString())                   
                {
                    if (e.Port == Port)
                    {
                        portStatus = true;
                        break;
                    }     
                }
            }
            return portStatus;
        }
        public void Start()
        {
            //SendMsgThread
            if (threadSend.ThreadState != ThreadState.Running)
            {
                threadSend.Priority = ThreadPriority.AboveNormal;
                threadSend.Start();
                if(threadSend.ThreadState == ThreadState.Running)
                {
                    //Console.WriteLine("ThreadSend Is Running");
                }
            }
            //ReceiveMsgThread
            if (threadReceive.ThreadState != ThreadState.Running)
            {
                threadReceive.Priority = ThreadPriority.AboveNormal;
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
                if (receiveFlag)
                {
                    if (receiveBytes.Length > 4)
                    {
                        //可能返回0.0.0.0未配置的设备。过滤掉。
                        if (IPAddress.Any.ToString() != RemoteIpEndPoint.Address.ToString())
                        {
                            using MemoryStream memoryStream = new MemoryStream(receiveBytes);
                            using BinaryReader binaryReader = new BinaryReader(memoryStream);
                            string IPAddr = RemoteIpEndPoint.Address.ToString(); 
                            MikroTikInfo mikroTikInfo = new MikroTikInfo()
                            {
                                IPAddr = IPAddr
                            };
                            //TLV格式的数据指针偏移4
                            binaryReader.BaseStream.Position = 4;
                            //开始读取TLV格式的数据
                            //递归方法读取二进制流的数据。
                            ReadBytes(binaryReader, ref mikroTikInfo);
                            //注释掉
                            //逐一读取二进制流的数据                                                       
                            bool flag = false;
                            foreach (MikroTikInfo t in mikroTikInfos)
                            {
                                //if (t.MacAddr == MacAddr)
                                if (t.IPAddr == IPAddr)
                                {
                                    int i = mikroTikInfos.IndexOf(t);
                                    ListRemove lr = new ListRemove(MikroTikInfoRemove);
                                    lr(i);
                                    ListAdd la = new ListAdd(MikroTikInfoAdd);
                                    la(mikroTikInfo);
                                    flag = true;
                                    break;
                                }
                            }
                            if (!flag)
                            {
                                ListAdd la = new ListAdd(MikroTikInfoAdd);
                                la(mikroTikInfo);
                            }
                        }
                    }
                }
            }
        }
        void ReadBytes(BinaryReader binaryReader,ref MikroTikInfo mikroTikInfo)
        {
            byte[] Type = binaryReader.ReadBytes(2);
            Array.Reverse(Type);
            byte[] Length = binaryReader.ReadBytes(2);
            Array.Reverse(Length);
            ushort Length_Value = BitConverter.ToUInt16(Length);
            byte[] Value = binaryReader.ReadBytes(Length_Value);
            if(BitConverter.ToUInt16(Type) != TlvTypeUnknown)
            {
                switch (BitConverter.ToUInt16(Type))
                {
                    case TlvTypeMacAddr:
                        mikroTikInfo.MacAddr = BitConverter.ToString(Value).Replace("-", ":");
                        break;
                    case TlvTypeIdentity:
                        mikroTikInfo.Identity = Encoding.Default.GetString(Value);
                        break;
                    case TlvTypeVersion:
                        mikroTikInfo.Version = Encoding.Default.GetString(Value);
                        break;
                    case TlvTypePlatform:
                        mikroTikInfo.Platform = Encoding.Default.GetString(Value);
                        break;
                    case TlvTypeUptime:
                        mikroTikInfo.Uptime = TimeSpan.FromSeconds(BitConverter.ToUInt32(Value, 0)).ToString().Replace(".", "d");
                        break;
                    case TlvTypeSoftwareID:
                        mikroTikInfo.SoftwareID = Encoding.Default.GetString(Value);
                        break;
                    case TlvTypeBoard:
                        mikroTikInfo.Board = Encoding.Default.GetString(Value);
                        break;
                    case TlvTypeUnpack:
                        mikroTikInfo.Unpack = Encoding.Default.GetString(Value);
                        break;
                    case TlvTypeIPv6Addr:
                        mikroTikInfo.IPv6Addr = Encoding.Default.GetString(Value);
                        break;
                    case TlvTypeInterface:
                        mikroTikInfo.InterfaceName = Encoding.Default.GetString(Value);
                        break;
                }
                ReadBytes(binaryReader,ref mikroTikInfo);             
            }
        }
        void ReadBytes_v2(BinaryReader binaryReader ,ref MikroTikInfo mikroTikInfo)
        {
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
            byte[] Tlv_Unpack_Type = binaryReader.ReadBytes(2);
            Array.Reverse(Tlv_Unpack_Type);
            byte[] Tlv_Unpack_Length = binaryReader.ReadBytes(2);
            Array.Reverse(Tlv_Unpack_Length);
            ushort Tlv_Unpack_Length_Value = BitConverter.ToUInt16(Tlv_Unpack_Length);
            byte[] Tlv_Unpack_Value = binaryReader.ReadBytes(Tlv_Unpack_Length_Value);
            string Unpack = Encoding.Default.GetString(Tlv_Unpack_Value);
            if (BitConverter.ToUInt16(Tlv_Unpack_Type, 0) == TlvTypeUnpack)
            {
                mikroTikInfo.Unpack = Unpack;
            }
            byte[] Tlv_IPv6Addr_Type = binaryReader.ReadBytes(2);
            Array.Reverse(Tlv_IPv6Addr_Type);
            byte[] Tlv_IPv6Addr_Length = binaryReader.ReadBytes(2);
            Array.Reverse(Tlv_IPv6Addr_Length);
            ushort Tlv_IPv6Addr_Length_Value = BitConverter.ToUInt16(Tlv_IPv6Addr_Length);
            byte[] Tlv_IPv6Addr_Value = binaryReader.ReadBytes(Tlv_IPv6Addr_Length_Value);
            string IPv6Addr = Encoding.Default.GetString(Tlv_IPv6Addr_Value);
            if (BitConverter.ToUInt16(Tlv_IPv6Addr_Type, 0) == TlvTypeIPv6Addr)
            {
                mikroTikInfo.IPv6Addr = IPv6Addr;
            }
            byte[] Tlv_InterfaceName_Type = binaryReader.ReadBytes(2);
            Array.Reverse(Tlv_InterfaceName_Type);
            byte[] Tlv_InterfaceName_Length = binaryReader.ReadBytes(2);
            Array.Reverse(Tlv_InterfaceName_Length);
            ushort Tlv_InterfaceName_Length_Value = BitConverter.ToUInt16(Tlv_InterfaceName_Length);
            byte[] Tlv_InterfaceName_Value = binaryReader.ReadBytes(Tlv_InterfaceName_Length_Value);
            string InterfaceName = Encoding.Default.GetString(Tlv_InterfaceName_Value);
            if (BitConverter.ToUInt16(Tlv_InterfaceName_Type, 0) == TlvTypeInterface)
            {
                mikroTikInfo.InterfaceName = InterfaceName;
            }
        }
        delegate void ListRemove(int i);
        private void MikroTikInfoRemove(int i)
        {
            mikroTikInfos.RemoveAt(i);
        }
        delegate void ListAdd(MikroTikInfo m);
        private void MikroTikInfoAdd(MikroTikInfo m)
        {
            mikroTikInfos.Add(m);
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
                Thread.Sleep(500);
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
        public string Unpack { get; set; }
        public string IPv6Addr { get; set; }
        public string InterfaceName { get; set; }
    }
}