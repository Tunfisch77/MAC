using System;
using System.IO;
using System.Threading;
class ProcessCheckTest {
 static void Main(string[] args) {
  if(args.Length>0){if(args[0]=="fail"){Console.Error.WriteLine("MAC_DIAGNOSTIC_FIXTURE");Environment.Exit(23);}if(args[0]=="wait")Thread.Sleep(5000);return;}
  string mono=Environment.GetEnvironmentVariable("MAC_TEST_MONO");string self=System.Reflection.Assembly.GetExecutingAssembly().Location;
  string executable=mono??self;string prefix=mono==null?"":"\""+self+"\" ";
  ProcessCheck.Run(executable,prefix+"pass",Path.GetDirectoryName(self),3000);
  bool failed=false;try{ProcessCheck.Run(executable,prefix+"fail",Path.GetDirectoryName(self),3000);}catch(Exception e){failed=e.Message.Contains("MAC_DIAGNOSTIC_FIXTURE")&&e.Message.Contains("23");}if(!failed)throw new Exception("Missing stderr or exit code");
  failed=false;try{ProcessCheck.Run(executable,prefix+"wait",Path.GetDirectoryName(self),100);}catch(Exception e){failed=e.Message.Contains("Zeitlimit");}if(!failed)throw new Exception("Timeout handling failed");
  Console.WriteLine("PASS: successful child, failing child stderr/exit code and timeout.");
 }
}
