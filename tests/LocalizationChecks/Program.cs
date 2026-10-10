using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using LocalRemote;

using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("LocalRemote.strings.json")!;
var catalog=JsonSerializer.Deserialize<Dictionary<string,string>>(stream)!;
UiText.Current.Use("en");
int templates=0;
foreach(var pair in catalog) {
    var sourceTokens=Regex.Matches(pair.Key,@"\{(\d+)\}").Select(m=>m.Groups[1].Value).Order().ToArray();
    var translatedTokens=Regex.Matches(pair.Value,@"\{(\d+)\}").Select(m=>m.Groups[1].Value).Order().ToArray();
    if(!sourceTokens.SequenceEqual(translatedTokens)) throw new Exception("Catalog placeholder mismatch");
    var values=Enumerable.Range(0,8).Select(i=>(object)$"opaque中文{{6}}-{i}").ToArray();
    string source=string.Format(CultureInfo.InvariantCulture,pair.Key,values), expected=string.Format(CultureInfo.InvariantCulture,pair.Value,values);
    if(UiText.Text(source)!=expected) throw new Exception($"Translation mismatch: {pair.Key}");
    if(sourceTokens.Length>0) templates++;
}
if(UiText.Text("untrusted 文件.txt {0}")!="untrusted 文件.txt {0}") throw new Exception("Opaque value altered");
UiText.Current.Use("zh-Hans");
if(UiText.Text("目标IP")!="目标IP") throw new Exception("Chinese catalog failed");
try { UiText.Current.Use("arbitrary"); throw new Exception("Unknown language accepted"); } catch(ArgumentException) {}
Console.WriteLine($"PASS {catalog.Count} shared UI strings / {templates} templates, placeholder parity, opaque values, Chinese and invalid language");
var originalCulture=CultureInfo.CurrentUICulture;
try {
    UiText.Current.Use("en",true); UiText.Current.Use("zh-Hans"); UiText.Current.Configure(true);
    if(UiText.Current.Language!="en") throw new Exception("Saved UI language not restored");
    UiText.Current.Use("zh-Hans",true); UiText.Current.Use("en"); UiText.Current.Configure(true);
    if(UiText.Current.Language!="zh-Hans") throw new Exception("Saved Chinese not restored");
    Console.WriteLine("PASS explicit language preference survives reconfiguration (isolated test profile)");
} finally { CultureInfo.CurrentUICulture=originalCulture; }
