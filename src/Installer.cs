using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using MacApp;

class Installer : Form {
 readonly TextBox status=new TextBox{Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,BorderStyle=BorderStyle.None,BackColor=Color.FromArgb(243,246,244)}; readonly Button install=new Button(); readonly ProgressBar progress=new ProgressBar();
 string target; bool busy;
 static readonly string[] Files={"MAC-Kundenklaerung.exe","ANLEITUNG.md","assets/template.xlsm","assets/logo.png","assets/mac.ico"};
 [STAThread] static void Main(string[] args) {
  if(args.Length==2&&args[0]=="--verify-package"){var bytes=File.ReadAllBytes(args[1]);if(!Updater.Verify(bytes,PayloadInfo.Hash))throw new Exception("Package checksum mismatch");Unpack(bytes);Console.WriteLine("PASS: release archive hash and exact five-file package.");return;}
  if(args.Contains("--self-test")){Tests();return;}
  Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  try{Application.Run(new Installer(args));}catch(Exception e){MacDialog.Show(null,new Palette(false),"Setup konnte nicht starten",e.Message);Environment.ExitCode=1;}
 }
 Installer(string[] args){
  target=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MAC-Versicherungsagentur");
  if(args.Length==2&&args[0]=="/target")target=Path.GetFullPath(args[1]);
  Text="MAC Setup "+BuildInfo.Version;ClientSize=new Size(620,420);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;StartPosition=FormStartPosition.CenterScreen;Font=new Font("Segoe UI",10);BackColor=Color.FromArgb(243,246,244);
  Controls.Add(new Label{Text="MAC Versicherungsagentur",Tag="brand",ForeColor=Color.White,BackColor=Color.FromArgb(43,81,78),Font=new Font("Segoe UI",22,FontStyle.Bold),Dock=DockStyle.Top,Height=72,TextAlign=ContentAlignment.MiddleCenter});
  status.SetBounds(24,94,570,220);status.Text="Kundenklärung installieren oder reparieren\n\nProgrammdateien werden von GitHub geladen. Internet ist für die Installation erforderlich.\n\nZiel: "+target;Controls.Add(status);
  progress.SetBounds(24,326,570,18);Controls.Add(progress);
  install.Tag="primary";install.Text="Herunterladen und installieren";install.SetBounds(24,360,570,40);install.BackColor=Color.FromArgb(220,221,56);install.FlatStyle=FlatStyle.Flat;Controls.Add(install);install.Click+=async(s,e)=>await Run();
  new Palette(false).Apply(this);
  FormClosing+=(s,e)=>{if(busy){e.Cancel=true;MacDialog.Show(this,new Palette(false),"Installation läuft","Bitte warten, bis die Installation abgeschlossen ist.");}};
 }
 public static Dictionary<string,byte[]> Unpack(byte[] bytes){
  var result=new Dictionary<string,byte[]>();long total=0;
  using(var m=new MemoryStream(bytes))using(var z=new ZipArchive(m,ZipArchiveMode.Read)){
   foreach(var entry in z.Entries){if(!Files.Contains(entry.FullName)||result.ContainsKey(entry.FullName))throw new Exception("Das Programmpaket enthält unerwartete Dateien.");total+=entry.Length;if(total>50*1024*1024)throw new Exception("Programmpaket zu groß.");using(var s=entry.Open())using(var b=new MemoryStream()){s.CopyTo(b);result.Add(entry.FullName,b.ToArray());}}
  }
  if(result.Count!=Files.Length)throw new Exception("Das Programmpaket ist unvollständig.");return result;
 }
 async Task Run(){busy=true;install.Enabled=false;progress.Style=ProgressBarStyle.Marquee;string stage=null;
  try{
   status.Text="Programmdateien werden von Tunfisch77/MAC heruntergeladen …";
   var files=await Task.Run(()=>{byte[] bytes=Updater.Fetch(PayloadInfo.Url,20*1024*1024);if(!Updater.Verify(bytes,PayloadInfo.Hash))throw new Exception("Die Prüfsumme stimmt nicht. Bitte das aktuelle Setup verwenden. Es wurde nichts installiert.");return Unpack(bytes);});
   status.Text="Download geprüft. Anwendung wird vor der Installation getestet …";
   stage=Path.Combine(Path.GetTempPath(),"MAC-Setup-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(stage);
   foreach(var f in files){string path=Path.Combine(stage,f.Key);Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,f.Value);}
   string testRoot=stage;
   await Task.Run(()=>ProcessCheck.Run(Path.Combine(testRoot,"MAC-Kundenklaerung.exe"),"--self-test",testRoot,30000));
   status.Text="Programmdateien werden installiert …";
   await Task.Run(()=>{
    foreach(var p in Process.GetProcessesByName("MAC-Kundenklaerung")){using(p){string path;try{path=p.MainModule.FileName;}catch{continue;}if(string.Equals(Path.GetFullPath(path),Path.Combine(target,"MAC-Kundenklaerung.exe"),StringComparison.OrdinalIgnoreCase)&&!p.WaitForExit(15000))throw new Exception("Bitte die laufende MAC-App schließen und erneut installieren.");}}
    Directory.CreateDirectory(target);var backups=new Dictionary<string,byte[]>();foreach(var name in Files){string path=Path.Combine(target,name);backups[name]=File.Exists(path)?File.ReadAllBytes(path):null;}
    var written=new List<string>();try{foreach(var f in files){string path=Path.Combine(target,f.Key);Directory.CreateDirectory(Path.GetDirectoryName(path));written.Add(f.Key);File.WriteAllBytes(path,f.Value);}}catch{foreach(var name in written){try{string path=Path.Combine(target,name);if(backups[name]!=null)File.WriteAllBytes(path,backups[name]);else File.Delete(path);}catch{}}throw;}
   });
   string exe=Path.Combine(target,"MAC-Kundenklaerung.exe");string warning="";
   try{Shortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),"MAC Kundenklärung.lnk"),exe,target);}catch{warning+="\nStartmenü-Verknüpfung konnte nicht erstellt werden.";}
   try{Shortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"MAC Kundenklärung.lnk"),exe,target);}catch{warning+="\nDesktop-Verknüpfung konnte nicht erstellt werden.";}
   status.Text="Installation abgeschlossen.\n\nProgrammordner: "+target+warning;
   using(var app=Process.Start(new ProcessStartInfo(exe){UseShellExecute=true,WorkingDirectory=target})){
    bool exited=await Task.Run(()=>app.WaitForExit(2500));
    if(exited)status.Text+="\n\nDie App wurde sofort beendet (Code "+app.ExitCode+"). Bitte diese Meldung bei der Fehlersuche angeben.";
    else status.Text+="\n\nDie App wurde gestartet. Dieses Setup kannst du schließen.";
   }
   install.Text="Erneut installieren / reparieren";
  }catch(System.Net.WebException e){var response=e.Response as System.Net.HttpWebResponse;status.Text=response!=null&&response.StatusCode==System.Net.HttpStatusCode.NotFound?"Die Programmdateien fehlen noch auf GitHub.\n\nBitte zuerst im öffentlichen Release v"+BuildInfo.Version+" die Datei MAC-Programm-"+BuildInfo.Version+".zip bereitstellen. Danach dieses Setup erneut starten.":"Download fehlgeschlagen. Bitte Internetverbindung prüfen und erneut versuchen.\n\n"+e.Message;
  }catch(Exception e){status.Text="Installation nicht abgeschlossen.\n\n"+e.Message+"\n\nDu kannst die Installation erneut versuchen.";
  }finally{if(stage!=null){try{Directory.Delete(stage,true);}catch{}}busy=false;install.Enabled=true;progress.Style=ProgressBarStyle.Blocks;}
 }
 static void Shortcut(string path,string exe,string dir){
  Type t=Type.GetTypeFromProgID("WScript.Shell");object shell=Activator.CreateInstance(t);object shortcut=null;
  try{shortcut=t.InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,shell,new object[]{path});Type s=shortcut.GetType();s.InvokeMember("TargetPath",BindingFlags.SetProperty,null,shortcut,new object[]{exe});s.InvokeMember("WorkingDirectory",BindingFlags.SetProperty,null,shortcut,new object[]{dir});s.InvokeMember("IconLocation",BindingFlags.SetProperty,null,shortcut,new object[]{Path.Combine(dir,"assets","mac.ico")});s.InvokeMember("Save",BindingFlags.InvokeMethod,null,shortcut,null);}finally{if(shortcut!=null)System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shortcut);System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);}
 }
 static void Tests(){
  byte[] bad;using(var m=new MemoryStream()){using(var z=new ZipArchive(m,ZipArchiveMode.Create,true)){z.CreateEntry("../escape.exe");}bad=m.ToArray();}
  bool rejected=false;try{Unpack(bad);}catch{rejected=true;}if(!rejected)throw new Exception("Unsafe ZIP accepted");
  if(!Updater.Allowed(new Uri(PayloadInfo.Url))||PayloadInfo.Hash.Length!=64)throw new Exception("Invalid release configuration");
  Console.WriteLine("PASS: bootstrap URL, pinned SHA-256 and ZIP traversal rejection.");
 }
}

