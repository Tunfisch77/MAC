using System.Reflection;

[assembly: AssemblyTitle("MAC Kundenklärung")]
[assembly: AssemblyProduct("MAC Kundenklärung")]
[assembly: AssemblyCompany("MAC Versicherungsagentur")]
[assembly: AssemblyDescription("Lokale Kundenerfassung und Excel-Export")]
[assembly: AssemblyVersion(MacApp.BuildInfo.Version)]
[assembly: AssemblyFileVersion(MacApp.BuildInfo.Version)]

namespace MacApp {
 public static class BuildInfo {
  public const string Version = "1.3.0";
 }
}
