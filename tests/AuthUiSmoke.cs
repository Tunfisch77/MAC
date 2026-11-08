using System;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MacApp;
class AuthUiSmoke {
 static void Capture(Form form,string name){form.Refresh();Application.DoEvents();using(var bmp=new Bitmap(form.ClientSize.Width,form.ClientSize.Height)){using(var g=Graphics.FromImage(bmp))g.CopyFromScreen(form.PointToScreen(Point.Empty),Point.Empty,form.ClientSize);bmp.Save(Path.Combine(Path.GetTempPath(),name));}}
 [STAThread] static void Main(string[] args){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);string dir=Path.Combine(Path.GetTempPath(),"MAC-UI-Auth-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);try{
  var client=new AuthClient(dir);bool sawLogin=false;
  using(var tick=new Timer{Interval=400}){tick.Tick+=(s,e)=>{var f=Application.OpenForms.Cast<Form>().FirstOrDefault();if(f==null)return;tick.Stop();if(f.ClientSize.Height<600)throw new Exception("Login clipped");var boxes=f.Controls.OfType<InputFrame>().Select(x=>x.Input).ToArray();if(boxes.Length!=3||!boxes[2].UseSystemPasswordChar)throw new Exception("Password not masked");boxes[0].Text="Tuncay";boxes[1].Text="Guliyev";
   var show=f.Controls.OfType<Button>().Single(x=>x.Text=="Anzeigen");show.PerformClick();if(boxes[2].UseSystemPasswordChar)throw new Exception("Show password failed");show.PerformClick();if(!boxes[2].UseSystemPasswordChar)throw new Exception("Hide password failed");
   if(f.Controls.OfType<CheckBox>().Count()!=1)throw new Exception("Admin toggle still present");
   f.Controls.OfType<Button>().Single(x=>x.Text=="Anmelden").PerformClick();if(!f.Controls.OfType<Label>().Any(x=>x.Text=="Bitte dein Passwort eingeben."))throw new Exception("Empty password validation missing");Capture(f,"mac131-login.png");sawLogin=true;f.DialogResult=DialogResult.Cancel;};tick.Start();if(AuthUi.Login(null,new Palette(false),client))throw new Exception("Cancel accepted login");}if(!sawLogin||client.Session!=null)throw new Exception("Login state");
  typeof(AuthClient).GetProperty("Server").GetSetMethod(true).Invoke(client,new object[]{args[0]});using(var tick=new Timer{Interval=400}){tick.Tick+=(s,e)=>{var f=Application.OpenForms.Cast<Form>().FirstOrDefault();if(f==null)return;tick.Stop();var boxes=f.Controls.OfType<InputFrame>().Select(x=>x.Input).ToArray();boxes[2].Text="Synthetic Admin Password 47!";f.Controls.OfType<CheckBox>().Single().Checked=true;Capture(f,"mac131-login-dark.png");f.Controls.OfType<Button>().Single(x=>x.Text=="Anmelden").PerformClick();};tick.Start();if(!AuthUi.Login(null,new Palette(true),client)||client.Session.role!="admin"||client.Remember||File.Exists(Path.Combine(dir,"auth-session.bin")))throw new Exception("UI auto-admin login or remember failed");}
  System.Threading.Tasks.Task.Run(async()=>{await client.Call("POST","/v1/admin/users",new{first="Tuncay",last="Guliyev",password="Synthetic Advisor 42!"});}).GetAwaiter().GetResult();
  using(var f=new AdminWindow(client,new Palette(true)))using(var tick=new Timer{Interval=500}){int steps=0;tick.Tick+=(s,e)=>{var list=f.Controls.OfType<ListBox>().First();if(list.Items.Count==0){if(++steps>20)throw new Exception("Admin list timeout");return;}tick.Stop();list.SelectedIndex=0;Capture(f,"mac131-admin.png");f.Controls.OfType<Button>().First(x=>x.Text=="Als Berater öffnen").PerformClick();};tick.Start();if(f.ShowDialog()!=DialogResult.OK||!f.Selected||client.Session.user.Name!="Tuncay Guliyev")throw new Exception("Admin selection failed");}
  System.Threading.Tasks.Task.Run(()=>client.Logout()).GetAwaiter().GetResult();Console.WriteLine("PASS: login layout, password masking, cancel, live admin list, profile selection and logout.");
 }catch(Exception ex){Console.Error.WriteLine(ex);Environment.ExitCode=1;}finally{Directory.Delete(dir,true);}}
}
