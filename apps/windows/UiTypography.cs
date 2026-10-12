using System.Windows.Markup;
using System.Windows.Media;
namespace LocalRemoteDesktop;

// Segoe UI lacks CJK. Its default en-US fallback may mix JhengHei and Yu Gothic
// within one Chinese label, selecting different real weights for each glyph.
// Map CJK explicitly; use installed OS fonts only (no redistributed font files).
public static class UiTypography {
    public static FontFamily Family { get; }=CreateFamily();
    static FontFamily CreateFamily() {
        var family=new FontFamily();
        family.FamilyNames.Add(XmlLanguage.GetLanguage("en-US"),"Local Remote Desktop UI");
        family.FamilyMaps.Add(new FontFamilyMap {
            Unicode="2E80-9FFF,F900-FAFF,FE30-FE4F,FF00-FFEF,20000-2FFFF,30000-323AF",
            Target="Microsoft YaHei UI, Microsoft YaHei, Global User Interface"
        });
        family.FamilyMaps.Add(new FontFamilyMap {
            Unicode="0000-10FFFF",Target="Segoe UI, Microsoft YaHei UI, Global User Interface"
        });
        return family;
    }
}
