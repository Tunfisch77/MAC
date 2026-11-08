using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
namespace MacApp {
 public sealed class SmoothGrid:DataGridView {public SmoothGrid(){DoubleBuffered=true;}}
 public sealed class SmoothTable:TableLayoutPanel {public SmoothTable(){DoubleBuffered=true;}}
 public static class Shapes {
  public static GraphicsPath Rounded(Rectangle r,int radius){var p=new GraphicsPath();int d=Math.Min(radius*2,Math.Min(r.Width,r.Height));if(d<2){p.AddRectangle(r);return p;}p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
 }
 public class SoftButton:Button,IThemedControl {
  protected Palette palette=new Palette(false);bool hover;
  public SoftButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;}
  public virtual void SetPalette(Palette p){palette=p;Invalidate();}
  protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
  protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
  protected override void OnPaint(PaintEventArgs e){
   e.Graphics.Clear(Parent==null?palette.Surface:Parent.BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
   bool primary=(Tag as string)=="primary";Color fill=primary?palette.Accent:(hover?palette.Selected:palette.Input);
   using(var path=Shapes.Rounded(new Rectangle(1,1,Math.Max(1,Width-3),Math.Max(1,Height-3)),9))using(var brush=new SolidBrush(fill))using(var pen=new Pen(Focused?palette.Brand:palette.Line,Focused?2:1)){e.Graphics.FillPath(brush,path);if(!primary)e.Graphics.DrawPath(pen,path);}
   TextRenderer.DrawText(e.Graphics,DisplayText,Font,new Rectangle(10,3,Math.Max(1,Width-20),Math.Max(1,Height-6)),Enabled?(primary?palette.Brand:palette.Text):palette.Muted,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|(TextAlign==ContentAlignment.MiddleLeft?TextFormatFlags.Left:TextFormatFlags.HorizontalCenter));
   if(Focused)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(6,6,Math.Max(1,Width-13),Math.Max(1,Height-13)),palette.Muted,fill);
  }
  protected virtual string DisplayText {get{return Text;}}
 }
 public sealed class InputFrame:Panel,IThemedControl {
  Palette palette=new Palette(false);public readonly TextBox Input;
  public InputFrame(TextBox input){Input=input;DoubleBuffered=true;Padding=new Padding(11,8,11,7);input.BorderStyle=BorderStyle.None;input.Dock=DockStyle.Fill;Controls.Add(input);input.GotFocus+=(s,e)=>Invalidate();input.LostFocus+=(s,e)=>Invalidate();Resize+=(s,e)=>Invalidate();}
  public void SetPalette(Palette p){palette=p;Input.BackColor=p.Input;Input.ForeColor=p.Text;Invalidate();}
  protected override void OnPaintBackground(PaintEventArgs e){e.Graphics.Clear(Parent==null?palette.Surface:Parent.BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var path=Shapes.Rounded(new Rectangle(1,1,Math.Max(1,Width-3),Math.Max(1,Height-3)),9))using(var b=new SolidBrush(palette.Input))using(var pen=new Pen(Input.Focused?palette.Brand:palette.Line,Input.Focused?2:1)){e.Graphics.FillPath(b,path);e.Graphics.DrawPath(pen,path);}}
 }
 public sealed class FormScrollPanel:Panel {
  public FormScrollPanel(){AutoScroll=true;DoubleBuffered=true;}
  public void ScrollBy(int delta){int current=-AutoScrollPosition.Y;AutoScrollPosition=new Point(0,Math.Max(0,current-delta/120*60));}
 }
 public sealed class PolicyTextBox:TextBox {
  public PolicyTextBox(){MaxLength=12;ShortcutsEnabled=true;ImeMode=ImeMode.Disable;}
  public bool TryInsertText(string value){string next=Text.Remove(SelectionStart,SelectionLength).Insert(SelectionStart,value);if(next.Length>12||next.Any(c=>c<'0'||c>'9'))return false;SelectedText=value;return true;}
  protected override void OnKeyPress(KeyPressEventArgs e){if(!char.IsControl(e.KeyChar)&&(e.KeyChar<'0'||e.KeyChar>'9'))e.Handled=true;base.OnKeyPress(e);}
  protected override void WndProc(ref Message m){if(m.Msg==0x0302){try{if(Clipboard.ContainsText())TryInsertText(Clipboard.GetText());}catch{}return;}base.WndProc(ref m);}
 }
 public sealed class ClickChoice:SoftButton {
  public readonly List<string> Items=new List<string>();ToolStripDropDown popup;
  public Action<int> ScrollRequested;
  public ClickChoice(){TextAlign=ContentAlignment.MiddleLeft;}
  protected override string DisplayText {get{return (string.IsNullOrEmpty(Text)?"Bitte auswählen":Text)+"   ▾";}}
  protected override void OnMouseWheel(MouseEventArgs e){if(ScrollRequested!=null)ScrollRequested(e.Delta);}
  protected override bool ProcessCmdKey(ref Message msg,Keys key){if(key==Keys.Up||key==Keys.Down||key==Keys.PageUp||key==Keys.PageDown)return true;return base.ProcessCmdKey(ref msg,key);}
  protected override void OnClick(EventArgs e){base.OnClick(e);if(popup!=null){popup.Close();popup.Dispose();}
   var list=new FlowLayoutPanel{Size=new Size(Math.Max(Width,330),Math.Min(300,Items.Count*40+14)),AutoScroll=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(5)};
   foreach(var item in Items.Where(x=>x!="")){string selected=item;var button=new SoftButton{Text=item,Width=list.Width-30,Height=34,TextAlign=ContentAlignment.MiddleLeft,Margin=new Padding(0,0,0,4)};button.Click+=(s,a)=>{Text=selected;popup.Close();};list.Controls.Add(button);}
   palette.Apply(list);popup=new ToolStripDropDown{Padding=Padding.Empty};popup.Items.Add(new ToolStripControlHost(list){AutoSize=false,Size=list.Size,Margin=Padding.Empty,Padding=Padding.Empty});popup.Show(this,new Point(0,Height));
  }
  public void TestWheel(int delta){OnMouseWheel(new MouseEventArgs(MouseButtons.None,0,0,0,delta));}
  protected override void Dispose(bool disposing){if(disposing&&popup!=null)popup.Dispose();base.Dispose(disposing);}
 }
}
