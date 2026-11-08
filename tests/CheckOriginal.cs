using System;using System.IO;using System.Linq;using MacApp;
class CheckOriginal {
 static void Main(string[] args){using(var b=Book.Load(args[0])){
  int invalid=b.Rows.Count(r=>!Book.ValidPolicy(r.Values[1]));int duplicated=b.Rows.Count(r=>b.DuplicateIndex(r.Values[1],b.Rows.IndexOf(r))>=0);
  var session=new SessionState{Book=b,Draft=new Record().Values,Baseline="",Editing=-1,Dirty=true};byte[] encoded=SessionCodec.Encode(session);
  using(var restored=SessionCodec.Decode(encoded)){
   if(restored.Book.Rows.Count!=b.Rows.Count)throw new Exception("Recovery row loss");
   for(int i=0;i<b.Rows.Count;i++){if(!restored.Book.Rows[i].Values.SequenceEqual(b.Rows[i].Values)||!restored.Book.Rows[i].OriginalValues.SequenceEqual(b.Rows[i].OriginalValues)||restored.Book.Rows[i].Original.ToString()!=b.Rows[i].Original.ToString())throw new Exception("Original recovery mismatch");}
   foreach(var part in b.Parts)if(!restored.Book.Parts[part.Key].SequenceEqual(part.Value))throw new Exception("Recovery package changed");
  }Array.Clear(encoded,0,encoded.Length);
  // Real source is read only. Test each valid-policy row independently in memory,
  // explicitly filling source blanks with the marker; never invent a policy number.
  int checkedRows=0,checkedValues=0;
  foreach(var record in b.Rows){
   if(!Book.ValidPolicy(record.Values[1]))continue;
   using(var single=Book.Load(args[0])){foreach(var old in single.Rows)old.Clear();single.Rows.Clear();var copy=record.Clone();for(int i=0;i<10;i++)if(string.IsNullOrWhiteSpace(copy.Values[i]))copy.Values[i]=Book.NotSpecified;single.Rows.Add(copy);byte[] output=single.Export();using(var stream=new MemoryStream(output))using(var result=Book.Load(stream)){
    for(int i=0;i<10;i++){if(result.Rows[0].Values[i]!=copy.Values[i])throw new Exception("Export value mismatch");checkedValues++;}
    if(!result.Parts["xl/vbaProject.bin"].SequenceEqual(b.Parts["xl/vbaProject.bin"]))throw new Exception("VBA changed");
   }Array.Clear(output,0,output.Length);checkedRows++;}
  }
  Console.WriteLine("PASS: "+b.Rows.Count+" source rows preserved in recovery; "+checkedRows+" valid-policy rows / "+checkedValues+" values exported in RAM; VBA unchanged.");
  Console.WriteLine("Source requires review: "+invalid+" policy formats invalid under new 8–12 digit rule; "+duplicated+" rows with duplicate valid policy. No customer output written.");
  session.Book=null;session.Dispose();
 }}
}
