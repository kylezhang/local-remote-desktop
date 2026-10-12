using System.Reflection;
namespace LocalRemoteDesktop;
internal static class BuildInfo {
    internal const string InterfaceRevision="entry-2026-10-12-r3";
    internal static string Display=>$"{typeof(BuildInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown"} · {InterfaceRevision}";
}
