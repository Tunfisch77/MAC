using System;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace MacApp {
 public sealed class AuthUser {public string id,first,last;public bool enabled;public string Name {get{return first+" "+last;}}public override string ToString(){return Name+(enabled?"":" · gesperrt");}}
 public sealed class AuthReply {public string token,role,error,service;public int api;public long expires;public AuthUser user;public AuthUser[] users;}
 sealed class AuthTicket {public string server,token;}
 public sealed class AuthFailure:Exception {public int Status;public AuthFailure(int status,string message):base(message){Status=status;}}
 public sealed class AuthClient {
  readonly string directory;public string Server {get;private set;}public AuthReply Session;public bool Remember;
  public AuthClient(string folder){directory=folder;string p=Path.Combine(folder,"auth-server.txt");if(File.Exists(p))try{if(new FileInfo(p).Length<2048)Server=ValidateServer(File.ReadAllText(p));}catch{} }
  public static string ValidateServer(string value){Uri u;if(!Uri.TryCreate((value??"").Trim(),UriKind.Absolute,out u)||u.Scheme!="https"||!string.IsNullOrEmpty(u.UserInfo)||!string.IsNullOrEmpty(u.Query)||!string.IsNullOrEmpty(u.Fragment)||u.AbsolutePath!="/"||u.IsLoopback||!u.IsDefaultPort)throw new Exception("Bitte die vollständige HTTPS-Serveradresse ohne Pfad eingeben, z. B. https://mac-anmeldung.onrender.com. Keine GitHub-Adresse.");return u.GetLeftPart(UriPartial.Authority);}
  public async Task Configure(string value){string next=ValidateServer(value);var health=await Request(next,"GET","/health",null,null);if(health.service!="mac-auth"||health.api!=1)throw new Exception("Diese Adresse gehört nicht zu einem kompatiblen MAC-Anmeldedienst.");if(Server!=next)DeleteTicket();Directory.CreateDirectory(directory);LocalState.AtomicWrite(Path.Combine(directory,"auth-server.txt"),Encoding.UTF8.GetBytes(next));Server=next;}
  static async Task<AuthReply> Request(string server,string method,string route,object body,string token){return await Task.Run(()=>{
   byte[] bytes=null;
   try{
    ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;
    var request=(HttpWebRequest)WebRequest.Create(server+route);request.Method=method;request.AllowAutoRedirect=false;request.Timeout=15000;request.ReadWriteTimeout=15000;request.Accept="application/json";request.UserAgent="MAC-Kundenklaerung/"+BuildInfo.Version;
    if(!string.IsNullOrEmpty(token))request.Headers[HttpRequestHeader.Authorization]="Bearer "+token;
    if(method=="POST"){bytes=Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(body??new object()));request.ContentType="application/json; charset=utf-8";request.ContentLength=bytes.Length;using(var stream=request.GetRequestStream())stream.Write(bytes,0,bytes.Length);}
    HttpWebResponse response;try{response=(HttpWebResponse)request.GetResponse();}catch(WebException e){response=e.Response as HttpWebResponse;if(response==null)throw new AuthFailure(0,"Anmeldeserver nicht erreichbar. Internetverbindung und Serveradresse prüfen. Es werden keine Kundendaten übertragen.");}
    using(response){if((int)response.StatusCode>=300&&(int)response.StatusCode<400)throw new AuthFailure(0,"Der Anmeldeserver darf keine Weiterleitung verwenden. Bitte die endgültige HTTPS-Adresse eintragen.");if(response.ContentLength>1024*1024)throw new AuthFailure(0,"Ungültige Serverantwort.");using(var stream=response.GetResponseStream())using(var buffer=new MemoryStream()){var block=new byte[8192];int read;while((read=stream.Read(block,0,block.Length))>0){if(buffer.Length+read>1024*1024)throw new AuthFailure(0,"Serverantwort zu groß.");buffer.Write(block,0,read);}AuthReply result;try{result=new JavaScriptSerializer{MaxJsonLength=1024*1024}.Deserialize<AuthReply>(Encoding.UTF8.GetString(buffer.ToArray()));}catch{throw new AuthFailure(0,"Der Server liefert keine gültige MAC-Antwort.");}if(result==null)throw new AuthFailure(0,"Leere Serverantwort.");if((int)response.StatusCode!=200)throw new AuthFailure((int)response.StatusCode,result.error??"Anmeldung nicht möglich.");return result;}}
   }catch(AuthFailure){throw;}catch{throw new AuthFailure(0,"Verbindung zum Anmeldeserver fehlgeschlagen. Bitte HTTPS-Adresse und Internet prüfen.");}finally{if(bytes!=null)Array.Clear(bytes,0,bytes.Length);}
  });}
  public Task<AuthReply> Call(string method,string route,object body=null){if(Session==null)throw new AuthFailure(401,"Bitte zuerst anmelden.");return Request(Server,method,route,body,Session.token);}
  public async Task Login(string first,string last,string password,bool admin,bool remember){var result=await Request(Server,"POST","/v1/login",new{first=first.Trim(),last=last.Trim(),password=password,admin=admin,remember=remember},null);CheckSession(result,true);Session=result;Remember=remember&&!admin;DeleteTicket();if(Remember)SaveTicket();}
  public static void CheckSession(AuthReply session,bool needsToken){if(session==null||(session.role!="advisor"&&session.role!="admin")||(session.role=="advisor"&&session.user==null)|| (session.user!=null&&(!System.Text.RegularExpressions.Regex.IsMatch(session.user.id??"","^[a-f0-9]{32}$")||!session.user.enabled||string.IsNullOrWhiteSpace(session.user.first)||string.IsNullOrWhiteSpace(session.user.last)))||(needsToken&&!System.Text.RegularExpressions.Regex.IsMatch(session.token??"","^[A-Za-z0-9_-]{43}$")))throw new AuthFailure(0,"Ungültige Anmeldeantwort.");}
  public async Task Validate(){var result=await Call("GET","/v1/me");CheckSession(result,false);if(Session.user!=null&&(result.user==null||result.user.id!=Session.user.id))throw new AuthFailure(401,"Beraterzuordnung hat sich geändert. Bitte erneut anmelden.");result.token=Session.token;Session=result;}
  public async Task<bool> Resume(){string p=Path.Combine(directory,"auth-session.bin");if(string.IsNullOrEmpty(Server)||!File.Exists(p))return false;byte[] cipher=null,plain=null;try{if(new FileInfo(p).Length>8192)throw new Exception();cipher=File.ReadAllBytes(p);plain=ProtectedData.Unprotect(cipher,null,DataProtectionScope.CurrentUser);var ticket=new JavaScriptSerializer().Deserialize<AuthTicket>(Encoding.UTF8.GetString(plain));if(ticket.server!=Server)return false;Session=new AuthReply{token=ticket.token};await Validate();if(Session.role!="advisor")throw new Exception();Remember=true;return true;}catch{Session=null;return false;}finally{if(cipher!=null)Array.Clear(cipher,0,cipher.Length);if(plain!=null)Array.Clear(plain,0,plain.Length);}}
  void SaveTicket(){byte[] plain=Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(new AuthTicket{server=Server,token=Session.token})),cipher=null;try{cipher=ProtectedData.Protect(plain,null,DataProtectionScope.CurrentUser);LocalState.AtomicWrite(Path.Combine(directory,"auth-session.bin"),cipher);}finally{Array.Clear(plain,0,plain.Length);if(cipher!=null)Array.Clear(cipher,0,cipher.Length);}}
  internal static void TestTicketStorage(string folder){
   var client=new AuthClient(folder);client.Server="https://example.invalid";client.Session=new AuthReply{token=new string('T',43),role="advisor"};client.SaveTicket();string path=Path.Combine(folder,"auth-session.bin");byte[] encrypted=File.ReadAllBytes(path),plain=null;
   try{if(Encoding.UTF8.GetString(encrypted).Contains(client.Session.token))throw new Exception("Token im Klartext");plain=ProtectedData.Unprotect(encrypted,null,DataProtectionScope.CurrentUser);var saved=new JavaScriptSerializer().Deserialize<AuthTicket>(Encoding.UTF8.GetString(plain));if(saved.server!=client.Server||saved.token!=client.Session.token)throw new Exception("Anmeldenachweis nicht korrekt gespeichert");}finally{Array.Clear(encrypted,0,encrypted.Length);if(plain!=null)Array.Clear(plain,0,plain.Length);client.DeleteTicket();}if(File.Exists(path))throw new Exception("Anmeldenachweis nicht gelöscht");
  }
  public void DeleteTicket(){string p=Path.Combine(directory,"auth-session.bin");if(File.Exists(p))File.Delete(p);if(File.Exists(p+".new"))File.Delete(p+".new");}
  public async Task Logout(){try{if(Session!=null)await Call("POST","/v1/logout",new{});}catch{}finally{Session=null;Remember=false;DeleteTicket();}}
  public async Task Assume(AuthUser user){var result=await Call("POST","/v1/admin/assume",new{id=user.id});CheckSession(result,true);try{await Call("POST","/v1/logout",new{});}catch{}Session=result;Remember=false;DeleteTicket();}
  public string ProfileKey {get{if(Session==null||Session.user==null)throw new Exception("Berater fehlt.");using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(Server+"/"+Session.user.id))).Replace("-","").ToLowerInvariant();}}
  public string SuggestedFile {get{return "MAC_Kundenklaerung_"+SafeName(Session.user.Name)+"_"+DateTime.Now.ToString("yyyy-MM-dd")+".xlsm";}}
  public static string SafeName(string value){return new string(value.Select(c=>char.IsLetterOrDigit(c)||c=='-'?c:'_').ToArray());}
 }
}
