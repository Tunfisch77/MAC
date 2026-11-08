using System;
using System.Reflection;
using System.Windows.Forms;
class InstallerUiSmoke {
 [STAThread] static void Main(){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
 foreach(bool success in new[]{false,true}){
  using(var f=new Installer(new string[0])){bool closed=false;f.FormClosed+=(s,e)=>closed=true;
   f.Shown+=(s,e)=>f.BeginInvoke(new Action(()=>{
    typeof(Installer).GetField("busy",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(f,true);
    f.FinishRun(success);
    if(closed!=success){Console.Error.WriteLine("Incorrect automatic setup close result");Environment.ExitCode=1;}
    if(!success){if(!f.Visible)Environment.ExitCode=1;f.Close();}
   }));f.ShowDialog();
  }
 }
 Console.WriteLine("PASS: setup closes automatically after success; failed/incomplete run remains visible and can be closed manually.");
 }
}
