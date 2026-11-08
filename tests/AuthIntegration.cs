using System;
using System.IO;
using System.Threading.Tasks;
using MacApp;
class AuthIntegration {
 static void Assert(bool ok,string reason){if(!ok)throw new Exception(reason);}
 static void Main(string[] args){Run(args).GetAwaiter().GetResult();}
 static async Task Run(string[] args){
  string directory=Path.Combine(Path.GetTempPath(),"MAC-Auth-Test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
  try{
   var admin=new AuthClient(directory);
   // Test-only loopback transport. Production Configure() always requires HTTPS.
   typeof(AuthClient).GetProperty("Server").GetSetMethod(true).Invoke(admin,new object[]{args[0]});
   await admin.Login("","","Synthetic Admin Password 47!",true,false);Assert(admin.Session.role=="admin"&&admin.Session.user==null,"Initial admin login");
   var created=await admin.Call("POST","/v1/admin/users",new{first="tuncay",last="guliyev",password="Synthetic Advisor 42!"});
   Assert(created.user.Name=="Tuncay Guliyev","Name normalization");var users=await admin.Call("GET","/v1/admin/users");Assert(users.users.Length==1,"Admin list");
   var user=new AuthClient(directory);typeof(AuthClient).GetProperty("Server").GetSetMethod(true).Invoke(user,new object[]{args[0]});
   bool rejected=false;try{await user.Login("Tuncay","Guliyev","Wrong",false,false);}catch(AuthFailure e){rejected=e.Status==401;}Assert(rejected,"Wrong password not rejected");
   await user.Login("Tuncay","Guliyev","Synthetic Advisor 42!",false,false);await user.Validate();Assert(user.ProfileKey.Length==64&&user.SuggestedFile.Contains("Tuncay_Guliyev"),"Profile/filename");
   rejected=false;try{await user.Call("GET","/v1/admin/users");}catch(AuthFailure e){rejected=e.Status==403;}Assert(rejected,"Advisor escalated to admin");
   await admin.Call("POST","/v1/admin/user",new{id=created.user.id,password="Synthetic Reset Password!"});rejected=false;try{await user.Validate();}catch(AuthFailure e){rejected=e.Status==401;}Assert(rejected,"Reset did not revoke");
   await admin.Assume(created.user);Assert(admin.Session.user.id==created.user.id&&admin.Session.role=="admin","Admin target");await admin.Logout();
   Assert(Directory.GetFiles(directory).Length==0,"Credentials persisted without remember");Console.WriteLine("PASS: C# client / live loopback API: login, names, errors, roles, reset, revoke, admin assume, logout; no credential files.");
  }finally{Directory.Delete(directory,true);}
 }
}
