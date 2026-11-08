using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
namespace MacApp {
 public sealed class Preferences {
  public string UpdateNoticeVersion="";public long UpdateNoticeUtcTicks;
  public bool Dark=false,Maximized=false;public int Width=1440,Height=960;public double Split=0.40;
 }
 public sealed class SessionState:IDisposable {
  public Book Book;public string[] Draft;public string Baseline;public int Editing=-1;public bool Dirty;
  public void Dispose(){if(Book!=null)Book.Dispose();Book=null;if(Draft!=null)Array.Clear(Draft,0,Draft.Length);Draft=null;Baseline=null;}
 }
 public static class SessionCodec {
  const int Limit=96*1024*1024;
  static void Values(BinaryWriter w,string[] values){foreach(string v in values)Text(w,v??"");}
  static string[] Values(BinaryReader r){return Enumerable.Range(0,10).Select(i=>Text(r,128000)).ToArray();}
  static void Text(BinaryWriter w,string value){byte[] bytes=Encoding.UTF8.GetBytes(value);try{Blob(w,bytes);}finally{Array.Clear(bytes,0,bytes.Length);}}
  static string Text(BinaryReader r,int limit){byte[] b=Blob(r,limit);try{return Encoding.UTF8.GetString(b);}finally{Array.Clear(b,0,b.Length);}}
  static void Blob(BinaryWriter w,byte[] b){w.Write(b.Length);w.Write(b);if(w.BaseStream.Length>Limit)throw new InvalidDataException("Sitzung zu groß.");}
  static byte[] Blob(BinaryReader r,int limit){int length=r.ReadInt32();if(length<0||length>limit||length>r.BaseStream.Length-r.BaseStream.Position)throw new InvalidDataException("Ungültige Sitzungsdatei.");return r.ReadBytes(length);}
  static XElement Element(string value){using(var sr=new StringReader(value))using(var reader=XmlReader.Create(sr,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null}))return XElement.Load(reader);}
  public static byte[] Encode(SessionState state){using(var m=new MemoryStream()){try{using(var w=new BinaryWriter(m,Encoding.UTF8,true)){
   w.Write(120);w.Write(state.Book.Parts.Count);foreach(var p in state.Book.Parts){Text(w,p.Key);Blob(w,p.Value);}Values(w,state.Book.Headers);Text(w,state.Book.Pattern.ToString(SaveOptions.DisableFormatting));w.Write(state.Book.Rows.Count);
   foreach(var record in state.Book.Rows){Values(w,record.Values);w.Write(record.Original!=null);if(record.Original!=null){Text(w,record.Original.ToString(SaveOptions.DisableFormatting));Values(w,record.OriginalValues);}}
   Values(w,state.Draft);Text(w,state.Baseline??"");w.Write(state.Editing);w.Write(state.Dirty);w.Flush();return m.ToArray();
  }}finally{Array.Clear(m.GetBuffer(),0,(int)m.Length);}}}
  public static SessionState Decode(byte[] data){if(data.Length>Limit)throw new InvalidDataException("Sitzung zu groß.");var state=new SessionState{Book=new Book()};try{using(var m=new MemoryStream(data))using(var r=new BinaryReader(m,Encoding.UTF8)){
   if(r.ReadInt32()!=120)throw new InvalidDataException("Sitzungsformat nicht unterstützt.");int parts=r.ReadInt32();if(parts<1||parts>1000)throw new InvalidDataException();for(int i=0;i<parts;i++){string name=Text(r,4096);state.Book.Parts.Add(name,Blob(r,Limit));}state.Book.Headers=Values(r);state.Book.Pattern=Element(Text(r,2*1024*1024));
   int count=r.ReadInt32();if(count<0||count>996)throw new InvalidDataException();for(int i=0;i<count;i++){var record=new Record{Values=Values(r)};if(r.ReadBoolean()){record.Original=Element(Text(r,2*1024*1024));record.OriginalValues=Values(r);}state.Book.Rows.Add(record);}
   state.Draft=Values(r);state.Baseline=Text(r,2*1024*1024);state.Editing=r.ReadInt32();state.Dirty=r.ReadBoolean();if(state.Editing< -1||state.Editing>=count||m.Position!=m.Length||!state.Book.Parts.ContainsKey("xl/vbaProject.bin"))throw new InvalidDataException();return state;
  }}catch{state.Dispose();throw;}}
 }
 public sealed class LocalState:IDisposable {
  readonly string folder;string recoveryFolder;FileStream lease;
  public static string ProfileFolder {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MAC-Versicherungsagentur-Daten");}}
  public LocalState(string directory){folder=directory;recoveryFolder=directory;}
  public void UseProfile(string key){if(!System.Text.RegularExpressions.Regex.IsMatch(key??"","^[a-f0-9]{64}$"))throw new InvalidDataException("Ungültiges Beraterprofil.");recoveryFolder=Path.Combine(folder,"profiles",key);Directory.CreateDirectory(recoveryFolder);}
  public void Acquire(){Directory.CreateDirectory(folder);try{lease=new FileStream(Path.Combine(folder,"session.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}catch(IOException){throw new IOException("Die MAC-App ist bereits geöffnet. Bitte das vorhandene Fenster verwenden.");}}
  string Recovery {get{return Path.Combine(recoveryFolder,"session.macdraft");}}
  public bool HasRecovery {get{return File.Exists(Recovery)||File.Exists(Recovery+".new");}}
  public Preferences ReadPreferences(){string p=Path.Combine(folder,"settings.json");if(!File.Exists(p))return new Preferences();if(new FileInfo(p).Length>8192)throw new InvalidDataException();return new JavaScriptSerializer().Deserialize<Preferences>(File.ReadAllText(p))??new Preferences();}
  public void WritePreferences(Preferences preferences){byte[] data=Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(preferences));try{AtomicWrite(Path.Combine(folder,"settings.json"),data);}finally{Array.Clear(data,0,data.Length);}}
  static void RequireWindows(){if(Environment.OSVersion.Platform!=PlatformID.Win32NT)throw new PlatformNotSupportedException("Verschlüsselte Wiederherstellung benötigt Windows-Datenschutz.");}
  public void Save(SessionState state){RequireWindows();byte[] plain=SessionCodec.Encode(state),cipher=null;try{cipher=ProtectedData.Protect(plain,null,DataProtectionScope.CurrentUser);AtomicWrite(Recovery,cipher);}finally{Array.Clear(plain,0,plain.Length);if(cipher!=null)Array.Clear(cipher,0,cipher.Length);}}
  public SessionState Restore(){RequireWindows();string p=File.Exists(Recovery)?Recovery:Recovery+".new";if(new FileInfo(p).Length>100*1024*1024)throw new InvalidDataException();byte[] cipher=File.ReadAllBytes(p),plain=null;try{plain=ProtectedData.Unprotect(cipher,null,DataProtectionScope.CurrentUser);return SessionCodec.Decode(plain);}finally{Array.Clear(cipher,0,cipher.Length);if(plain!=null)Array.Clear(plain,0,plain.Length);}}
  public void DeleteRecovery(){if(File.Exists(Recovery))File.Delete(Recovery);if(File.Exists(Recovery+".new"))File.Delete(Recovery+".new");}
  internal static void AtomicWrite(string path,byte[] data){Directory.CreateDirectory(Path.GetDirectoryName(path));string pending=path+".new";try{using(var f=new FileStream(pending,FileMode.Create,FileAccess.Write,FileShare.None)){f.Write(data,0,data.Length);f.Flush(true);}if(File.Exists(path))File.Replace(pending,path,null);else File.Move(pending,path);}catch{throw;}}
  public void Dispose(){if(lease!=null)lease.Dispose();lease=null;}
 }
}