// Only the synthetic startup test is captured. No client workbook is opened.
public static class ProcessCheck {
 public static void Run(string executable,string arguments,string directory,int timeout) {
  var info=new ProcessStartInfo(executable,arguments){UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=directory,RedirectStandardOutput=true,RedirectStandardError=true};
  using(var process=Process.Start(info)) {
   var output=process.StandardOutput.ReadToEndAsync();
   var errors=process.StandardError.ReadToEndAsync();
   if(!process.WaitForExit(timeout)) {
    try{process.Kill();process.WaitForExit(5000);}catch{}
    throw new Exception("Der Anwendungstest hat das Zeitlimit überschritten. Es wurde nichts installiert.");
   }
   string text=errors.GetAwaiter().GetResult();string stdout=output.GetAwaiter().GetResult();
   if(process.ExitCode==0)return;
   if(string.IsNullOrWhiteSpace(text))text=stdout;
   if(string.IsNullOrWhiteSpace(text))text="Windows hat keine Fehlerdetails geliefert.";
   if(text.Length>16000)text=text.Substring(0,16000);
   throw new Exception("Anwendungstest fehlgeschlagen. Exitcode: "+process.ExitCode+" (0x"+process.ExitCode.ToString("X8")+").\r\n\r\n"+text.Trim()+"\r\n\r\nBitte diesen Fehlertext kopieren oder als Screenshot senden. Ein fehlendes .NET Framework ist damit noch nicht nachgewiesen.");
  }
 }
}
