using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MacApp {
 static class Program {
  [STAThread] static void Main(string[] args){
   if(args.Contains("--self-test")){try{SelfTest.Run();Environment.Exit(0);}catch(Exception e){Console.Error.WriteLine(e.ToString());Environment.Exit(1);}return;}
   Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
   Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
   Application.ThreadException+=(s,e)=>{var active=Application.OpenForms.OfType<MainForm>().FirstOrDefault();if(active!=null)active.ShowFailure();else MacDialog.Show(Form.ActiveForm,new Palette(false),"Aktion nicht abgeschlossen","Bitte Eingaben prüfen und erneut versuchen.");};
   try {
    var main=new MainForm(args.Contains("--ui-smoke"));
    if(args.Contains("--ui-smoke"))main.Shown+=(s,e)=>main.BeginInvoke(new Action(()=>{try{main.Smoke();Environment.Exit(0);}catch(Exception failure){Console.Error.WriteLine(failure.ToString());Environment.Exit(1);}}));
    Application.Run(main);
   }catch(Exception failure){
    if(args.Contains("--ui-smoke")){Console.Error.WriteLine(failure.ToString());Environment.Exit(1);return;}
    if(failure is IOException&&failure.Message.StartsWith("Die MAC-App ist bereits geöffnet",StringComparison.Ordinal)){MacDialog.Show(null,new Palette(false),"App bereits geöffnet",failure.Message);return;}
    MacDialog.Show(null,new Palette(false),"Start nicht möglich","Bitte die MAC-App mit dem Installer erneut installieren. Die Dateien im Unterordner assets müssen vorhanden sein.\r\n\r\nFehlercode: "+failure.GetType().Name+" / 0x"+failure.HResult.ToString("X8"));Environment.ExitCode=1;
   }
  }
 }
 public sealed class MainForm:Form {
  AuthClient auth;readonly Timer authTimer=new Timer{Interval=60000};bool authBusy;readonly Label identity=new Label();Button accountButton;
  Book book; Palette palette=new Palette(false);
  readonly DataGridView grid=new SmoothGrid();readonly Control[] fields=new Control[10];readonly Label[] errors=new Label[10];readonly RowStyle[] errorRows=new RowStyle[10];
  readonly MultiSelect sparten=new MultiSelect();readonly Label status=new Label(),counts=new Label(),formTitle=new Label();
  readonly TextBox search=new TextBox();FormScrollPanel scroller;Control toolbar,editor;Button themeButton,commitButton,duplicateJump,undoButton;SplitContainer split;
  readonly CheckBox incompleteOnly=new CheckBox{Text="Noch zu ergänzen",AutoSize=true};
  readonly Timer saveTimer=new Timer{Interval=2000};LocalState local;Preferences preferences=new Preferences();bool testing,ready,closing,recoveryPending,preferencesPending,initializing=true;
  readonly Stack<RemovedEntry> removed=new Stack<RemovedEntry>();
  sealed class RemovedEntry {public Record Record;public int Index;}
  int editing=-1;bool dirty,updating,showErrors;string draftBaseline="";
  readonly string[] standardSparten={"EH (Eigenheim)","UV (Unfall)","KFZ (Kraftfahrzeug)","RS (Rechtsschutz)","HHV (Haushaltsversicherung)","PHV (Private Haftpflichtversicherung)","FRS (Firmenrechtsschutz)","KV (Krankenversicherung)","LV (Lebensversicherung)","BU (Berufsunfähigkeit)","BHV (Betriebshaftpflichtversicherung)","BINV (Betriebsinhaltsversicherung)","GEB (Gebäudeversicherung)","RV (Reiseversicherung)","CY (Cyberversicherung)"};
  string Template {get{return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","template.xlsm");}}
  public MainForm(bool testMode=false){
   testing=testMode;if(!testing){local=new LocalState(LocalState.ProfileFolder);local.Acquire();try{preferences=local.ReadPreferences();}catch{preferences=new Preferences();}}palette=new Palette(preferences.Dark);DoubleBuffered=true;
   var area=Screen.PrimaryScreen.WorkingArea;Text="MAC Versicherungsagentur · Kundenklärung · "+Updater.Version;Width=Math.Min(Math.Max(1100,preferences.Width),area.Width);Height=Math.Min(Math.Max(720,preferences.Height),area.Height);MinimumSize=new Size(Math.Min(1100,area.Width),Math.Min(720,area.Height));StartPosition=FormStartPosition.CenterScreen;Font=new Font("Segoe UI",10);Tag="background";
   Icon=new Icon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","mac.ico"));book=Book.Load(Template);
   var root=Table(1,4);root.Padding=Padding.Empty;root.Tag="background";root.RowStyles.Add(new RowStyle(SizeType.Absolute,82));root.RowStyles.Add(new RowStyle(SizeType.Absolute,54));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,30));Controls.Add(root);
   var banner=new Panel{Dock=DockStyle.Fill,Tag="brand",Padding=new Padding(20),Margin=Padding.Empty};root.Controls.Add(banner,0,0);
   var logo=new PictureBox{Location=new Point(18,12),Size=new Size(170,55),SizeMode=PictureBoxSizeMode.Zoom,Tag="brand",Image=Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","logo.png"))};banner.Controls.Add(logo);
   banner.Controls.Add(new Label{Text="Kundenklärung",Location=new Point(212,8),Size=new Size(600,38),Tag="brand",Font=new Font("Segoe UI",22,FontStyle.Bold)});
   identity.Text="Zentrale Anmeldung · Kundendaten bleiben lokal";identity.Location=new Point(214,49);identity.Size=new Size(640,26);identity.AutoEllipsis=true;identity.Tag="brand";banner.Controls.Add(identity);
   themeButton=Btn("Dunkelmodus",ToggleTheme,false);themeButton.AutoSize=false;themeButton.Size=new Size(144,36);banner.Controls.Add(themeButton);banner.Layout+=(s,e)=>themeButton.Location=new Point(banner.ClientSize.Width-themeButton.Width-18,23);
   var bar=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(12,9,12,7),WrapContents=false,AutoScroll=true,Tag="background",Margin=Padding.Empty};toolbar=bar;root.Controls.Add(bar,0,1);
   bar.Controls.Add(Btn("+ Neuer Eintrag",()=>{if(DiscardDraft())ClearForm();},false));bar.Controls.Add(Btn("Excel öffnen",OpenBook,false));bar.Controls.Add(Btn("Zurücksetzen",Reset,false));bar.Controls.Add(Btn("Nach Updates suchen",async ()=>await CheckUpdates(),false));bar.Controls.Add(Btn("Als Excel speichern",async ()=>await SaveAuthorized(),true));accountButton=Btn("Konto / Abmelden",async ()=>await AccountMenu(),false);bar.Controls.Add(accountButton);
   split=new SplitContainer{Size=new Size(1300,600),Dock=DockStyle.Fill,SplitterDistance=535,Panel1MinSize=450,Panel2MinSize=430,SplitterWidth=8,Tag="background",Margin=Padding.Empty};editor=split;root.Controls.Add(split,0,2);
   var left=Table(1,3);left.Padding=new Padding(18);left.RowStyles.Add(new RowStyle(SizeType.Absolute,86));left.RowStyles.Add(new RowStyle(SizeType.Percent,100));left.RowStyles.Add(new RowStyle(SizeType.Absolute,48));split.Panel1.Controls.Add(left);
   var heading=new Panel{Dock=DockStyle.Fill};formTitle.Text="Neuer Eintrag";formTitle.Font=new Font("Segoe UI",17,FontStyle.Bold);formTitle.Dock=DockStyle.Top;formTitle.Height=34;heading.Controls.Add(formTitle);heading.Controls.Add(new Label{Text="Polizzennummer: 8–12 Ziffern, eindeutig.\nAndere Felder: Angabe oder „Nicht angegeben“.",Dock=DockStyle.Bottom,Height=46,Tag="muted"});left.Controls.Add(heading,0,0);
   scroller=new FormScrollPanel{Dock=DockStyle.Fill};left.Controls.Add(scroller,0,1);
   var form=new SmoothTable{Dock=DockStyle.Top,AutoSize=true,ColumnCount=1,Margin=Padding.Empty,Padding=new Padding(0,0,12,0)};form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));scroller.Controls.Add(form);int row=0;
   for(int i=0;i<10;i++){
    int index=i;
    if(i==0||i==3||i==8){var label=new Label{Text=i==0?"01   VERTRAG UND SPARTE":i==3?"02   BEARBEITUNG":"03   BETRÄGE",Font=new Font("Segoe UI",9,FontStyle.Bold),Tag="muted",Dock=DockStyle.Fill,TextAlign=ContentAlignment.BottomLeft,Padding=new Padding(0,0,0,7)};AddRow(form,label,row++,38);}
    var labelBar=new SmoothTable{ColumnCount=2,Dock=DockStyle.Fill,Margin=Padding.Empty};labelBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));labelBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,132));
    labelBar.Controls.Add(new Label{Text=Caption(i)+" *",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,Margin=Padding.Empty},0,0);
    if(i!=1&&i!=2&&i!=4&&i!=5&&i!=7){var missing=new SoftButton{Text="Nicht angegeben",Dock=DockStyle.Fill,FlatStyle=FlatStyle.Flat,Font=new Font("Segoe UI",8),Margin=Padding.Empty,TabStop=false,AccessibleName=Caption(i)+": Nicht angegeben"};missing.Click+=(s,e)=>{fields[index].Text=Book.NotSpecified;fields[index].Focus();};labelBar.Controls.Add(missing,1,0);}
    if(i==1){duplicateJump=Btn("Eintrag öffnen",JumpDuplicate,false);duplicateJump.Font=new Font("Segoe UI",8);duplicateJump.Dock=DockStyle.Fill;duplicateJump.Margin=Padding.Empty;duplicateJump.Visible=false;labelBar.Controls.Add(duplicateJump,1,0);}
    AddRow(form,labelBar,row++,27);
    Control input;
    if(i==2){sparten.ResetOptions(standardSparten);sparten.SelectionChanged+=(s,e)=>FieldChanged();input=sparten;}
    else if(i==4||i==5||i==7){var combo=new ClickChoice{ScrollRequested=delta=>scroller.ScrollBy(delta)};combo.Items.AddRange(Choices(i));input=combo;}
    else input=i==1?(TextBox)new PolicyTextBox():new TextBox{Multiline=i==3,ScrollBars=i==3?ScrollBars.Vertical:ScrollBars.None,MaxLength=32000};
    input.Dock=DockStyle.Fill;input.Margin=new Padding(0,4,0,0);input.AccessibleName=Caption(i);fields[i]=input;input.TextChanged+=(sender,e)=>FieldChanged();Control display=input is TextBox?new InputFrame((TextBox)input){Dock=DockStyle.Fill,Margin=new Padding(0,3,0,0)}:input;AddRow(form,display,row++,i==2?84:i==3?66:39);
    if(i==0)input.Leave+=(sender,e)=>NormalizeNameField();
    errors[i]=new Label{Dock=DockStyle.Fill,Font=new Font("Segoe UI",8),Tag="error",Margin=Padding.Empty};AddRow(form,errors[i],row++,8);errorRows[i]=form.RowStyles[row-1];
   }
   commitButton=Btn("Eintrag in Vorschau übernehmen",()=>Commit(),true);commitButton.Dock=DockStyle.Fill;left.Controls.Add(commitButton,0,2);
   var right=Table(1,4);right.Padding=new Padding(18);right.RowStyles.Add(new RowStyle(SizeType.Absolute,67));right.RowStyles.Add(new RowStyle(SizeType.Absolute,51));right.RowStyles.Add(new RowStyle(SizeType.Percent,100));right.RowStyles.Add(new RowStyle(SizeType.Absolute,50));split.Panel2.Controls.Add(right);
   var previewHeading=new Panel{Dock=DockStyle.Fill};previewHeading.Controls.Add(new Label{Text="Vorschau",Dock=DockStyle.Top,Height=35,Font=new Font("Segoe UI",17,FontStyle.Bold)});counts.Dock=DockStyle.Bottom;counts.Height=28;counts.Tag="muted";previewHeading.Controls.Add(counts);right.Controls.Add(previewHeading,0,0);
   var searchBar=new Panel{Dock=DockStyle.Fill,Padding=new Padding(0,0,0,9),Margin=Padding.Empty};search.AccessibleName="Vorschau durchsuchen";search.TextChanged+=(sender,e)=>RefreshRows();incompleteOnly.CheckedChanged+=(sender,e)=>RefreshRows();incompleteOnly.AutoSize=false;incompleteOnly.Width=155;incompleteOnly.Dock=DockStyle.Right;incompleteOnly.Padding=new Padding(10,0,0,0);searchBar.Controls.Add(new InputFrame(search){Dock=DockStyle.Fill});searchBar.Controls.Add(incompleteOnly);searchBar.Controls.Add(new Label{Text="Suchen",Dock=DockStyle.Left,Width=65,TextAlign=ContentAlignment.MiddleLeft,Tag="muted"});right.Controls.Add(searchBar,0,1);
   ConfigureGrid();right.Controls.Add(grid,0,2);
   var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false,AutoScroll=true,Padding=new Padding(0,8,0,0)};actions.Controls.Add(Btn("Auswahl bearbeiten",()=>{if(grid.CurrentRow!=null&&DiscardDraft())Edit(SourceIndex(grid.CurrentRow));},false));actions.Controls.Add(Btn("Entfernen",Remove,false));undoButton=Btn("Rückgängig",UndoRemove,false);undoButton.Enabled=false;actions.Controls.Add(undoButton);right.Controls.Add(actions,0,3);
   status.Dock=DockStyle.Fill;status.TextAlign=ContentAlignment.MiddleLeft;status.Tag="muted";status.Font=new Font("Segoe UI",9);status.Text="Lokale, verschlüsselte Wiederherstellung · Version "+Updater.Version;root.Controls.Add(status,0,3);
   ClearForm();RefreshRows();ApplyTheme();FormClosing+=OnFormClosingRequested;
   if(!testing){auth=new AuthClient(LocalState.ProfileFolder);SetAccess(false);}
   authTimer.Tick+=async(sender,e)=>{if(!authBusy&&ready&&!updating&&Application.OpenForms.Count==1&&!await ValidateAccess())await SwitchAccount();};
   saveTimer.Tick+=(sender,e)=>{if(preferencesPending)SavePreferences();if(recoveryPending)PersistRecovery();};
   split.SplitterMoved+=(sender,e)=>{if(ready){preferences.Split=(double)split.SplitterDistance/Math.Max(1,split.Width);preferencesPending=true;}};
   ResizeEnd+=(sender,e)=>preferencesPending=true;SizeChanged+=(sender,e)=>{if(ready)preferencesPending=true;};
   Shown+=(sender,e)=>{ApplyWindowPreferences();if(!testing)BeginInvoke(new Action(StartSession));else ready=true;};initializing=false;
  }
  TableLayoutPanel Table(int columns,int rows){var t=new SmoothTable{Dock=DockStyle.Fill,ColumnCount=columns,RowCount=rows,Margin=Padding.Empty};for(int i=0;i<columns;i++)t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100f/columns));return t;}
  void AddRow(TableLayoutPanel table,Control control,int row,int height){table.RowCount=row+1;table.RowStyles.Add(new RowStyle(SizeType.Absolute,height));table.Controls.Add(control,0,row);}
  string Caption(int i){return book.Headers[i].Trim()+(i==8?" (%)":i==9?" (€)":"");}
  Button Btn(string text,Action action,bool primary){var b=new SoftButton{Text=text,Font=Font,AutoSize=true,Height=36,Padding=new Padding(10,3,10,3),Margin=new Padding(0,0,8,0),Tag=primary?"primary":"secondary"};b.Click+=(s,e)=>{try{action();}catch(Exception){Tell("Aktion nicht abgeschlossen","Bitte Datei, Zugriffsrechte und Eingaben prüfen und erneut versuchen.");}};return b;}
  public void ShowFailure(){Tell("Aktion nicht abgeschlossen","Bitte Eingaben prüfen und erneut versuchen.");}
  void Tell(string title,string text){MacDialog.Show(this,palette,title,text);}
  bool Confirm(string title,string text,string yes){return MacDialog.Show(this,palette,title,text,new DialogChoice(yes,DialogResult.Yes),new DialogChoice("Abbrechen",DialogResult.Cancel))==DialogResult.Yes;}
  void ToggleTheme(){palette=new Palette(!palette.Dark);ApplyTheme();preferences.Dark=palette.Dark;preferencesPending=true;if(ready&&!testing)SavePreferences();}
  void ApplyTheme(){palette.Apply(this);themeButton.Text=palette.Dark?"Hellmodus":"Dunkelmodus";grid.BackgroundColor=palette.Surface;grid.GridColor=palette.Line;grid.DefaultCellStyle.BackColor=palette.Surface;grid.DefaultCellStyle.ForeColor=palette.Text;grid.DefaultCellStyle.SelectionBackColor=palette.Selected;grid.DefaultCellStyle.SelectionForeColor=palette.Text;grid.AlternatingRowsDefaultCellStyle.BackColor=palette.Input;grid.ColumnHeadersDefaultCellStyle.BackColor=palette.Brand;grid.ColumnHeadersDefaultCellStyle.ForeColor=Color.White;grid.Invalidate();RefreshErrors();}
  void ConfigureGrid(){grid.Dock=DockStyle.Fill;grid.Font=Font;grid.ReadOnly=true;grid.AllowUserToAddRows=false;grid.AllowUserToDeleteRows=false;grid.AllowUserToResizeRows=false;grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect;grid.MultiSelect=false;grid.BorderStyle=BorderStyle.None;grid.RowHeadersVisible=false;grid.EnableHeadersVisualStyles=false;grid.ColumnHeadersHeight=42;grid.RowTemplate.Height=38;grid.CellBorderStyle=DataGridViewCellBorderStyle.SingleHorizontal;grid.DefaultCellStyle.Padding=new Padding(8,0,8,0);grid.DefaultCellStyle.Font=Font;grid.ColumnHeadersDefaultCellStyle.Font=new Font("Segoe UI",10,FontStyle.Bold);
   grid.Columns.Add("state","Prüfung");grid.Columns[0].Width=100;int[] widths={190,145,160,240,145,250,140,150,125,160};for(int i=0;i<10;i++){grid.Columns.Add("c"+i,Caption(i));grid.Columns[i+1].Width=widths[i];if(i>=8)grid.Columns[i+1].DefaultCellStyle.Alignment=DataGridViewContentAlignment.MiddleRight;}foreach(DataGridViewColumn c in grid.Columns)c.SortMode=DataGridViewColumnSortMode.NotSortable;grid.Columns[0].Frozen=true;grid.Columns[1].Frozen=true;
   grid.CellDoubleClick+=(s,e)=>{if(e.RowIndex>=0&&DiscardDraft())Edit(SourceIndex(grid.Rows[e.RowIndex]));};
  }
  string[] Choices(int column){var values=column==4?new[]{"","Mahnstufe 1","Mahnstufe 2","Klage","Exekution",Book.NotSpecified}:column==5?new[]{"","durch VN (Vertragsgeber)","durch VU (Vertragsunternehmen)",Book.NotSpecified}:new[]{"","ja","nein",Book.NotSpecified};return values;}
  Record Read(){var r=editing>=0?book.Rows[editing].Clone():new Record();for(int i=0;i<10;i++)if(i!=2)r.Values[i]=fields[i].Text;var chosen=sparten.Selected;var old=r.Values[2].Split(new char[]{','}).Select(x=>x.Trim()).Where(x=>x!="").ToArray();if(!chosen.OrderBy(x=>x).SequenceEqual(old.OrderBy(x=>x)))r.Values[2]=string.Join(", ",chosen);return r;}
  string Draft(){var r=Read();try{return string.Join("\u001f",r.Values);}finally{r.Clear();}}
  bool DraftChanged(){return Draft()!=draftBaseline;}
  bool DiscardDraft(){return !DraftChanged()||Confirm("Formular verwerfen?","Die aktuellen Änderungen im Formular wurden noch nicht in die Vorschau übernommen.","Verwerfen");}
  void FieldChanged(){if(initializing||closing)return;if(showErrors)RefreshErrors();QueueRecovery();}
  void RefreshErrors(){if(fields.Any(x=>x==null)||errors.Any(x=>x==null))return;var r=Read();try{for(int i=0;i<10;i++){errors[i].Text=showErrors?Book.FieldError(r,i):"";if(i==1){int duplicate=book.DuplicateIndex(r.Values[1],editing);duplicateJump.Visible=duplicate>=0;if(showErrors&&duplicate>=0)errors[i].Text="Bereits in Eintrag "+(duplicate+1)+" vorhanden. Bitte andere Polizze verwenden.";}errors[i].ForeColor=palette.Error;if(errorRows[i]!=null&&errorRows[i].Height!=(errors[i].Text==""?8:32))errorRows[i].Height=errors[i].Text==""?8:32;}}finally{r.Clear();}}
  bool ValidateForm(){showErrors=true;RefreshErrors();for(int i=0;i<10;i++)if(errors[i].Text!=""){scroller.ScrollControlIntoView(fields[i].Parent);scroller.ScrollControlIntoView(fields[i]);fields[i].Focus();status.Text="Bitte die markierten Felder ergänzen oder „Nicht angegeben“ wählen.";return false;}return true;}
  void ClearForm(){showErrors=false;editing=-1;foreach(var c in fields)if(c!=null&&c!=sparten){c.Text="";var combo=c as ClickChoice;if(combo!=null)combo.Text="";var text=c as TextBox;if(text!=null)text.ClearUndo();}sparten.SetSelected(new string[0]);draftBaseline=Draft();formTitle.Text="Neuer Eintrag";if(commitButton!=null)commitButton.Text="Eintrag in Vorschau übernehmen";RefreshErrors();QueueRecovery();}
  void ClearChoices(){sparten.ResetOptions(standardSparten);foreach(int i in new[]{4,5,7}){var c=(ClickChoice)fields[i];c.Items.Clear();c.Items.AddRange(Choices(i));c.Text="";}}
  bool Commit(){NormalizeNameField();if(!ValidateForm())return false;if(book.Rows.Count>=996&&editing<0){Tell("Vorschau voll","Maximal 996 Einträge möglich.");return false;}var r=Read();if(editing>=0){book.Rows[editing].Clear();book.Rows[editing]=r;}else book.Rows.Add(r);dirty=true;ClearForm();RefreshRows();QueueRecovery();status.Text="Eintrag übernommen · Noch nicht als Excel exportiert.";return true;}
  bool Complete(Record r){return book.RecordError(r,book.Rows.IndexOf(r))=="";}
  int SourceIndex(DataGridViewRow row){return book.Rows.IndexOf((Record)row.Tag);}
  void RefreshRows(){grid.Rows.Clear();int incomplete=0;foreach(var r in book.Rows){bool complete=Complete(r);if(!complete)incomplete++;if(incompleteOnly.Checked&&complete)continue;if(search.Text.Length>0&&!r.Values.Any(x=>(x??"").IndexOf(search.Text,StringComparison.CurrentCultureIgnoreCase)>=0))continue;var values=new object[11];values[0]=complete?"Bereit":book.DuplicateIndex(r.Values[1],book.Rows.IndexOf(r))>=0?"Doppelt":"Ergänzen";for(int i=0;i<10;i++){values[i+1]=r.Values[i];if(i>=8&&!string.IsNullOrWhiteSpace(r.Values[i])&&!Book.IsNotSpecified(r.Values[i]))try{values[i+1]=Book.Number(r.Values[i]).ToString(i==9?"N2":"0.####################",System.Globalization.CultureInfo.GetCultureInfo("de-AT"))+(i==8?" %":" €");}catch{}}
   int k=grid.Rows.Add(values);grid.Rows[k].Tag=r;grid.Rows[k].Cells[0].ToolTipText=complete?"Alle Felder geprüft":"Bitte fehlende oder ungültige Angaben ergänzen.";}
   counts.Text=grid.Rows.Count+" von "+book.Rows.Count+" Einträgen · "+(incomplete==0?"Alle vollständig":incomplete+" zu ergänzen");}
  void Edit(int index){if(index<0||index>=book.Rows.Count)return;ClearForm();editing=index;var r=book.Rows[index];for(int i=0;i<10;i++)if(i!=2){var combo=fields[i] as ClickChoice;if(combo!=null&&!combo.Items.Contains(r.Values[i]))combo.Items.Add(r.Values[i]);fields[i].Text=r.Values[i];}sparten.SetSelected(r.Values[2].Split(new char[]{','}).Select(x=>x.Trim()));draftBaseline=Draft();formTitle.Text="Eintrag "+(index+1)+" bearbeiten";commitButton.Text="Änderungen übernehmen";showErrors=!Complete(r);RefreshErrors();scroller.AutoScrollPosition=Point.Empty;}
  void JumpDuplicate(){var r=Read();int index=book.DuplicateIndex(r.Values[1],editing);r.Clear();if(index<0||!DiscardDraft())return;search.Clear();incompleteOnly.Checked=false;Edit(index);RefreshRows();foreach(DataGridViewRow row in grid.Rows)if(SourceIndex(row)==index){grid.CurrentCell=row.Cells[1];break;}}
  void NormalizeNameField(){if(fields[0]!=null)fields[0].Text=Book.NormalizeName(fields[0].Text);}
  void Remove(){if(grid.CurrentRow==null)return;if(!DiscardDraft()||!Confirm("Eintrag entfernen?","Der ausgewählte Eintrag wird aus dieser Sitzung entfernt. Mit „Rückgängig“ lässt er sich in dieser Sitzung wiederherstellen.","Entfernen"))return;RemoveAt(SourceIndex(grid.CurrentRow));}
  void RemoveAt(int index){ClearForm();removed.Push(new RemovedEntry{Record=book.Rows[index],Index=index});book.Rows.RemoveAt(index);dirty=true;undoButton.Enabled=true;RefreshRows();QueueRecovery();}
  void UndoRemove(){if(removed.Count==0)return;if(book.Rows.Count>=996){Tell("Vorschau voll","Zum Wiederherstellen muss zuerst ein Platz frei werden.");return;}var item=removed.Peek();if(book.DuplicateIndex(item.Record.Values[1],-1)>=0){Tell("Polizzennummer vorhanden","Die entfernte Polizzennummer wird inzwischen wieder verwendet. Bitte den vorhandenen Eintrag zuerst bearbeiten.");return;}if(!DiscardDraft())return;ClearForm();removed.Pop();book.Rows.Insert(Math.Min(item.Index,book.Rows.Count),item.Record);dirty=true;undoButton.Enabled=removed.Count>0;RefreshRows();QueueRecovery();}
  void ClearUndoHistory(){foreach(var item in removed)item.Record.Clear();removed.Clear();if(undoButton!=null)undoButton.Enabled=false;}
  void NewSession(Book next){editing=-1;showErrors=false;ClearUndoHistory();book.Dispose();book=next;ClearChoices();ClearForm();dirty=book.Rows.Any(r=>r.OriginalValues!=null&&r.Values[0]!=r.OriginalValues[0]);search.Clear();search.ClearUndo();incompleteOnly.Checked=false;RefreshRows();QueueRecovery();}
  void Reset(){if(!Confirm("Sitzung zurücksetzen?","Alle Einträge, Formulareingaben und die lokale Wiederherstellungskopie werden entfernt. Bereits gespeicherte Excel-Dateien bleiben bestehen.","Zurücksetzen"))return;if(!DeleteRecovery())return;NewSession(Book.Load(Template));status.Text="Neue, leere Sitzung · Wiederherstellungskopie entfernt.";}
  void OpenBook(){if((dirty||DraftChanged())&&!Confirm("Excel-Datei öffnen?","Die aktuelle Sitzung wird verworfen. Nicht exportierte Eingaben gehen dabei verloren.","Datei auswählen"))return;using(var d=new OpenFileDialog{Filter="MAC Excel mit Makros (*.xlsm)|*.xlsm",Title="Vorhandene MAC-Datei öffnen",RestoreDirectory=true})if(d.ShowDialog(this)==DialogResult.OK)try{Book next=Book.Load(d.FileName);if(!DeleteRecovery()){next.Dispose();return;}NewSession(next);status.Text="Datei geöffnet · Namensanfänge vereinheitlicht; ungültige Angaben vor Export korrigieren.";}catch(Exception e){Tell("Import nicht möglich",e.Message);}}
  async Task<bool> Save(){
   if(DraftChanged()&&!Commit())return false;
   int invalid=book.Rows.FindIndex(r=>!Complete(r));if(invalid>=0){Edit(invalid);ValidateForm();Tell("Angaben ergänzen","Eintrag "+(invalid+1)+" ist noch unvollständig oder enthält einen ungültigen Wert. Bitte die markierten Felder ausfüllen. Die Polizzennummer muss eindeutig sein und aus 8–12 Ziffern bestehen. Für andere Felder ist „Nicht angegeben“ erlaubt.");return false;}
   try{byte[] data=book.Export(testing?null:auth.Session.user.Name);try{using(var d=new SaveFileDialog{Filter="Excel mit Makros (*.xlsm)|*.xlsm",DefaultExt="xlsm",AddExtension=true,FileName=testing?"MAC_Kundenklaerung.xlsm":auth.SuggestedFile,OverwritePrompt=true,Title="Dateiname und Speicherort wählen",RestoreDirectory=true}){if(d.ShowDialog(this)!=DialogResult.OK)return false;if(!testing)await auth.Validate();string target=Path.GetFullPath(d.FileName);if(!Path.GetExtension(target).Equals(".xlsm",StringComparison.OrdinalIgnoreCase))throw new Exception("Bitte die Dateiendung .xlsm verwenden, damit die Makros erhalten bleiben.");string install=Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);if(target.StartsWith(install,StringComparison.OrdinalIgnoreCase))throw new Exception("Kundendateien bitte außerhalb des Programmordners speichern.");using(var f=new FileStream(target,FileMode.Create,FileAccess.Write,FileShare.None)){f.Write(data,0,data.Length);f.Flush(true);}dirty=false;recoveryPending=false;bool cleared=DeleteRecovery();status.Text=cleared?"Excel-Datei gespeichert · Lokaler Entwurf entfernt.":"Excel-Datei gespeichert · Lokale Wiederherstellungskopie konnte nicht entfernt werden.";return true;}}finally{Array.Clear(data,0,data.Length);}}catch(AuthFailure e){SetAccess(false);Tell("Anmeldung erforderlich",e.Message+"\nDie Excel-Datei wurde nicht geschrieben; der Entwurf bleibt erhalten.");return false;}catch(Exception e){Tell("Export nicht möglich",e.Message);return false;}
  }
  void ApplyWindowPreferences(){if(preferences.Maximized)WindowState=FormWindowState.Maximized;int distance=(int)(split.Width*Math.Max(.25,Math.Min(.7,preferences.Split)));split.SplitterDistance=Math.Max(split.Panel1MinSize,Math.Min(split.Width-split.Panel2MinSize-split.SplitterWidth,distance));}
  void SavePreferences(){if(testing||local==null)return;try{preferences.Dark=palette.Dark;preferences.Maximized=WindowState==FormWindowState.Maximized;Size bounds=WindowState==FormWindowState.Normal?Size:RestoreBounds.Size;if(bounds.Width>0){preferences.Width=bounds.Width;preferences.Height=bounds.Height;}preferences.Split=(double)split.SplitterDistance/Math.Max(1,split.Width);local.WritePreferences(preferences);preferencesPending=false;}catch{status.Text="Einstellungen konnten nicht gespeichert werden. Bitte lokale Schreibrechte prüfen.";}}
  void QueueRecovery(){if(!initializing&&!closing)recoveryPending=true;}
  SessionState Snapshot(){var r=Read();try{return new SessionState{Book=book,Draft=(string[])r.Values.Clone(),Baseline=draftBaseline,Editing=editing,Dirty=dirty};}finally{r.Clear();}}
  bool PersistRecovery(){if(testing||!ready||closing)return true;try{if(dirty||DraftChanged()){var state=Snapshot();try{local.Save(state);}finally{state.Book=null;state.Dispose();}status.Text="Entwurf lokal verschlüsselt gesichert · Excel-Export noch ausständig.";}else local.DeleteRecovery();recoveryPending=false;return true;}catch{status.Text="Entwurf konnte NICHT gesichert werden. Bitte als Excel exportieren oder erneut versuchen.";return false;}}
  bool DeleteRecovery(){if(testing||local==null)return true;try{local.DeleteRecovery();recoveryPending=false;return true;}catch{Tell("Lokaler Entwurf bleibt bestehen","Die Wiederherstellungskopie konnte nicht entfernt werden. Bitte Schreibrechte prüfen und erneut versuchen.");return false;}}
  void ApplyRecovered(SessionState state){Book restored=state.Book;state.Book=null;NewSession(restored);if(state.Editing>=0)Edit(state.Editing);for(int i=0;i<10;i++)if(i!=2){var choice=fields[i] as ClickChoice;if(choice!=null&&!choice.Items.Contains(state.Draft[i]))choice.Items.Add(state.Draft[i]);fields[i].Text=state.Draft[i];}sparten.SetSelected(state.Draft[2].Split(new char[]{','}).Select(x=>x.Trim()));draftBaseline=state.Baseline;dirty=state.Dirty;RefreshRows();showErrors=false;RefreshErrors();}
  async void StartSession(){
   try{authBusy=true;if(!await Authenticate(true)){authBusy=false;Close();return;}authBusy=false;RestoreSession();}catch(Exception e){Tell("Anmeldung nicht möglich",e.Message);authBusy=false;Close();}finally{authBusy=false;}
  }
  void RestoreSession(){
   if(auth.Session.role=="admin"&&!local.HasRecovery){using(var legacy=new LocalState(LocalState.ProfileFolder))if(legacy.HasRecovery){
    if(Confirm("Alten Entwurf zuordnen?","Auf diesem PC liegt noch ein Entwurf aus Version 1.2 ohne Beraterzuordnung. Als Admin kannst du ihn ausdrücklich dem Berater "+auth.Session.user.Name+" zuordnen. Bei Abbruch bleibt er unverändert erhalten.","Diesem Berater zuordnen")){
     try{using(var previous=legacy.Restore())local.Save(previous);legacy.DeleteRecovery();}catch{Tell("Alter Entwurf nicht vollständig übernommen","Der alte oder bereits kopierte Entwurf bleibt erhalten. Bitte vor dem Zurücksetzen prüfen.");}
    }
   }}
   if(local.HasRecovery){var answer=MacDialog.Show(this,palette,"Letzte Sitzung wiederherstellen?","Es gibt einen lokal verschlüsselten Entwurf. Auch noch unvollständige Formulareingaben können wiederhergestellt werden.",new DialogChoice("Wiederherstellen",DialogResult.Yes),new DialogChoice("Verwerfen",DialogResult.No));
    if(answer==DialogResult.Yes){try{using(var state=local.Restore())ApplyRecovered(state);status.Text="Letzte Sitzung wiederhergestellt · Bitte vor dem Excel-Export prüfen.";}catch{Tell("Wiederherstellung nicht möglich","Die Datei ist beschädigt oder kann mit diesem Windows-Benutzer nicht entschlüsselt werden. Sie wurde nicht überschrieben. Die App wird geschlossen; beim nächsten Start kannst du den Entwurf erneut versuchen oder ausdrücklich verwerfen.");Close();return;}}
    else if(answer==DialogResult.No){if(!DeleteRecovery()){Close();return;}}
    else{Close();return;}
   }
   ready=true;recoveryPending=false;saveTimer.Start();authTimer.Start();SetAccess(true);
  }
  void OnFormClosingRequested(object sender,FormClosingEventArgs e){
   if(authBusy){e.Cancel=true;return;}
   if(updating){Tell("Bitte kurz warten","Die Updateprüfung oder der Download läuft noch.");e.Cancel=true;return;}
   if(ready&&!PersistRecovery()){Tell("Schließen abgebrochen","Der lokale Entwurf konnte nicht gesichert werden. Bitte die Daten als Excel exportieren, Schreibrechte prüfen oder die Sitzung ausdrücklich zurücksetzen.");e.Cancel=true;return;}
   SavePreferences();closing=true;saveTimer.Stop();authTimer.Stop();ClearForm();ClearChoices();ClearUndoHistory();grid.Rows.Clear();search.Clear();search.ClearUndo();book.Dispose();draftBaseline="";if(local!=null)local.Dispose();
  }
  async Task CheckUpdates(){await CheckUpdates(Updater.Check);}
  async Task CheckUpdates(Func<Task<Release>> check){if(updating)return;updating=true;toolbar.Enabled=false;editor.Enabled=false;try{
   status.Text="GitHub Releases werden geprüft …";var release=await check();if(release==null){Tell("App ist aktuell","Keine neuere veröffentlichte Version verfügbar. Deine Eingaben bleiben erhalten.");return;}
   if(!Confirm("Update verfügbar","Version "+release.Version+" ist verfügbar. Das Herunterladen und Installieren startet die App neu.","Update vorbereiten"))return;
   if(dirty||DraftChanged()){if(!Confirm("Eingaben zuerst speichern","Vor der Installation müssen die Eingaben als Excel exportiert werden. Bei Abbruch bleiben App und Eingaben geöffnet.","Jetzt exportieren")||!await SaveAuthorized())return;}
   status.Text="Update wird heruntergeladen und geprüft …";string setup=await Updater.Download(release);Process.Start(new ProcessStartInfo(setup,"/target \""+AppDomain.CurrentDomain.BaseDirectory.TrimEnd(new char[]{'\\'})+"\""){UseShellExecute=true});updating=false;Close();
  }catch(Exception e){Tell("Update nicht verfügbar",e.Message);}finally{updating=false;if(!IsDisposed){SetAccess(editor.Visible);status.Text="Kundendaten lokal · Anmeldung zentral · Updates über GitHub.";}}}
  void SetAccess(bool allowed){toolbar.Visible=true;toolbar.Enabled=true;editor.Visible=allowed;editor.Enabled=allowed&&!updating;foreach(Control c in toolbar.Controls)c.Enabled=allowed&&!updating;if(accountButton!=null){accountButton.Enabled=!updating;accountButton.Text=allowed?"Konto / Abmelden":"Erneut anmelden";}if(!allowed)identity.Text="Gesperrt · Online-Anmeldung erforderlich";else if(!testing&&auth.Session!=null&&auth.Session.user!=null)identity.Text=(auth.Session.role=="admin"?"Adminmodus · Berater: ":"Angemeldet als: ")+auth.Session.user.Name;}
  async Task<bool> Authenticate(bool resume){
   SetAccess(false);
   bool accepted=resume&&await auth.Resume();
   while(true){
    if(!accepted&&!AuthUi.Login(this,palette,auth))return false;
    if(auth.Session.user==null){using(var admin=new AdminWindow(auth,palette)){if(admin.ShowDialog(this)!=DialogResult.OK){await auth.Logout();accepted=false;continue;}}}
    local.UseProfile(auth.ProfileKey);return true;
   }
  }
  async Task<bool> ValidateAccess(){
   if(testing)return true;if(authBusy||closing||!ready)return false;authBusy=true;
   try{await auth.Validate();SetAccess(true);return true;}catch(Exception e){
    SetAccess(false);authTimer.Stop();PersistRecovery();Tell("Anmeldung prüfen",e.Message+"\nDein Entwurf bleibt lokal erhalten. Bitte erneut anmelden.");return false;
   }finally{authBusy=false;}
  }
  async Task<bool> SaveAuthorized(){
   if(testing)return await Save();if(!await ValidateAccess()){await SwitchAccount();return false;}
   authBusy=true;authTimer.Stop();toolbar.Enabled=false;editor.Enabled=false;try{return await Save();}finally{authBusy=false;if(ready)authTimer.Start();SetAccess(editor.Visible);}
  }
  async Task SwitchAccount(){
   if(authBusy||updating||closing)return;if(!PersistRecovery())return;authBusy=true;authTimer.Stop();saveTimer.Stop();ready=false;SetAccess(false);
   try{await auth.Logout();NewSession(Book.Load(Template));if(!await Authenticate(false)){authBusy=false;Close();return;}authBusy=false;RestoreSession();}catch(Exception e){Tell("Anmeldung nicht möglich",e.Message);authBusy=false;Close();}finally{authBusy=false;}
  }
  async Task AccountMenu(){
   if(testing||authBusy||updating)return;if(!editor.Visible){await SwitchAccount();return;}
   var choice=MacDialog.Show(this,palette,"Beraterkonto",identity.Text+"\nBeim Wechsel bleibt der Entwurf diesem Berater zugeordnet. Er wird nicht an den Server gesendet.",new DialogChoice("Wechseln / Abmelden",DialogResult.Yes),new DialogChoice(auth.Session.role=="admin"?"Administration":"Passwort ändern",DialogResult.No),new DialogChoice("Zurück",DialogResult.Cancel));
   if(choice==DialogResult.Yes){await SwitchAccount();return;}if(choice!=DialogResult.No)return;
   if(!await ValidateAccess()){await SwitchAccount();return;}
   if(auth.Session.role=="admin"){
    if(!PersistRecovery())return;ready=false;saveTimer.Stop();authTimer.Stop();SetAccess(false);using(var admin=new AdminWindow(auth,palette)){
     if(admin.ShowDialog(this)==DialogResult.OK){NewSession(Book.Load(Template));local.UseProfile(auth.ProfileKey);RestoreSession();}
     else {ready=true;saveTimer.Start();authTimer.Start();if(!await ValidateAccess())await SwitchAccount();}
    }
   }else{
    string old,next;if(!AuthUi.Password(this,palette,null,out old,out next,true))return;try{await auth.Call("POST","/v1/password",new{old_password=old,new_password=next});Tell("Passwort geändert","Alle bisherigen Anmeldungen wurden beendet. Bitte mit dem neuen Passwort anmelden.");await SwitchAccount();}catch(Exception e){Tell("Passwort nicht geändert",e.Message);}finally{old=next=null;}
   }
  }
  public void Smoke(){
   if(ValidateForm())throw new Exception("Empty form accepted");fields[0].Text="mustermann max";fields[1].Text="0000123456";sparten.SetSelected(new[]{"RS","HHV","PHV"});fields[3].Text="Rückmeldung angefordert";fields[4].Text="Mahnstufe 1";fields[5].Text=Book.NotSpecified;fields[6].Text=Book.NotSpecified;fields[7].Text="ja";fields[8].Text="4,29";fields[9].Text="1234,56";
   var policy=(PolicyTextBox)fields[1];policy.SelectAll();if(policy.TryInsertText("1234ABCD")||policy.TryInsertText("1234567890123")||policy.Text!="0000123456")throw new Exception("Invalid paste modified policy");
   foreach(int i in new[]{4,5,7}){var choice=(ClickChoice)fields[i];string before=choice.Text;choice.TestWheel(-120);if(choice.Text!=before)throw new Exception("Mouse wheel changed choice");}
   string draft=Draft();ToggleTheme();if(Draft()!=draft)throw new Exception("Theme lost draft");ToggleTheme();if(!Commit()||book.Rows.Count!=1||book.Rows[0].Values[0]!="Mustermann Max")throw new Exception("Commit/name");
   Edit(0);fields[1].Text="12345678";if(!Commit())throw new Exception("Edited policy");
   Edit(0);editing=-1;draftBaseline="";if(ValidateForm()||!duplicateJump.Visible)throw new Exception("Duplicate not blocked");ClearForm();
   var second=book.Rows[0].Clone();second.Values[1]="87654321";book.Rows.Add(second);RefreshRows();search.Text="87654321";if(grid.Rows.Count!=1||SourceIndex(grid.Rows[0])!=1)throw new Exception("Filtered index");Edit(SourceIndex(grid.Rows[0]));if(fields[1].Text!="87654321")throw new Exception("Filtered edit");search.Clear();ClearForm();
   RemoveAt(1);if(book.Rows.Count!=1)throw new Exception("Remove");UndoRemove();if(book.Rows.Count!=2||book.Rows[1].Values[1]!="87654321")throw new Exception("Undo");
   book.Rows[1].Values[1]="123";incompleteOnly.Checked=true;if(grid.Rows.Count!=1)throw new Exception("Incomplete filter");book.Rows[1].Values[1]="87654321";incompleteOnly.Checked=false;RefreshRows();Edit(0);
   sparten.TestPicker();var bytes=book.Export();Array.Clear(bytes,0,bytes.Length);
   fields[3].Text="Unfertiger Entwurf";fields[9].Text="";var snapshot=Snapshot();byte[] encoded=SessionCodec.Encode(snapshot);snapshot.Book=null;snapshot.Dispose();using(var restored=SessionCodec.Decode(encoded))ApplyRecovered(restored);Array.Clear(encoded,0,encoded.Length);if(fields[3].Text!="Unfertiger Entwurf"||fields[9].Text!=""||book.Rows.Count!=2||!DraftChanged())throw new Exception("Draft restoration");fields[9].Text="1234,56";
   scroller.AutoScrollPosition=Point.Empty;Refresh();Application.DoEvents();if(search.Parent.Bottom>search.Parent.Parent.ClientSize.Height)throw new Exception("Search clipped");CaptureTestImage("mac120-light.png");ToggleTheme();Refresh();Application.DoEvents();CaptureTestImage("mac120-dark.png");
   using(var timer=new Timer{Interval=400}){timer.Tick+=(sender,e)=>{var popup=Application.OpenForms.OfType<SpartenDialog>().FirstOrDefault();if(popup!=null){timer.Stop();popup.Refresh();Application.DoEvents();using(var bmp=new Bitmap(popup.ClientSize.Width,popup.ClientSize.Height)){using(var g=Graphics.FromImage(bmp))g.CopyFromScreen(popup.PointToScreen(Point.Empty),Point.Empty,popup.ClientSize);bmp.Save(Path.Combine(Path.GetTempPath(),"mac120-sparten.png"));}popup.DialogResult=DialogResult.Cancel;}};timer.Start();sparten.OpenPicker();}
   var selectedBefore=sparten.Selected;using(var timer=new Timer{Interval=150}){timer.Tick+=(sender,e)=>{var popup=Application.OpenForms.OfType<SpartenDialog>().FirstOrDefault();if(popup!=null){timer.Stop();popup.TestSelection();popup.DialogResult=DialogResult.OK;}};timer.Start();sparten.OpenPicker();}if(sparten.Selected.Length!=1||sparten.Selected[0]!="EH")throw new Exception("Confirmed sparten not applied");sparten.SetSelected(selectedBefore);
   bool checkedWhileDirty=false;string beforeCheck=Draft();using(var timer=new Timer{Interval=150}){timer.Tick+=(sender,e)=>{var popup=Application.OpenForms.Cast<Form>().FirstOrDefault(f=>f!=this);if(popup!=null){timer.Stop();popup.DialogResult=DialogResult.OK;}};timer.Start();CheckUpdates(()=>{checkedWhileDirty=true;return Task.FromResult<Release>(null);}).GetAwaiter().GetResult();}if(!checkedWhileDirty||Draft()!=beforeCheck)throw new Exception("Update check lost draft");
   scroller.ScrollControlIntoView(fields[9].Parent);Application.DoEvents();if(fields[9].RectangleToScreen(fields[9].ClientRectangle).Bottom>scroller.RectangleToScreen(scroller.ClientRectangle).Bottom)throw new Exception("Last field inaccessible");
   NewSession(Book.Load(Template));if(book.Rows.Count!=0||grid.Rows.Count!=0||DraftChanged()||dirty||fields[0].Text!=""||removed.Count!=0)throw new Exception("Reset");
   Console.WriteLine("PASS: new UI, strict paste, wheel protection, names, duplicates, filtered editing, undo, incomplete filter, recovery, popup cancel, themes and reset.");
  }
  void CaptureTestImage(string file){using(var bitmap=new Bitmap(ClientSize.Width,ClientSize.Height)){using(var graphics=Graphics.FromImage(bitmap))graphics.CopyFromScreen(PointToScreen(Point.Empty),Point.Empty,ClientSize);bitmap.Save(Path.Combine(Path.GetTempPath(),file));}}
 }
}
