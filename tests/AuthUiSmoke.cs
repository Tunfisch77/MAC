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
  using(var tick=new Timer{Interval=400}){tick.Tick+=(s,e)=>{var f=Application.OpenForms.Cast<Form>().FirstOrDefault();if(f==null)return;tick.Stop();if(f.ClientSize.Height<640)throw new Exception("Login clipped");var boxes=f.Controls.OfType<InputFrame>().Select(x=>x.Input).ToArray();if(boxes.Length!=4||!boxes[3].UseSystemPasswordChar)throw new Exception("Password not masked");boxes[0].Text="https://mac-anmeldung.onrender.com";boxes[1].Text="Tuncay";boxes[2].Text="Guliyev";Capture(f,"mac130-login.png");sawLogin=true;f.DialogResult=DialogResult.Cancel;};tick.Start();if(AuthUi.Login(null,new Palette(false),client))throw new Exception("Cancel accepted login");}if(!sawLogin||client.Session!=null)throw new Exception("Login state");
  typeof(AuthClient).GetProperty("Server").GetSetMethod(true).Invoke(client,new object[]{args[0]});System.Threading.Tasks.Task.Run(async()=>{await client.Login("","","Synthetic Admin Password 47!",true,false);await client.Call("POST","/v1/admin/users",new{first="Tuncay",last="Guliyev",password="Synthetic Advisor 42!"});}).GetAwaiter().GetResult();
  using(var f=new AdminWindow(client,new Palette(true)))using(var tick=new Timer{Interval=500}){int steps=0;tick.Tick+=(s,e)=>{var list=f.Controls.OfType<ListBox>().First();if(list.Items.Count==0){if(++steps>20)throw new Exception("Admin list timeout");return;}tick.Stop();list.SelectedIndex=0;Capture(f,"mac130-admin.png");f.Controls.OfType<Button>().First(x=>x.Text=="Als Berater öffnen").PerformClick();};tick.Start();if(f.ShowDialog()!=DialogResult.OK||!f.Selected||client.Session.user.Name!="Tuncay Guliyev")throw new Exception("Admin selection failed");}
  System.Threading.Tasks.Task.Run(()=>client.Logout()).GetAwaiter().GetResult();Console.WriteLine("PASS: login layout, password masking, cancel, live admin list, profile selection and logout.");
 }catch(Exception ex){Console.Error.WriteLine(ex);Environment.ExitCode=1;}finally{Directory.Delete(dir,true);}}
}
