using System;
using System.Drawing;
using System.Windows.Forms;

namespace MacApp {
 public interface IThemedControl { void SetPalette(Palette palette); }
 public sealed class Palette {
  public bool Dark;
  public Color Background,Surface,Input,Text,Muted,Line,Selected;
  public readonly Color Brand=Color.FromArgb(43,81,78),Accent=Color.FromArgb(220,221,56);
  public Color Error { get { return Dark?Color.FromArgb(255,160,150):Color.FromArgb(172,45,38); } }
  public Palette(bool dark){Dark=dark;Background=dark?Color.FromArgb(19,29,29):Color.FromArgb(239,243,241);Surface=dark?Color.FromArgb(29,43,42):Color.White;Input=dark?Color.FromArgb(38,54,52):Color.FromArgb(247,249,248);Text=dark?Color.FromArgb(236,242,239):Color.FromArgb(29,53,49);Muted=dark?Color.FromArgb(168,188,180):Color.FromArgb(92,112,103);Line=dark?Color.FromArgb(65,85,78):Color.FromArgb(211,223,216);Selected=dark?Color.FromArgb(61,85,72):Color.FromArgb(228,237,212);}
  public void Apply(Control root){
   string role=root.Tag as string??"";
   root.BackColor=role=="brand"?Brand:role=="background"?Background:Surface;
   root.ForeColor=role=="brand"?Color.White:role=="muted"?Muted:role=="error"?Error:Text;
   var b=root as Button;
   if(b!=null){b.UseVisualStyleBackColor=false;b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderSize=role=="primary"?0:1;b.FlatAppearance.BorderColor=Line;b.BackColor=role=="primary"?Accent:Input;b.ForeColor=role=="primary"?Brand:Text;b.FlatAppearance.MouseOverBackColor=role=="primary"?Color.FromArgb(231,232,90):Selected;}
   if(root is TextBoxBase||root is ComboBox||root is ListBox){root.BackColor=Input;root.ForeColor=Text;}
   foreach(Control child in root.Controls)Apply(child);
   var multi=root as IThemedControl;if(multi!=null)multi.SetPalette(this);
  }
 }
 public sealed class DialogChoice {
  public string Text; public DialogResult Result;
  public DialogChoice(string text,DialogResult result){Text=text;Result=result;}
 }
 public static class MacDialog {
  public static DialogResult Show(IWin32Window owner,Palette palette,string title,string text,params DialogChoice[] choices){
   using(var f=new Form{Text="MAC · "+title,StartPosition=owner==null?FormStartPosition.CenterScreen:FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,ShowInTaskbar=false,ClientSize=new Size(550,275),Font=new Font("Segoe UI",10),Padding=new Padding(24)}){
    var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4};layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,23));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,44));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,44));f.Controls.Add(layout);
    layout.Controls.Add(new Label{Text="MAC VERSICHERUNGSAGENTUR",Tag="muted",Dock=DockStyle.Fill,Font=new Font("Segoe UI",9,FontStyle.Bold)},0,0);
    layout.Controls.Add(new Label{Text=title,Dock=DockStyle.Fill,Font=new Font("Segoe UI",18,FontStyle.Bold)},0,1);
    layout.Controls.Add(new TextBox{Text=text,ReadOnly=true,Multiline=true,BorderStyle=BorderStyle.None,Dock=DockStyle.Fill,ScrollBars=ScrollBars.Vertical,TabStop=false},0,2);
    var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft,WrapContents=false};layout.Controls.Add(actions,0,3);
    if(choices.Length==0)choices=new[]{new DialogChoice("Verstanden",DialogResult.OK)};
    for(int i=choices.Length-1;i>=0;i--){var choice=choices[i];var b=new SoftButton{Text=choice.Text,DialogResult=choice.Result,AutoSize=true,Height=36,Padding=new Padding(10,3,10,3),Tag=i==0?"primary":"secondary",Margin=new Padding(8,4,0,0)};actions.Controls.Add(b);if(i==0)f.AcceptButton=b;if(choice.Result==DialogResult.Cancel||choices.Length==1)f.CancelButton=b;}
    if(f.CancelButton==null){f.KeyPreview=true;f.KeyDown+=(sender,e)=>{if(e.KeyCode==Keys.Escape){f.DialogResult=DialogResult.Cancel;f.Close();}};}
    palette.Apply(f);return f.ShowDialog(owner);
   }
  }
 }
}
