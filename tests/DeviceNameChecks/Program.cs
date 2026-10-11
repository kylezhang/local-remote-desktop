using System.Text.Json;
using LocalRemoteDesktop;
static class Check {
    static void Require(bool okay,string why) {if(!okay) throw new Exception(why);}
    static void Main() {
        var root=Path.Combine(Path.GetTempPath(),"lrd-name-check-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try {
            var path=Path.Combine(root,"name.txt");var names=new DeviceName(path,"Fixture PC");
            Require(names.Current=="Fixture PC" && !File.Exists(path),"Default name persisted without consent");
            foreach(var value in new[]{"", "   ",new string('A',33),"Bad\nName","Bad\0Name"}) {
                try {names.Save(value);throw new Exception("Invalid name accepted");} catch(ArgumentException) {}
            }
            Require(names.Current=="Fixture PC" && !File.Exists(path),"Rejected edit changed name");
            names.Save("  办公室 Mac  ");Require(names.Current=="办公室 Mac","Trimming/name save failed");
            Require(new DeviceName(path,"Fixture PC").Current=="办公室 Mac","Name not restored after restart");
            names.Save("Fixture PC");Require(!File.Exists(path),"System restore left override");
            Require(new DeviceName(path,"Renamed System").Current=="Renamed System","System default frozen after restore");
            Require(DeviceName.Valid(string.Concat(Enumerable.Repeat("😀",32)))!=null,"Unicode scalar boundary rejected");
            using var valid=JsonDocument.Parse("{\"deviceName\":\"Office target\"}");
            using var invalid=JsonDocument.Parse("{\"deviceName\":123}");
            using var missing=JsonDocument.Parse("{}");
            Require(DeviceName.Peer(valid.RootElement,"192.168.1.24")=="Office target","Peer label not accepted");
            Require(DeviceName.Peer(invalid.RootElement,"192.168.1.24")=="192.168.1.24" && DeviceName.Peer(missing.RootElement,"fallback")=="fallback","Legacy/malformed metadata did not fall back");
            File.WriteAllText(path,new string('x',1024));Require(new DeviceName(path,"Fixture PC").Current=="Fixture PC","Oversized preference accepted");
            Console.WriteLine("PASS isolated name persistence, restore, Unicode bounds, invalid edits and authenticated metadata fallback");
        } finally {Directory.Delete(root,true);}
    }
}
