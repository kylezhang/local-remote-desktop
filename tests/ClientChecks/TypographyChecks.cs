using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LocalRemoteDesktop;

static partial class Program {
    static void CheckTypography(MainWindow window) {
        var previous=UiText.Current.Language;
        try {
            foreach(var language in new[]{"en","zh-Hans"}) {
                UiText.Current.Use(language,false);
                var content=(FrameworkElement)window.Content;
                content.Measure(new Size(544,528));content.Arrange(new Rect(0,0,544,528));content.UpdateLayout();
                foreach(var name in new[]{"EntryPrompt","InitiateLabel","AcceptLabel"}) {
                    var label=(TextBlock)window.FindName(name);
                    Assert(label.Foreground is SolidColorBrush ink && ink.Color==Color.FromRgb(32,45,70),"Native role theme replaced the approved text color with system black");
                    var drawing=new DrawingVisual();
                    using(var context=drawing.RenderOpen()) context.DrawText(new FormattedText(label.Text,
                        CultureInfo.GetCultureInfo("en-US"),FlowDirection.LeftToRight,
                        new Typeface(label.FontFamily,label.FontStyle,label.FontWeight,label.FontStretch),
                        label.FontSize,label.Foreground,1),new Point(0,0));
                    var runs=TextRuns(drawing.Drawing).ToArray();
                    Assert(runs.Length>0,"Native formatter produced no glyph runs");
                    Assert(label.FontWeight==FontWeights.Normal,"Entry type selected an unsupported medium weight");
                    foreach(var run in runs) {
                        bool chinese=run.Characters?.Any(ch=>ch is >= '\u2E80' and <= '\u9FFF' or >= '\uFF00' and <= '\uFFEF')==true;
                        var families=run.GlyphTypeface.FamilyNames.Values;
                        if(chinese) Assert(families.Any(value=>value.Contains("Microsoft YaHei",StringComparison.OrdinalIgnoreCase)),
                            $"CJK glyphs fell back to another family: {run.GlyphTypeface.FontUri}");
                        else Assert(families.Any(value=>value.StartsWith("Segoe UI",StringComparison.OrdinalIgnoreCase)),
                            $"Latin entry glyphs do not use Segoe UI: {run.GlyphTypeface.FontUri}");
                        Assert(run.GlyphTypeface.Weight==FontWeights.Normal,"Entry glyphs mixed real font weights");
                    }
                    for(DependencyObject? ancestor=label;ancestor!=null;ancestor=VisualTreeHelper.GetParent(ancestor))
                        if(ancestor is UIElement element) Assert(element.Effect==null,"Card shadow rasterizes text descendants");
                }
                Assert(((Image)window.FindName("SettingsGlyph")).Source is DrawingImage,"Settings is an OS-dependent text/emoji glyph");
                Assert(!window.IsVisible && !window.IsActive,"Typography check displayed or activated an app");
            }
            Console.WriteLine("PASS native WPF glyph runs under en-US fallback: Chinese entry text uses only Microsoft YaHei UI, Latin Segoe UI, uniform real Regular weights and approved ink; shadow has no text ancestors; Settings is vector; no visible window");
        } finally {UiText.Current.Use(previous,false);}
    }
    static IEnumerable<GlyphRun> TextRuns(Drawing drawing) {
        if(drawing is GlyphRunDrawing glyph) yield return glyph.GlyphRun;
        if(drawing is DrawingGroup group) foreach(var child in group.Children) foreach(var run in TextRuns(child)) yield return run;
    }
}
