# mndp
MNDP  MikroTik Neighbor Discovery Protocol

MKMndp mkMndp = new MKMndp();
mkMndp.Start();
while (!Console.KeyAvailable){Thread.Sleep(100);}            
List<MikroTikInfo> mikroTikInfos = mkMndp.GetMikroTikInfos;
mikroTikInfos.ForEach((m) => Console.WriteLine("IPAddr:{0},MacAddr:{1},Identify:{2},Version{3},Platform:{4}", m.IPAddr, m.MacAddr, m.Identity, m.Version, m.Platform));
mkMndp.Stop();