using System;
using System.Drawing;
using System.Windows.Forms;
using MacApp;

// Compile beside the app for a synthetic test of the minimum window size.
class SmallWindow {
 [STAThread] static void Main() {
  Application.EnableVisualStyles();
  Application.SetCompatibleTextRenderingDefault(false);
  var form=new MainForm(true);
  form.Size=new Size(1100,720);
  form.Shown+=(s,e)=>form.BeginInvoke(new Action(()=>{
   try {form.Smoke();Environment.Exit(0);}
   catch(Exception failure){Console.Error.WriteLine(failure);Environment.Exit(1);}
  }));
  Application.Run(form);
 }
}
