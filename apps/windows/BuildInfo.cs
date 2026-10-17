using System.Reflection;
namespace LocalRemoteDesktop;
internal static class BuildInfo {
    internal const string InterfaceRevision="screen-desk-2026-10-17-home";
    internal static string Display=>$"{typeof(BuildInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown"} · {InterfaceRevision}";
}
