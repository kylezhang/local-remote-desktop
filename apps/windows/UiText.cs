using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace LocalRemoteDesktop;

// The shared catalog maps existing UI messages to English, preserving opaque values.
public sealed class UiText : INotifyPropertyChanged {
    public static UiText Current {get;}=new();
    readonly Dictionary<string,string> catalog;
    readonly (Regex pattern,string english,int count)[] templates;
    string language="zh-Hans";
    public string Language=>language;
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? LanguageChanged;
    public string this[string text]=>Translate(text);
    UiText() {
        using var stream=typeof(UiText).Assembly.GetManifestResourceStream("LocalRemoteDesktop.strings.json")??throw new InvalidDataException("Missing UI catalog");
        catalog=JsonSerializer.Deserialize<Dictionary<string,string>>(stream)??throw new InvalidDataException("Invalid UI catalog");
        templates=catalog.Where(pair=>pair.Key.Contains("{0}"))
            .OrderByDescending(pair=>Regex.Replace(pair.Key,@"\{\d+\}","").Length)
            .Select(pair=> {
                var builder=new StringBuilder("\\A"); int end=0,count=0;
                foreach(Match match in Regex.Matches(pair.Key,@"\{(\d+)\}")) {
                    builder.Append(Regex.Escape(pair.Key[end..match.Index]));
                    int index=int.Parse(match.Groups[1].Value,CultureInfo.InvariantCulture);
                    builder.Append($"(?<p{index}>.*?)"); count=Math.Max(count,index+1); end=match.Index+match.Length;
                }
                builder.Append(Regex.Escape(pair.Key[end..])).Append("\\z");
                return (new Regex(builder.ToString(),RegexOptions.Singleline|RegexOptions.CultureInvariant|RegexOptions.NonBacktracking,TimeSpan.FromMilliseconds(20)),pair.Value,count);
            }).ToArray();
    }
    public static string Text(string text)=>Current.Translate(text);
    public string Translate(string text) {
        if(language=="zh-Hans" || text.Length>16384) return text;
        if(catalog.TryGetValue(text,out var exact)) return exact;
        foreach(var entry in templates) {
            var match=entry.pattern.Match(text); if(!match.Success) continue;
            var args=Enumerable.Range(0,entry.count).Select(i=>(object)match.Groups[$"p{i}"].Value).ToArray();
            return string.Format(CultureInfo.CurrentCulture,entry.english,args);
        }
        return text;
    }
    static string Preference=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"LocalRemote","language.txt");
    public void Configure(bool remember) {
        if(!remember) return;
        string selected=CultureInfo.CurrentUICulture.TwoLetterISOLanguageName=="zh"?"zh-Hans":"en";
        try { var saved=File.ReadAllText(Preference).Trim(); if(saved is "en" or "zh-Hans") selected=saved; } catch(IOException) {} catch(UnauthorizedAccessException) {}
        Use(selected,false);
    }
    public void Use(string code,bool persist=false) {
        if(code is not ("en" or "zh-Hans")) throw new ArgumentException("Unsupported UI language");
        if(language!=code) { language=code; PropertyChanged?.Invoke(this,new("Item[]")); LanguageChanged?.Invoke(); }
        if(persist) { Directory.CreateDirectory(Path.GetDirectoryName(Preference)!); File.WriteAllText(Preference,code); }
    }
}
