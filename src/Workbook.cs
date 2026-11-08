using System;
using System.IO;
using System.IO.Compression;
using System.Xml.Linq;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

namespace MacApp {
public sealed class Record {
 public string[] Values = new string[10];
 public XElement Original;
 public string[] OriginalValues;
 public Record() { for(int i=0;i<10;i++) Values[i]=""; }
 public Record Clone() { return new Record { Values=(string[])Values.Clone(), Original=Original==null?null:new XElement(Original), OriginalValues=OriginalValues==null?null:(string[])OriginalValues.Clone() }; }
 public void Clear() { Array.Clear(Values,0,Values.Length);if(OriginalValues!=null)Array.Clear(OriginalValues,0,OriginalValues.Length);if(Original!=null)Original.RemoveAll();Original=null;OriginalValues=null; }
}
public sealed class Book : IDisposable {
 public const string NotSpecified="Nicht angegeben";
 public static readonly XNamespace N="http://schemas.openxmlformats.org/spreadsheetml/2006/main";
 public Dictionary<string,byte[]> Parts=new Dictionary<string,byte[]>();
 public List<Record> Rows=new List<Record>();
 public string[] Headers;
 public XElement Pattern;
 public static string Text(XElement c,string[] strings) {
  if(c==null)return "";
  string t=(string)c.Attribute("t"),v=(string)c.Element(N+"v")??"";
  if(t=="s")return strings[int.Parse(v,CultureInfo.InvariantCulture)];
  if(t=="inlineStr")return string.Concat(c.Descendants(N+"t").Select(x=>x.Value));
  return v;
 }
 public static Book Load(string file) {
  using(var f=File.OpenRead(file))return Load(f);
 }
 public static Book Load(Stream input){
  if(input.Length>20*1024*1024)throw new Exception("Die Datei ist größer als 20 MB.");
  Book b=new Book();
  using(var z=new ZipArchive(input,ZipArchiveMode.Read,true)) {
   long total=0;
   foreach(var e in z.Entries){ total+=e.Length;if(total>80*1024*1024)throw new Exception("Die Excel-Datei ist zu groß.");using(var s=e.Open())using(var m=new MemoryStream()){s.CopyTo(m);b.Parts.Add(e.FullName,m.ToArray());} }
  }
  if(!b.Parts.ContainsKey("xl/vbaProject.bin"))throw new Exception("Bitte die MAC-Vorlage im Format .xlsm auswählen. Die Makros fehlen.");
  var wb=b.Xml("xl/workbook.xml");
  if(wb.Descendants(N+"sheet").Count()!=1 || (string)wb.Descendants(N+"sheet").First().Attribute("name")!="Tabelle1")throw new Exception("Diese Version unterstützt die MAC-Vorlage mit dem Blatt Tabelle1.");
  var sheet=b.Xml("xl/worksheets/sheet1.xml");
  if(sheet.Descendants(N+"f").Any())throw new Exception("Diese Datei enthält Zellformeln. Der Import wird abgebrochen, damit keine Formeln überschrieben werden.");
  string[] strings=b.Parts.ContainsKey("xl/sharedStrings.xml")?b.Xml("xl/sharedStrings.xml").Root.Elements(N+"si").Select(x=>string.Concat(x.Descendants(N+"t").Select(t=>t.Value))).ToArray():new string[0];
  var data=sheet.Root.Element(N+"sheetData");
  var header=data.Elements(N+"row").First(x=>(int)x.Attribute("r")==4);
  b.Headers=Enumerable.Range(0,10).Select(i=>Text(header.Elements(N+"c").FirstOrDefault(c=>(string)c.Attribute("r")==((char)('A'+i))+"4"),strings)).ToArray();
  string[] expected={"VN","Polizzennr.","Sparte","Antwort Betreuer","Inkassostatus","Kündigung","KFZ Risiko","VM vorhanden?","SS","offene Prämie"};
  if(!b.Headers.Select(x=>x.Trim()).SequenceEqual(expected))throw new Exception("Die Spalten entsprechen nicht der MAC-Vorlage. Es wurde nichts importiert.");
  b.Pattern=new XElement(data.Elements(N+"row").First(x=>(int)x.Attribute("r")==5));
  foreach(var row in data.Elements(N+"row").Where(x=>(int)x.Attribute("r")>=5)) {
   var r=new Record();int number=(int)row.Attribute("r");
   for(int i=0;i<10;i++){
    var c=row.Elements(N+"c").FirstOrDefault(x=>(string)x.Attribute("r")==((char)('A'+i))+number.ToString());
    r.Values[i]=Text(c,strings);
    if(i==8 && r.Values[i]!="" && !IsNotSpecified(r.Values[i])) {decimal v;if(!decimal.TryParse(r.Values[i],NumberStyles.Float,CultureInfo.InvariantCulture,out v))throw new Exception("Ungültiger Prozentwert in der Vorlage.");r.Values[i]=(v*100).ToString("0.####################",CultureInfo.GetCultureInfo("de-AT"));}
    if(i==9 && r.Values[i]!="" && !IsNotSpecified(r.Values[i])) {decimal v;if(!decimal.TryParse(r.Values[i],NumberStyles.Float,CultureInfo.InvariantCulture,out v))throw new Exception("Ungültiger Prämienwert in der Vorlage.");r.Values[i]=v.ToString("0.####################",CultureInfo.GetCultureInfo("de-AT"));}
   }
   if(r.Values.All(string.IsNullOrEmpty))continue;
   r.Original=new XElement(row);r.OriginalValues=(string[])r.Values.Clone();r.Values[0]=NormalizeName(r.Values[0]);b.Rows.Add(r);
  }
  if(b.Rows.Count>996)throw new Exception("Maximal 996 Einträge werden unterstützt.");
  return b;
 }
 public XDocument Xml(string name){using(var m=new MemoryStream(Parts[name]))return XDocument.Load(m);}
 public static decimal Number(string value){ decimal d;if(!decimal.TryParse(value,NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint,CultureInfo.GetCultureInfo("de-AT"),out d))throw new Exception("Zahlen bitte ohne Tausenderpunkte eingeben, z. B. 1234,56.");return d; }
 public static bool IsNotSpecified(string value){return string.Equals((value??"").Trim(),NotSpecified,StringComparison.OrdinalIgnoreCase);}
 public static string FieldError(Record r,int i){
  string value=r.Values[i]??"";
  if(i==1&&!ValidPolicy(value))return "Polizzennummer: 8–12 Ziffern (0–9), ohne Leerzeichen.";
  if(string.IsNullOrWhiteSpace(value))return "Bitte ausfüllen oder „Nicht angegeben“ wählen.";
  if(value.Length>32000)return "Maximal 32.000 Zeichen möglich.";
  if(i==2&&value.Split(new char[]{','}).Any(IsNotSpecified)&&!IsNotSpecified(value))return "„Nicht angegeben“ bitte ohne weitere Sparte wählen.";
  if((i==8||i==9)&&!IsNotSpecified(value))try{Number(value);}catch{return "Zahl mit Komma, ohne Tausenderpunkte, oder „Nicht angegeben“.";}
  return "";
 }
 public static bool ValidPolicy(string value){return value!=null&&value.Length>=8&&value.Length<=12&&value.All(c=>c>='0'&&c<='9');}
 public static string NormalizeName(string value){
  if(IsNotSpecified(value))return NotSpecified;
  var chars=(value??"").ToCharArray();bool start=true;
  for(int i=0;i<chars.Length;i++){if(char.IsWhiteSpace(chars[i]))start=true;else{if(start)chars[i]=char.ToUpper(chars[i],CultureInfo.GetCultureInfo("de-AT"));start=false;}}return new string(chars);
 }
 public int DuplicateIndex(string policy,int except){if(!ValidPolicy(policy))return -1;for(int i=0;i<Rows.Count;i++)if(i!=except&&Rows[i].Values[1]==policy)return i;return -1;}
 public string RecordError(Record r,int index){for(int i=0;i<10;i++){string error=FieldError(r,i);if(error!="")return error;}return DuplicateIndex(r.Values[1],index)>=0?"Polizzennummer bereits vorhanden.":"";}
 public static void Validate(Record r){for(int i=0;i<10;i++){string error=FieldError(r,i);if(error!="")throw new Exception("Feld "+(i+1)+": "+error);}}
 public byte[] Export(string advisor=null){
  if(Rows.Count==0)throw new Exception("Bitte zuerst einen Eintrag hinzufügen oder eine vorhandene Datei öffnen.");
  if(Rows.Count>996)throw new Exception("Maximal 996 Einträge werden unterstützt.");
  foreach(var r in Rows)r.Values[0]=NormalizeName(r.Values[0]);
  for(int i=0;i<Rows.Count;i++){string error=RecordError(Rows[i],i);if(error!="")throw new Exception("Eintrag "+(i+1)+": "+error);}
  var doc=Xml("xl/worksheets/sheet1.xml");var data=doc.Root.Element(N+"sheetData");
  data.Elements(N+"row").Where(x=>(int)x.Attribute("r")>=5).Remove();
  for(int index=0;index<Rows.Count;index++){
   var r=Rows[index]; int number=index+5;
   var row=new XElement(r.Original??Pattern);row.SetAttributeValue("r",number);
   foreach(var c in row.Elements(N+"c")){string a=(string)c.Attribute("r");c.SetAttributeValue("r",new string(a.TakeWhile(char.IsLetter).ToArray())+number);}
   for(int i=0;i<10;i++){
    string address=((char)('A'+i))+number.ToString();var c=row.Elements(N+"c").FirstOrDefault(x=>(string)x.Attribute("r")==address);
    if(c==null){c=new XElement(N+"c",new XAttribute("r",address));row.Add(c);}
    if(r.Original!=null && r.Values[i]==r.OriginalValues[i])continue;
    c.RemoveNodes();if(c.Attribute("t")!=null)c.Attribute("t").Remove();
    if(string.IsNullOrEmpty(r.Values[i]))continue;
    if((i==8||i==9)&&!IsNotSpecified(r.Values[i])){decimal d=Number(r.Values[i]);if(i==8)d/=100;c.Add(new XElement(N+"v",d.ToString(CultureInfo.InvariantCulture)));}
    else { c.SetAttributeValue("t","inlineStr");c.Add(new XElement(N+"is",new XElement(N+"t",new XAttribute(XNamespace.Xml+"space","preserve"),r.Values[i]))); }
   }
   // Excel requires ascending cell order.
   var cells=row.Elements(N+"c").OrderBy(c=>((string)c.Attribute("r")).TakeWhile(char.IsLetter).Count()).ThenBy(c=>(string)c.Attribute("r")).ToArray();row.RemoveNodes();row.Add(cells);data.Add(row);
  }
  if(doc.Root.Element(N+"dimension")!=null)doc.Root.Element(N+"dimension").SetAttributeValue("ref","A1:J"+(Rows.Count+4));
  foreach(var validation in doc.Descendants(N+"dataValidation")){
   var formula=validation.Element(N+"formula1");
   if(formula!=null&&formula.Value.StartsWith("\"",StringComparison.Ordinal)&&formula.Value.EndsWith("\"",StringComparison.Ordinal)&&!formula.Value.Contains(NotSpecified))formula.Value=formula.Value.Substring(0,formula.Value.Length-1)+","+NotSpecified+"\"";
  }
  // Resolve shared strings so removed records cannot remain in an unused string table.
  string[] shared=Parts.ContainsKey("xl/sharedStrings.xml")?Xml("xl/sharedStrings.xml").Root.Elements(N+"si").Select(x=>string.Concat(x.Descendants(N+"t").Select(t=>t.Value))).ToArray():new string[0];
  foreach(var c in doc.Descendants(N+"c").Where(x=>(string)x.Attribute("t")=="s")){string text=shared[int.Parse(c.Element(N+"v").Value)];c.RemoveNodes();c.SetAttributeValue("t","inlineStr");c.Add(new XElement(N+"is",new XElement(N+"t",new XAttribute(XNamespace.Xml+"space","preserve"),text)));}
  if(!string.IsNullOrEmpty(advisor)){
   if(advisor.Length>161||advisor.Any(char.IsControl))throw new Exception("Ungültiger Beratername.");
   var row3=data.Elements(N+"row").FirstOrDefault(x=>(int)x.Attribute("r")==3);
   if(row3==null)throw new Exception("Der Kopfbereich der Vorlage fehlt.");
   foreach(var cell in row3.Elements(N+"c")){
    string address=(string)cell.Attribute("r");if(address=="A3")continue;string value=Text(cell,new string[0]);
    if(value!=""&&!(address=="B3"&&value.StartsWith("Berater: ",StringComparison.Ordinal)))throw new Exception("Zeile 3 enthält eigene Inhalte. Der Beratername wurde nicht darübergeschrieben; bitte eine unveränderte MAC-Vorlage verwenden.");
   }
   var target=row3.Elements(N+"c").FirstOrDefault(x=>(string)x.Attribute("r")=="B3");if(target==null){target=new XElement(N+"c",new XAttribute("r","B3"));row3.Add(target);}target.RemoveNodes();target.SetAttributeValue("t","inlineStr");target.SetAttributeValue("s","48");target.Add(new XElement(N+"is",new XElement(N+"t","Berater: "+advisor)));row3.SetAttributeValue("ht",25);row3.SetAttributeValue("customHeight",1);
   var merges=doc.Root.Element(N+"mergeCells");if(merges==null)throw new Exception("Vorlagenformat nicht unterstützt.");
   if(!merges.Elements(N+"mergeCell").Any(x=>(string)x.Attribute("ref")=="B3:J3"))merges.Add(new XElement(N+"mergeCell",new XAttribute("ref","B3:J3")));merges.SetAttributeValue("count",merges.Elements().Count());
  }
  var book=Xml("xl/workbook.xml");foreach(var n in book.Descendants(N+"definedName").Where(x=>(string)x.Attribute("name")=="_xlnm.Print_Area"))n.Value="'Tabelle1'!$A$1:$J$"+(Rows.Count+4);
  using(var output=new MemoryStream()){
   using(var z=new ZipArchive(output,ZipArchiveMode.Create,true))foreach(var p in Parts){var e=z.CreateEntry(p.Key,CompressionLevel.Optimal);using(var s=e.Open()){if(p.Key=="xl/worksheets/sheet1.xml")doc.Save(s);else if(p.Key=="xl/workbook.xml")book.Save(s);else if(p.Key=="xl/sharedStrings.xml")new XDocument(new XElement(N+"sst",new XAttribute("count",0),new XAttribute("uniqueCount",0))).Save(s);else s.Write(p.Value,0,p.Value.Length);}}
   return output.ToArray();
  }
 }
 public void Dispose(){foreach(var r in Rows)r.Clear();Rows.Clear();foreach(var b in Parts.Values)Array.Clear(b,0,b.Length);Parts.Clear();if(Pattern!=null)Pattern.RemoveAll();Pattern=null;Headers=null;}
}
}
