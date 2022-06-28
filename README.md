# MNDP
## MikroTik Neighbor Discovery Protocol
````
MKMndp mkMndp = new MKMndp();
mkMndp.Start();
while (!Console.KeyAvailable)
{
	Thread.Sleep(100);
}
List<MikroTikInfo> mikroTikInfos = mkMndp.GetMikroTikInfos;
mikroTikInfos.ForEach((m) => Console.WriteLine("IPAddr:{0},MacAddr:{1},Identify:{2},Version{3},Platform:{4}", m.IPAddr, m.MacAddr, m.Identity, m.Version, m.Platform));
mkMndp.Stop();
````

###### 警告，其实我早就发现了我开发MNDP的协议的一个BUG,就是无法兼容老版本，我的数据结构很清楚了。
###### 至于哪个版本，我也忘记了。哈哈。
###### 一些老旧版本里面根本没有相应的字段，所以导致老旧版本无法解析TLV格式的数据。
###### 我也不打算修复这个BUG.因为没时间。我本人也不是从事代码开发工作，所以没有时间.