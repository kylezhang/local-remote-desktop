using System.Windows.Data;
using System.Windows.Markup;
namespace LocalRemoteDesktop;
public sealed class TranslateExtension : MarkupExtension {
    public string Key {get;set;}="";
    public override object ProvideValue(IServiceProvider provider)=>
        new Binding {Source=UiText.Current,Path=new System.Windows.PropertyPath($"[{Key}]"),Mode=BindingMode.OneWay}.ProvideValue(provider);
}
