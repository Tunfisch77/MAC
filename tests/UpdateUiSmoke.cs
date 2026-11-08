using System;
using System.Linq;
using System.Reflection;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using MacApp;
class UpdateUiSmoke {
 static bool captured;
 static object Get(object o,string n){return o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);}
 static void Set(object o,string n,object v){o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);}
 static Task Poll(MainForm f,Func<Task<Release>> probe,DateTime now){return (Task)typeof(MainForm).GetMethod("PollUpdates",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(f,new object[]{probe,now});}
 static Task Manual(MainForm f,Release r){return (Task)typeof(MainForm).GetMethods(BindingFlags.Instance|BindingFlags.NonPublic).Single(x=>x.Name=="CheckUpdates"&&x.GetParameters().Length==2).Invoke(f,new object[]{new Func<Task<Release>>(()=>Task.FromResult(r)),false});}
 static void Assert(bool ok,string label){if(!ok)throw new Exception(label);}
 static Timer Dismiss(Func<Form,DialogResult> answer){var t=new Timer{Interval=50};t.Tick+=(s,e)=>{var popup=Application.OpenForms.Cast<Form>().FirstOrDefault(x=>!(x is MainForm));if(popup!=null){if(!captured&&popup.Text.Contains("Update verfügbar")){popup.Refresh();Application.DoEvents();using(var bmp=new Bitmap(popup.ClientSize.Width,popup.ClientSize.Height)){using(var g=Graphics.FromImage(bmp))g.CopyFromScreen(popup.PointToScreen(Point.Empty),Point.Empty,popup.ClientSize);bmp.Save(Path.Combine(Path.GetTempPath(),"mac132-update.png"));}captured=true;}popup.DialogResult=answer(popup);}};t.Start();return t;}
 [STAThread] static void Main(){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);var f=new MainForm(true);
 f.Shown+=async(s,e)=>{try{
  int probes=0,popups=0;DateTime due=DateTime.UtcNow.AddMinutes(1);var fields=(Control[])Get(f,"fields");fields[0].Text="Synthetic Draft";
  using(var guard=Dismiss(p=>{popups++;return DialogResult.Cancel;})){
   await Poll(f,()=>{probes++;throw new Exception("Synthetic network failure");},due);
   Assert(probes==1&&popups==0&&((Control)Get(f,"editor")).Enabled,"Automatic error disrupted editor");
   await Poll(f,()=>{probes++;return Task.FromResult<Release>(null);},due.AddMinutes(59));Assert(probes==1,"Retried before hour elapsed");
  }
  var release=new Release{Version="9.0.0",Name="Synthetic.exe"};var completion=new TaskCompletionSource<Release>();
  Task background=Poll(f,()=>{probes++;return completion.Task;},due.AddHours(2));
  Assert((bool)Get(f,"checkingAutomatically")&&((Control)Get(f,"editor")).Enabled,"Background probe blocked input");
  fields[0].Text="Draft Changed During Probe";
  await Poll(f,()=>{throw new Exception("Overlapping probe");},due.AddHours(2));
  Set(f,"fileDialogOpen",true);completion.SetResult(release);await background;
  Assert(Get(f,"pendingUpdate")!=null,"Update was not deferred behind file dialog");Set(f,"fileDialogOpen",false);
  using(var dismiss=Dismiss(p=>{popups++;Assert(p.Text.Contains("Update verfügbar"),"Wrong popup");return DialogResult.Cancel;}))await Poll(f,()=>{throw new Exception("Unneeded request for queued update");},due.AddHours(2));
  Assert(popups==1&&fields[0].Text=="Draft Changed During Probe","Later lost draft");
  using(var guard=Dismiss(p=>{popups++;return DialogResult.Cancel;}))await Poll(f,()=>Task.FromResult(release),due.AddHours(4));
  Assert(popups==1,"Same version notified within 24 hours");
  using(var dismiss=Dismiss(p=>{popups++;return DialogResult.Cancel;}))await Manual(f,release);
  Assert(popups==2,"Manual update ignored due to reminder snooze");
  int preparation=0;using(var dismiss=Dismiss(p=>{preparation++;return p.Text.Contains("Update verfügbar")?DialogResult.Yes:DialogResult.Cancel;}))await Manual(f,release);
  Assert(preparation==2&&!f.IsDisposed&&fields[0].Text=="Draft Changed During Probe","Cancelled export changed draft or started installation");
  Set(f,"pendingUpdate",null);var late=new TaskCompletionSource<Release>();Task inFlight=Poll(f,()=>late.Task,DateTime.UtcNow.AddDays(2));
  f.Close();late.SetResult(release);await inFlight;Assert(f.IsDisposed,"Closing during background check failed");
  Console.WriteLine("PASS: hourly schedule, silent failures, editable during probe, no overlap, modal deferral, Later, manual override, unsaved-export cancellation and close during probe.");
 }catch(Exception ex){Console.Error.WriteLine(ex);Environment.ExitCode=1;f.Dispose();Application.Exit();}};
 Application.Run(f);
 }
}
