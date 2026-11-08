using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;

namespace MacApp {
public sealed class Release { public string Version,Url,Hash,Name; }
// Only scheduling metadata; never stores customer values or credentials.
public sealed class UpdateSchedule {
 public DateTime NextCheckUtc {get;private set;}
 public UpdateSchedule(DateTime now){NextCheckUtc=now.AddSeconds(30);}
 public bool IsDue(DateTime now){return now>=NextCheckUtc;}
 public void Checked(DateTime now){NextCheckUtc=now.AddHours(1);}
 public static bool ShouldNotify(string version,string lastVersion,long lastTicks,DateTime now){
  if(version!=lastVersion||lastTicks<=0||lastTicks>DateTime.MaxValue.Ticks||now.Ticks<lastTicks)return true;
  return now.Ticks-lastTicks>=TimeSpan.FromHours(24).Ticks;
 }
}
public static class Updater {
 public const string Version=BuildInfo.Version;
 const string Api="https://api.github.com/repos/Tunfisch77/MAC/releases/latest";
 public static bool Allowed(Uri u){return u.Scheme=="https" && u.IsDefaultPort && u.UserInfo=="" && ((u.Host=="api.github.com"&&u.AbsolutePath=="/repos/Tunfisch77/MAC/releases/latest")||(u.Host=="github.com"&&u.AbsolutePath.StartsWith("/Tunfisch77/MAC/releases/download/",StringComparison.Ordinal))||u.Host=="release-assets.githubusercontent.com"||u.Host=="objects.githubusercontent.com");}
 public static Release Parse(string json){
  var d=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(json);
  if((bool)d["draft"]||(bool)d["prerelease"])return null;
  string tag=(string)d["tag_name"];System.Version v;
  if(!System.Version.TryParse(tag.TrimStart(new char[]{'v'}),out v))throw new Exception("Das Release hat keine gültige Versionsnummer.");
  if(v<=new System.Version(Version))return null;
  string name="MAC-Setup-"+v+".exe";
  foreach(var obj in (System.Collections.IEnumerable)d["assets"]){var a=(Dictionary<string,object>)obj;if((string)a["name"]!=name)continue;
   string url=(string)a["browser_download_url"];Uri u;
   if(!Uri.TryCreate(url,UriKind.Absolute,out u)||u.Scheme!="https"||u.Host!="github.com"||u.AbsolutePath!="/Tunfisch77/MAC/releases/download/"+Uri.EscapeDataString(tag)+"/"+name)throw new Exception("Das Update stammt nicht aus dem vorgesehenen Repository.");
   object digest;string hash=a.TryGetValue("digest",out digest)?digest as string:null;
   if(hash==null||!System.Text.RegularExpressions.Regex.IsMatch(hash,"^sha256:[0-9a-fA-F]{64}$"))throw new Exception("GitHub liefert keine SHA-256-Prüfsumme für den Installer. Das Update wird nicht ausgeführt.");
   return new Release{Version=v.ToString(),Url=url,Hash=hash.Substring(7),Name=name};
  }
  throw new Exception("Das Release enthält keinen passenden Windows-Installer ("+name+").");
 }
 public static byte[] Fetch(string url,int max){
  ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
  Uri u=new Uri(url);
  for(int hop=0;hop<6;hop++){
   if(!Allowed(u))throw new Exception("Nicht erlaubtes Updateziel. Der Download wurde abgebrochen.");
   var req=(HttpWebRequest)WebRequest.Create(u);req.AllowAutoRedirect=false;req.UserAgent="MAC-Kundenklaerung/"+Version;req.Accept="application/vnd.github+json";req.Timeout=20000;req.ReadWriteTimeout=20000;
   using(var res=(HttpWebResponse)req.GetResponse()){
    if((int)res.StatusCode>=300&&(int)res.StatusCode<400){u=new Uri(u,res.Headers["Location"]);continue;}
    if(res.ContentLength>max)throw new Exception("Die Update-Datei ist zu groß.");
    using(var s=res.GetResponseStream())using(var m=new MemoryStream()){byte[] buffer=new byte[65536];int n;while((n=s.Read(buffer,0,buffer.Length))>0){if(m.Length+n>max)throw new Exception("Downloadgröße überschritten.");m.Write(buffer,0,n);}return m.ToArray();}
   }
  }
  throw new Exception("Zu viele Weiterleitungen beim Download.");
 }
 public static Task<Release> Check(){return Task.Run(()=>{try{return Parse(System.Text.Encoding.UTF8.GetString(Fetch(Api,2*1024*1024)));}catch(WebException e){var r=e.Response as HttpWebResponse;if(r!=null&&r.StatusCode==HttpStatusCode.NotFound)throw new Exception("Noch kein öffentliches Release gefunden. Das Repository ist leer, privat oder hat noch keine Veröffentlichung.");throw new Exception("GitHub ist nicht erreichbar oder das Abfragelimit wurde erreicht. Bitte später erneut versuchen.");}});}
 public static bool Verify(byte[] data,string expected){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(data)).Replace("-","").Equals(expected,StringComparison.OrdinalIgnoreCase);}
 public static Task<string> Download(Release r){return Task.Run(()=>{byte[] bytes=Fetch(r.Url,50*1024*1024);if(!Verify(bytes,r.Hash))throw new Exception("Prüfsumme stimmt nicht. Die Datei wird nicht installiert.");string dir=Path.Combine(Path.GetTempPath(),"MAC-Updates",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);string p=Path.Combine(dir,r.Name);File.WriteAllBytes(p,bytes);Array.Clear(bytes,0,bytes.Length);return p;});}
}
}
