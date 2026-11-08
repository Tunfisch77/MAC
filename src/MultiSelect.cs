using System;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
namespace MacApp {
 public sealed class MultiSelect:UserControl,IThemedControl {
  readonly Dictionary<string,string> options=new Dictionary<string,string>();readonly List<string> selected=new List<string>();
  readonly FlowLayoutPanel chips=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true};
  readonly SoftButton open=new SoftButton{Text="Sparten auswählen …",Dock=DockStyle.Top,Height=36,TextAlign=ContentAlignment.MiddleLeft};
  Palette palette=new Palette(false);public event EventHandler SelectionChanged;public string[] Selected {get{return selected.ToArray();}}
  public MultiSelect(){DoubleBuffered=true;Controls.Add(chips);Controls.Add(open);open.Click+=(s,e)=>OpenPicker();}
  public void ResetOptions(string[] captions){options.Clear();foreach(var c in captions)options.Add(c.Split(new char[]{' '})[0],c);options.Add(Book.NotSpecified,Book.NotSpecified);SetSelected(new string[0]);}
  public void SetSelected(IEnumerable<string> codes){selected.Clear();foreach(var c in codes.Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct()){if(!options.ContainsKey(c))options.Add(c,c);selected.Add(c);}Render();}
  public void SetPalette(Palette p){palette=p;Render();}
  void Render(){SuspendLayout();foreach(Control c in chips.Controls.Cast<Control>().ToArray()){chips.Controls.Remove(c);c.Dispose();}open.Text=selected.Count==0?"Sparten auswählen …":"Sparten ändern …";
   foreach(string code in selected){string local=code;var b=new SoftButton{Text=code+"  ×",AutoSize=true,Height=29,Margin=new Padding(0,3,5,0),Padding=new Padding(3,0,3,0),AccessibleName=options[code]+" entfernen"};b.Click+=(s,e)=>{selected.Remove(local);Render();Changed();};chips.Controls.Add(b);palette.Apply(b);}
   if(selected.Count==0)chips.Controls.Add(new Label{Text="Mehrere Sparten möglich",AutoSize=true,ForeColor=palette.Muted,Margin=new Padding(4,5,0,0)});palette.Apply(open);chips.BackColor=palette.Surface;ResumeLayout();}
  void Changed(){if(SelectionChanged!=null)SelectionChanged(this,EventArgs.Empty);}
  public void OpenPicker(){using(var dialog=new SpartenDialog(options,selected,palette)){if(dialog.ShowDialog(FindForm())==DialogResult.OK){SetSelected(dialog.Selected);Changed();}}}
  public void TestPicker(){
   string[] original=Selected;using(var dialog=new SpartenDialog(options,selected,palette)){dialog.TestSelection();}if(!Selected.SequenceEqual(original))throw new Exception("Cancel changed selected sparten");
  }
 }
 public sealed class SpartenDialog:Form {
  readonly Dictionary<string,string> options;readonly HashSet<string> draft;readonly CheckedListBox list=new CheckedListBox();readonly TextBox search=new TextBox();readonly Label summary=new Label();bool filling;
  public string[] Selected {get{return options.Keys.Where(draft.Contains).ToArray();}}
  public SpartenDialog(Dictionary<string,string> values,IEnumerable<string> selected,Palette palette){
   options=new Dictionary<string,string>(values);draft=new HashSet<string>(selected);Text="MAC · Sparten auswählen";Font=new Font("Segoe UI",10);ClientSize=new Size(550,535);MinimumSize=new Size(480,430);StartPosition=FormStartPosition.CenterParent;MaximizeBox=false;MinimizeBox=false;ShowInTaskbar=false;Padding=new Padding(22);
   var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=6};layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));foreach(int h in new[]{40,30,43})layout.RowStyles.Add(new RowStyle(SizeType.Absolute,h));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,30));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,43));Controls.Add(layout);
   layout.Controls.Add(new Label{Text="Sparten auswählen",Font=new Font("Segoe UI",19,FontStyle.Bold),Dock=DockStyle.Fill},0,0);layout.Controls.Add(new Label{Text="Mehrfachauswahl · Erst „Übernehmen“ bestätigt die Auswahl.",Dock=DockStyle.Fill,Tag="muted"},0,1);
   search.AccessibleName="Sparten durchsuchen";layout.Controls.Add(new InputFrame(search){Dock=DockStyle.Fill,Margin=new Padding(0,0,0,5)},0,2);list.Dock=DockStyle.Fill;list.CheckOnClick=true;list.BorderStyle=BorderStyle.None;list.IntegralHeight=false;list.HorizontalScrollbar=true;list.FormattingEnabled=true;list.Format+=(s,e)=>e.Value=options[e.ListItem.ToString()];layout.Controls.Add(list,0,3);summary.Dock=DockStyle.Fill;summary.Tag="muted";layout.Controls.Add(summary,0,4);
   var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft};var accept=new SoftButton{Text="Übernehmen",DialogResult=DialogResult.OK,Tag="primary",Width=135,Height=36};var cancel=new SoftButton{Text="Abbrechen",DialogResult=DialogResult.Cancel,Width=125,Height=36};actions.Controls.Add(accept);actions.Controls.Add(cancel);layout.Controls.Add(actions,0,5);AcceptButton=accept;CancelButton=cancel;
   list.ItemCheck+=(s,e)=>{if(filling)return;string code=list.Items[e.Index].ToString();if(e.NewValue==CheckState.Checked){if(code==Book.NotSpecified)draft.Clear();else draft.Remove(Book.NotSpecified);draft.Add(code);}else draft.Remove(code);if(IsHandleCreated)BeginInvoke(new Action(()=>{if(!IsDisposed)Fill();}));};search.TextChanged+=(s,e)=>Fill();Fill();palette.Apply(this);
  }
  void Fill(){filling=true;int top=list.TopIndex;string active=list.SelectedItem as string;list.Items.Clear();foreach(var p in options.Where(x=>x.Value.IndexOf(search.Text,StringComparison.CurrentCultureIgnoreCase)>=0))list.Items.Add(p.Key,draft.Contains(p.Key));if(active!=null&&list.Items.Contains(active))list.SelectedItem=active;if(list.Items.Count>0)list.TopIndex=Math.Min(top,list.Items.Count-1);summary.Text=draft.Count+" ausgewählt";filling=false;}
  public void TestSelection(){var before=draft.ToArray();search.Text="Cyber";if(list.Items.Count!=1)throw new Exception("Search");list.SetItemChecked(0,true);if(!draft.Contains("CY")||!before.All(draft.Contains))throw new Exception("Search retains selection");search.Text=Book.NotSpecified;list.SetItemChecked(0,true);if(draft.Count!=1||!draft.Contains(Book.NotSpecified))throw new Exception("Missing exclusive");search.Text="Eigenheim";list.SetItemChecked(0,true);if(draft.Count!=1||!draft.Contains("EH"))throw new Exception("Replace missing");}
 }
}
