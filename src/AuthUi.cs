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
   using(var f=Window("Zentrale Anmeldung",560,610)){
    f.Controls.Add(new Label{Text="MAC · Willkommen",Font=new Font("Segoe UI",22,FontStyle.Bold),Location=new Point(24,18),Size=new Size(510,42)});
    f.Controls.Add(new Label{Text="Dein Konto wird online geprüft. Kundendaten bleiben lokal.",Location=new Point(24,65),Size=new Size(510,36),Tag="muted"});
    var first=Field(f,"Vorname",106);var last=Field(f,"Nachname",180);var password=Field(f,"Passwort",254,true);first.MaxLength=last.MaxLength=80;
    password.Parent.Width=358;
    var show=Button(f,"Anzeigen",394,280,142);show.AccessibleName="Passwort anzeigen";
    show.Click+=(s,e)=>{password.UseSystemPasswordChar=!password.UseSystemPasswordChar;show.Text=password.UseSystemPasswordChar?"Anzeigen":"Verbergen";show.AccessibleName="Passwort "+(password.UseSystemPasswordChar?"anzeigen":"verbergen");};
    f.Deactivate+=(s,e)=>{password.UseSystemPasswordChar=true;show.Text="Anzeigen";show.AccessibleName="Passwort anzeigen";};
    var remember=new CheckBox{Text="Angemeldet bleiben · nur für Berater (max. 30 Tage)",Location=new Point(24,334),Size=new Size(510,28)};f.Controls.Add(remember);
    f.Controls.Add(new Label{Text="Adminpasswort wird automatisch erkannt. Für die Kontoverwaltung beide Namensfelder leer lassen; für Profilzugriff den Beraternamen eingeben.",Location=new Point(24,377),Size=new Size(510,66),Tag="muted"});
    var error=new Label{Text=hint,Location=new Point(24,455),Size=new Size(510,88),Tag="error"};f.Controls.Add(error);
    var enter=Button(f,"Anmelden",338,554,198,true);var cancel=Button(f,"Schließen",24,554,140);cancel.DialogResult=DialogResult.Cancel;f.CancelButton=cancel;f.AcceptButton=enter;
    bool busy=false;f.FormClosing+=(s,e)=>{if(busy)e.Cancel=true;};f.Shown+=(s,e)=>first.Focus();
    enter.Click+=async(s,e)=>{if(busy)return;if(string.IsNullOrEmpty(password.Text)){error.Text="Bitte dein Passwort eingeben.";password.Focus();return;}if(string.IsNullOrWhiteSpace(first.Text)!=string.IsNullOrWhiteSpace(last.Text)){error.Text="Vorname und Nachname gemeinsam eingeben. Für die Kontoverwaltung beide leer lassen.";return;}busy=true;enter.Enabled=cancel.Enabled=show.Enabled=first.Enabled=last.Enabled=password.Enabled=remember.Enabled=false;password.UseSystemPasswordChar=true;show.Text="Anzeigen";error.Text="Anmeldung wird geprüft …";try{await client.Configure();await client.Login(first.Text,last.Text,password.Text,remember.Checked);password.Clear();password.ClearUndo();busy=false;f.DialogResult=DialogResult.OK;}catch(Exception ex){error.Text=ex.Message;password.Clear();password.ClearUndo();}finally{busy=false;enter.Enabled=cancel.Enabled=show.Enabled=first.Enabled=last.Enabled=password.Enabled=remember.Enabled=true;password.Focus();}};
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
