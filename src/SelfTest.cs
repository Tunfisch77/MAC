using System;
using System.IO;
using System.Linq;
using System.IO.Compression;
using System.Text;
using System.Security.Cryptography;
namespace MacApp {
static class SelfTest {
 static void Assert(bool ok,string label){if(!ok)throw new Exception("Test fehlgeschlagen: "+label);}
 public static void Run(){
  string template=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","template.xlsm");
  using(var b=Book.Load(template)){
   Assert(b.Rows.Count==0,"Vorlage ohne Kunden");Assert(b.Headers.Length==10,"zehn Spalten");
   var r=new Record{Values=new[]{"TESTKUNDE","00000123","HHV,RS,PHV","=SUM(1,2)","Mahnstufe 1","durch VN (Vertragsgeber)","TEST-1","ja","4,29","1234,56"}};
   Book.Validate(r);b.Rows.Add(r);var bytes=b.Export();
   using(var m=new MemoryStream(bytes))using(var z=new ZipArchive(m,ZipArchiveMode.Read)){
    using(var s=z.GetEntry("xl/vbaProject.bin").Open())using(var v=new MemoryStream()){s.CopyTo(v);Assert(v.ToArray().SequenceEqual(b.Parts["xl/vbaProject.bin"]),"Makros bytegleich");}
    using(var s=z.GetEntry("xl/worksheets/sheet1.xml").Open())using(var reader=new StreamReader(s)){string xml=reader.ReadToEnd();Assert(xml.Contains("00000123"),"führende Nullen");Assert(xml.Contains("0.0429"),"Prozentumrechnung");Assert(xml.Contains("1234.56"),"Euro");Assert(xml.Contains("HHV,RS,PHV"),"Mehrfachauswahl");Assert(!xml.Contains("<f>"),"keine Formeleinschleusung");}
   }
   byte[] attributed=b.Export("Tuncay Guliyev");using(var ms=new MemoryStream(attributed))using(var check=Book.Load(ms)){
    Assert(check.Rows[0].Values.SequenceEqual(r.Values),"Beraterkopf ändert keine Kundendaten");Assert(check.Parts["xl/vbaProject.bin"].SequenceEqual(b.Parts["xl/vbaProject.bin"]),"Makros bei Beraterexport unverändert");
    var sheet=check.Xml("xl/worksheets/sheet1.xml");Assert(sheet.Descendants(Book.N+"c").Any(c=>(string)c.Attribute("r")=="B3"&&Book.Text(c,new string[0])=="Berater: Tuncay Guliyev"),"Beratername in B3");Assert(sheet.Descendants(Book.N+"pane").Any(c=>(string)c.Attribute("topLeftCell")=="A5"),"Fixierung bei Beraterexport");
    byte[] renamed=check.Export("Anna Muster");using(var second=new MemoryStream(renamed))using(var reread=Book.Load(second))Assert(reread.Xml("xl/worksheets/sheet1.xml").Descendants(Book.N+"c").Any(c=>(string)c.Attribute("r")=="B3"&&Book.Text(c,new string[0])=="Berater: Anna Muster"),"Beraterwechsel im Export");Array.Clear(renamed,0,renamed.Length);
   }Array.Clear(attributed,0,attributed.Length);
   foreach(string badUrl in new[]{"http://example.com","https://user:pass@example.com","https://example.com/path","https://example.com?token=secret","https://localhost","https://example.com:444"}){bool blocked=false;try{AuthClient.ValidateServer(badUrl);}catch{blocked=true;}Assert(blocked,"Ungültiger Anmeldeserver abgewiesen");}
   Assert(AuthClient.ValidateServer("https://example.com/")=="https://example.com","HTTPS Serveradresse");Assert(AuthClient.SafeName("Tuncay Guliyev")=="Tuncay_Guliyev","Sicherer Dateiname");
   bool oldServer=false;try{AuthClient.CheckService(new AuthReply{service="mac-auth",api=1});}catch(AuthFailure){oldServer=true;}Assert(oldServer,"Alter Server verständlich abgewiesen");AuthClient.CheckService(new AuthReply{service="mac-auth",api=1,auto_login=true});
   Assert(AuthClient.ServerError(400,"Nur HTTPS ist erlaubt.").Contains("FORWARDED_ALLOW_IPS"),"HTTPS-Konfigurationsfehler erklärt");
   bool invalid=false;try{Book.Number("abc");}catch{invalid=true;}Assert(invalid,"ungültige Zahl");
   for(int i=0;i<10;i++){
    var missing=r.Clone();missing.Values[i]=" ";invalid=false;try{Book.Validate(missing);}catch{invalid=true;}Assert(invalid,"Pflichtfeld "+i);missing.Clear();
   }
   var unspecified=r.Clone();for(int i=0;i<10;i++)unspecified.Values[i]=Book.NotSpecified;unspecified.Values[1]="87654321";Book.Validate(unspecified);
   b.Rows.Add(unspecified);byte[] marked=b.Export();
   using(var stream=new MemoryStream(marked))using(var roundtrip=Book.Load(stream)){
    Assert(roundtrip.Rows[1].Values.Where((v,i)=>i!=1).All(v=>v==Book.NotSpecified),"Ausnahme in allen neun erlaubten Feldern, einschließlich Zahlen");
    Assert(roundtrip.Rows[0].Values.SequenceEqual(r.Values),"Werte nach Wiederimport unverändert");
    Assert(roundtrip.Xml("xl/worksheets/sheet1.xml").Descendants(Book.N+"dataValidation").All(v=>v.Element(Book.N+"formula1").Value.Contains(Book.NotSpecified)),"Excel-Auswahllisten mit Ausnahme");
   }Array.Clear(marked,0,marked.Length);
   unspecified.Values[8]="";invalid=false;try{b.Export();}catch{invalid=true;}Assert(invalid,"Unvollständiger Datensatz blockiert gesamten Export");unspecified.Values[8]=Book.NotSpecified;
   unspecified.Values[2]=Book.NotSpecified+", EH";invalid=false;try{Book.Validate(unspecified);}catch{invalid=true;}Assert(invalid,"Sparte-Ausnahme exklusiv");unspecified.Values[2]=Book.NotSpecified;
   foreach(string bad in new[]{"1234567","1234567890123","1234ABCD","1234 5678","1234-5678","１２３４５６７８",Book.NotSpecified," 12345678"})Assert(!Book.ValidPolicy(bad),"Ungültige Polizzennummer");
   Assert(Book.ValidPolicy("00001234")&&Book.ValidPolicy("123456789012"),"Längengrenzen/führende Nullen");
   Assert(Book.NormalizeName("mustermann max")=="Mustermann Max"&&Book.NormalizeName("  anna McDonald")=="  Anna McDonald","Namensformatierung");
   unspecified.Values[1]=r.Values[1];invalid=false;try{b.Export();}catch{invalid=true;}Assert(invalid,"Doppelpolizze blockiert Export");unspecified.Values[1]="87654321";
   Assert(b.DuplicateIndex(r.Values[1],0)==-1&&b.DuplicateIndex(r.Values[1],-1)==0,"Bearbeiten ignoriert eigenen Datensatz");
   unspecified.Values[0]=r.Values[0];Book.Validate(unspecified);var duplicateNameBytes=b.Export();Array.Clear(duplicateNameBytes,0,duplicateNameBytes.Length);
   var session=new SessionState{Book=b,Draft=(string[])r.Values.Clone(),Baseline=string.Join("\u001f",r.Values),Editing=0,Dirty=true};session.Draft[9]="";session.Draft[3]="UNFERTIGER_TESTENTWURF";
   byte[] encoded=SessionCodec.Encode(session);using(var restored=SessionCodec.Decode(encoded)){Assert(restored.Book.Rows.Count==2&&restored.Draft[9]==""&&restored.Draft[3]=="UNFERTIGER_TESTENTWURF"&&restored.Dirty&&restored.Editing==0,"Unvollständige Sitzung vollständig wiederhergestellt");Assert(restored.Book.Parts["xl/vbaProject.bin"].SequenceEqual(b.Parts["xl/vbaProject.bin"]),"Makros in Wiederherstellung");}byte[] truncated=encoded.Take(encoded.Length-1).ToArray();invalid=false;try{using(var ignored=SessionCodec.Decode(truncated)){} }catch{invalid=true;}Assert(invalid,"Abgeschnittene Sitzung abgewiesen");Array.Clear(truncated,0,truncated.Length);Array.Clear(encoded,0,encoded.Length);
   string testDirectory=Path.Combine(Path.GetTempPath(),"MAC-Synthetic-Test-"+Guid.NewGuid().ToString("N"));
   try{using(var state=new LocalState(testDirectory)){
    state.Acquire();state.WritePreferences(new Preferences{Dark=true,Width=1240,Height=850,Split=.43});Assert(state.ReadPreferences().Dark&&state.ReadPreferences().Width==1240,"Einstellungen persistent");state.WritePreferences(new Preferences{Dark=false});Assert(!state.ReadPreferences().Dark,"Einstellungen atomar ersetzt");
    invalid=false;using(var second=new LocalState(testDirectory)){try{second.Acquire();}catch(IOException){invalid=true;}}Assert(invalid,"Zweite Instanz blockiert");
    Directory.CreateDirectory(Path.Combine(testDirectory,"settings.json.new"));invalid=false;try{state.WritePreferences(new Preferences{Dark=true});}catch{invalid=true;}Assert(invalid&&!state.ReadPreferences().Dark,"Fehlgeschlagener Schreibvorgang erhält letzte Einstellungen");Directory.Delete(Path.Combine(testDirectory,"settings.json.new"));
    if(Environment.OSVersion.Platform==PlatformID.Win32NT){
     AuthClient.TestTicketStorage(testDirectory);
     state.Save(session);Assert(state.HasRecovery,"Entwurf gespeichert");byte[] cipher=File.ReadAllBytes(Path.Combine(testDirectory,"session.macdraft"));Assert(!Encoding.UTF8.GetString(cipher).Contains("UNFERTIGER_TESTENTWURF"),"Kein Klartext auf Datenträger");
     using(var restored=state.Restore())Assert(restored.Draft[3]==session.Draft[3],"Windows DPAPI Rundlauf");cipher[cipher.Length/2]^=1;File.WriteAllBytes(Path.Combine(testDirectory,"session.macdraft"),cipher);invalid=false;try{using(var ignored=state.Restore()){} }catch{invalid=true;}Assert(invalid,"Manipulierter Entwurf abgewiesen");state.Save(session);state.DeleteRecovery();Assert(!state.HasRecovery,"Entwurf gelöscht");state.UseProfile(new string('a',64));state.Save(session);state.UseProfile(new string('b',64));Assert(!state.HasRecovery,"Anderer Berater sieht keinen Entwurf");state.UseProfile(new string('a',64));Assert(state.HasRecovery,"Ursprünglicher Beraterentwurf bleibt");state.DeleteRecovery();Array.Clear(cipher,0,cipher.Length);
    }else Console.WriteLine("SKIP: Windows DPAPI native test; checked by self-test on Windows.");
   }}finally{if(Directory.Exists(testDirectory))Directory.Delete(testDirectory,true);}session.Book=null;session.Dispose();
   Assert(b.Xml("xl/worksheets/sheet1.xml").Descendants(Book.N+"pane").Any(p=>(string)p.Attribute("topLeftCell")=="A5"),"fixierte Zeilen 1–4");
   Assert(b.Xml("xl/worksheets/sheet1.xml").Descendants(Book.N+"dataValidation").Count()==4,"vier Excel-Auswahllisten");
  }
  var now=new DateTime(2026,11,8,12,0,0,DateTimeKind.Utc);var schedule=new UpdateSchedule(now);
  Assert(!schedule.IsDue(now.AddSeconds(29))&&schedule.IsDue(now.AddSeconds(30)),"Erste Updateprüfung nach 30 Sekunden");schedule.Checked(now.AddSeconds(30));
  Assert(!schedule.IsDue(now.AddMinutes(59))&&schedule.IsDue(now.AddSeconds(3630)),"Stündliche Prüfung ohne versteckten Dauerscan");
  Assert(!UpdateSchedule.ShouldNotify("2.0.0","2.0.0",now.Ticks,now.AddHours(23.99)),"Später unterdrückt gleiche Version 24 Stunden");
  Assert(UpdateSchedule.ShouldNotify("2.0.0","2.0.0",now.Ticks,now.AddHours(24)),"Erinnerung ab 24 Stunden");
  Assert(UpdateSchedule.ShouldNotify("2.0.1","2.0.0",now.Ticks,now),"Neue Version trotz alter Zurückstellung");
  Assert(UpdateSchedule.ShouldNotify("2.0.0","2.0.0",long.MaxValue,now),"Defekte gespeicherte Uhrzeit blockiert Updates nicht");
  string prefFolder=Path.Combine(Path.GetTempPath(),"MAC-Update-Test-"+Guid.NewGuid().ToString("N"));
  try{using(var state=new LocalState(prefFolder)){state.WritePreferences(new Preferences{UpdateNoticeVersion="2.0.0",UpdateNoticeUtcTicks=now.Ticks});var loaded=state.ReadPreferences();Assert(!UpdateSchedule.ShouldNotify("2.0.0",loaded.UpdateNoticeVersion,loaded.UpdateNoticeUtcTicks,now.AddHours(1)),"Später bleibt nach Neustart erhalten");}}finally{if(Directory.Exists(prefFolder))Directory.Delete(prefFolder,true);}
  string hash=new string('a',64);var current=new Version(Updater.Version);string future=new Version(current.Major,current.Minor+1,0).ToString();
  string fixture="{\"draft\":false,\"prerelease\":false,\"tag_name\":\"v1.1.0\",\"assets\":[{\"name\":\"MAC-Setup-1.1.0.exe\",\"browser_download_url\":\"https://github.com/Tunfisch77/MAC/releases/download/v1.1.0/MAC-Setup-1.1.0.exe\",\"digest\":\"sha256:"+hash+"\"}]}";
  fixture=fixture.Replace("1.1.0",future);
  Assert(Updater.Parse(fixture).Version==future,"neues Release");
  Assert(Updater.Parse(fixture.Replace(future,Updater.Version))==null,"gleiche Version");
  Assert(Updater.Parse(fixture.Replace("\"prerelease\":false","\"prerelease\":true"))==null,"Vorabversion");
  bool rejected=false;try{Updater.Parse(fixture.Replace("Tunfisch77/MAC","fremd/MAC"));}catch{rejected=true;}Assert(rejected,"fremdes Repository");
  rejected=false;try{Updater.Parse(fixture.Replace("sha256:","wrong:"));}catch{rejected=true;}Assert(rejected,"fehlender Hash");
  rejected=false;try{Updater.Parse(fixture.Replace("MAC-Setup-","Anderes-Setup-"));}catch{rejected=true;}Assert(rejected,"fehlender Installer");
  Assert(Updater.Parse(fixture.Replace("\"draft\":false","\"draft\":true"))==null,"Entwurf");
  Assert(!Updater.Allowed(new Uri("http://github.com/Tunfisch77/MAC/releases/download/v1.1.0/a.exe")),"unverschlüsselter Download");
  Assert(Updater.Verify(Encoding.UTF8.GetBytes("test"),"9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08"),"gültige Prüfsumme");
  Assert(!Updater.Allowed(new Uri("https://example.com/file.exe")),"fremder Redirect");Assert(!Updater.Verify(Encoding.UTF8.GetBytes("test"),hash),"falsche Prüfsumme");
  Console.WriteLine("PASS: Export, Datenformate, Makroerhalt, Pflicht-/Zahlprüfung, lokale Update-Fixtures.");
 }
}
}
