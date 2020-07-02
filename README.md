# mndp
MNDP  MikroTik Neighbor Discovery Protocol<br/>
<br/>
MKMndp mkMndp = new MKMndp();<br/>
mkMndp.Start();<br/>
while (!Console.KeyAvailable){Thread.Sleep(100);}<br/>
List<MikroTikInfo> mikroTikInfos = mkMndp.GetMikroTikInfos;<br/>
mikroTikInfos.ForEach((m) => Console.WriteLine("IPAddr:{0},MacAddr:{1},Identify:{2},Version{3},Platform:{4}", m.IPAddr, m.MacAddr, m.Identity, m.Version, m.Platform));<br/>
mkMndp.Stop();<br/>
