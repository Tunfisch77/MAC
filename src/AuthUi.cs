using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MacApp {
 static class AuthUi {
  internal static Form Window(string title,int width,int height){return new Form{Text="MAC · "+title,ClientSize=new Size(width,height),Font=new Font("Segoe UI",10),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MinimizeBox=false,MaximizeBox=false,ShowInTaskbar=false,AutoScaleMode=AutoScaleMode.Font};}
  internal static TextBox Field(Form form,string label,int y,bool password=false){form.Controls.Add(new Label{Text=label,Location=new Point(24,y),Size=new Size(form.ClientSize.Width-48,24)});var input=new TextBox{UseSystemPasswordChar=password,MaxLength=password?128:256};form.Controls.Add(new InputFrame(input){Location=new Point(24,y+26),Size=new Size(form.ClientSize.Width-48,38)});return input;}
  internal static SoftButton Button(Form form,string text,int x,int y,int width,bool primary=false){var b=new SoftButton{Text=text,Location=new Point(x,y),Size=new Size(width,38),Tag=primary?"primary":"secondary"};form.Controls.Add(b);return b;}
  public static bool Login(IWin32Window owner,Palette palette,AuthClient client,string hint=""){
   using(var f=Window("Zentrale Anmeldung",560,650)){
    f.Controls.Add(new Label{Text="MAC · Willkommen",Font=new Font("Segoe UI",22,FontStyle.Bold),Location=new Point(24,18),Size=new Size(510,42)});
    f.Controls.Add(new Label{Text="Dein Konto wird online geprüft. Kundendaten bleiben lokal.",Location=new Point(24,65),Size=new Size(510,36),Tag="muted"});
    var server=Field(f,"Anmeldeserver (Adresse von der MAC-Administration)",106);server.Text=client.Server??"";
    var first=Field(f,"Vorname",180);var last=Field(f,"Nachname",254);var password=Field(f,"Passwort",328,true);
    var admin=new CheckBox{Text="Adminpasswort verwenden",Location=new Point(24,407),Size=new Size(245,28)};f.Controls.Add(admin);
    var remember=new CheckBox{Text="Angemeldet bleiben (max. 30 Tage)",Location=new Point(24,440),Size=new Size(480,28)};f.Controls.Add(remember);
    var help=new Label{Text="Admin: Namen für direkten Profilzugriff eingeben oder beide Namen für die Kontoverwaltung leer lassen.",Location=new Point(24,477),Size=new Size(510,47),Tag="muted"};f.Controls.Add(help);
    var error=new Label{Text=hint,Location=new Point(24,528),Size=new Size(510,64),Tag="error"};f.Controls.Add(error);
    var enter=Button(f,"Anmelden",338,600,198,true);var cancel=Button(f,"Schließen",24,600,140);cancel.DialogResult=DialogResult.Cancel;f.CancelButton=cancel;f.AcceptButton=enter;
    admin.CheckedChanged+=(s,e)=>{remember.Enabled=!admin.Checked;if(admin.Checked)remember.Checked=false;};bool busy=false;
    f.FormClosing+=(s,e)=>{if(busy)e.Cancel=true;};
    enter.Click+=async(s,e)=>{if(busy)return;busy=true;enter.Enabled=false;cancel.Enabled=false;error.Text="Verbindung wird geprüft …";try{await client.Configure(server.Text);await client.Login(first.Text,last.Text,password.Text,admin.Checked,remember.Checked);password.Clear();password.ClearUndo();busy=false;f.DialogResult=DialogResult.OK;}catch(Exception ex){error.Text=ex.Message;password.Clear();password.ClearUndo();}finally{busy=false;enter.Enabled=true;cancel.Enabled=true;}};
    palette.Apply(f);bool result=f.ShowDialog(owner)==DialogResult.OK;password.Clear();password.ClearUndo();return result;
   }
  }
  public static bool Password(IWin32Window owner,Palette palette,Action<string,string> unused,out string oldPassword,out string newPassword,bool own){
   oldPassword=newPassword=null;using(var f=Window(own?"Passwort ändern":"Beraterpasswort setzen",530,own?370:296)){
    TextBox old=own?Field(f,"Bisheriges Passwort",20,true):null;int top=own?94:20;var next=Field(f,"Neues Passwort · 14–128 Zeichen",top,true);var repeat=Field(f,"Neues Passwort wiederholen",top+74,true);
    var error=new Label{Location=new Point(24,top+145),Size=new Size(482,45),Tag="error"};f.Controls.Add(error);var ok=Button(f,"Übernehmen",310,f.ClientSize.Height-54,196,true);var cancel=Button(f,"Abbrechen",24,f.ClientSize.Height-54,150);cancel.DialogResult=DialogResult.Cancel;f.CancelButton=cancel;f.AcceptButton=ok;
    ok.Click+=(s,e)=>{if(next.Text.Length<14||next.Text.Length>128||next.Text!=repeat.Text){error.Text="Mindestens 14 Zeichen und identische Wiederholung erforderlich.";return;}f.DialogResult=DialogResult.OK;};palette.Apply(f);if(f.ShowDialog(owner)!=DialogResult.OK)return false;oldPassword=old==null?"":old.Text;newPassword=next.Text;next.Clear();repeat.Clear();if(old!=null)old.Clear();return true;
   }
  }
 }
 sealed class AdminWindow:Form {
  readonly AuthClient client;readonly Palette palette;readonly ListBox list=new ListBox();readonly Label message=new Label();bool busy;public bool Selected;
  public AdminWindow(AuthClient auth,Palette colors){client=auth;palette=colors;Text="MAC · Kontoverwaltung";ClientSize=new Size(740,570);StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;
   Controls.Add(new Label{Text="Berater verwalten",Font=new Font("Segoe UI",22,FontStyle.Bold),Location=new Point(24,20),Size=new Size(680,42)});Controls.Add(new Label{Text="Zentrale Konten für alle Standorte · Gemeinsamer Adminzugang",Location=new Point(24,70),Size=new Size(680,32),Tag="muted"});
   list.Location=new Point(24,112);list.Size=new Size(692,292);list.BorderStyle=BorderStyle.None;list.DrawMode=DrawMode.OwnerDrawFixed;list.ItemHeight=40;list.DrawItem+=(sender,e)=>{if(e.Index<0)return;bool active=(e.State&DrawItemState.Selected)!=0;using(var brush=new SolidBrush(active?palette.Selected:palette.Input))e.Graphics.FillRectangle(brush,e.Bounds);TextRenderer.DrawText(e.Graphics,list.Items[e.Index].ToString(),Font,new Rectangle(e.Bounds.X+12,e.Bounds.Y,e.Bounds.Width-24,e.Bounds.Height),palette.Text,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);};Controls.Add(list);
   var add=AuthUi.Button(this,"Anlegen",24,420,114);add.Click+=async(s,e)=>await Run(Add);
   var reset=AuthUi.Button(this,"Passwort setzen",146,420,154);reset.Click+=async(s,e)=>await Run(Reset);
   var toggle=AuthUi.Button(this,"Sperren / Freigeben",308,420,200);toggle.Click+=async(s,e)=>await Run(Toggle);
   var revoke=AuthUi.Button(this,"Abmeldungen",516,420,200);revoke.Text="Sitzungen beenden";revoke.Click+=async(s,e)=>await Run(Revoke);
   message.Location=new Point(24,468);message.Size=new Size(692,42);message.Tag="error";Controls.Add(message);
   var use=AuthUi.Button(this,"Als Berater öffnen",478,518,238,true);use.Click+=async(s,e)=>await Run(async()=>{var user=Current();await client.Assume(user);Selected=true;DialogResult=DialogResult.OK;});
   var cancel=AuthUi.Button(this,"Zurück",24,518,130);cancel.DialogResult=DialogResult.Cancel;CancelButton=cancel;
   Shown+=async(s,e)=>await Run(LoadUsers);FormClosing+=(s,e)=>{if(busy&&!Selected)e.Cancel=true;};palette.Apply(this);
  }
  AuthUser Current(){var user=list.SelectedItem as AuthUser;if(user==null)throw new Exception("Bitte einen Berater auswählen.");return user;}
  async Task Run(Func<Task> action){if(busy)return;busy=true;foreach(var b in Controls.OfType<Button>())b.Enabled=false;message.Text="";try{await action();}catch(Exception e){message.Text=e.Message;}finally{busy=false;foreach(var b in Controls.OfType<Button>())b.Enabled=true;}}
  async Task LoadUsers(){var reply=await client.Call("GET","/v1/admin/users");list.Items.Clear();if(reply.users!=null)list.Items.AddRange(reply.users);if(list.Items.Count==0)message.Text="Noch keine Beraterkonten. Über „Anlegen“ das erste Konto erstellen.";}
  async Task Add(){using(var f=AuthUi.Window("Berater anlegen",530,440)){
   var first=AuthUi.Field(f,"Vorname",18);var last=AuthUi.Field(f,"Nachname",90);var pass=AuthUi.Field(f,"Erstes Passwort · 14–128 Zeichen",162,true);var repeat=AuthUi.Field(f,"Passwort wiederholen",234,true);var error=new Label{Location=new Point(24,312),Size=new Size(482,57),Tag="error"};f.Controls.Add(error);var ok=AuthUi.Button(f,"Konto anlegen",300,382,206,true);var cancel=AuthUi.Button(f,"Abbrechen",24,382,150);cancel.DialogResult=DialogResult.Cancel;f.CancelButton=cancel;f.AcceptButton=ok;
   bool working=false;f.FormClosing+=(s,e)=>{if(working)e.Cancel=true;};ok.Click+=async(s,e)=>{if(working)return;if(pass.Text!=repeat.Text){error.Text="Passwörter stimmen nicht überein.";return;}working=true;ok.Enabled=cancel.Enabled=false;try{await client.Call("POST","/v1/admin/users",new{first=first.Text,last=last.Text,password=pass.Text});working=false;f.DialogResult=DialogResult.OK;}catch(Exception ex){error.Text=ex.Message;}finally{working=false;ok.Enabled=cancel.Enabled=true;}};palette.Apply(f);f.ShowDialog(this);pass.Clear();repeat.Clear();await LoadUsers();}}
  async Task Reset(){var user=Current();string old,next;if(!AuthUi.Password(this,palette,null,out old,out next,false))return;try{await client.Call("POST","/v1/admin/user",new{id=user.id,password=next});message.Text="Passwort gesetzt; bestehende Anmeldungen dieses Beraters beendet.";}finally{next=null;}}
  async Task Toggle(){var user=Current();if(MacDialog.Show(this,palette,"Kontostatus ändern",user.Name+(user.enabled?" sperren? Alle Anmeldungen werden beendet.":" wieder freigeben?"),new DialogChoice("Bestätigen",DialogResult.Yes),new DialogChoice("Abbrechen",DialogResult.Cancel))!=DialogResult.Yes)return;await client.Call("POST","/v1/admin/user",new{id=user.id,enabled=!user.enabled});await LoadUsers();}
  async Task Revoke(){var user=Current();if(MacDialog.Show(this,palette,"Alle Sitzungen beenden?","Alle PCs müssen sich für "+user.Name+" erneut anmelden.",new DialogChoice("Beenden",DialogResult.Yes),new DialogChoice("Abbrechen",DialogResult.Cancel))!=DialogResult.Yes)return;await client.Call("POST","/v1/admin/user",new{id=user.id});message.Text="Sitzungen beendet.";}
 }
}
