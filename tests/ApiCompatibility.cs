using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
class ApiCompatibility {
 static void Main(string[] args) {
  var codes=typeof(OpCodes).GetFields().Where(f=>f.FieldType==typeof(OpCode)).Select(f=>(OpCode)f.GetValue(null)).ToDictionary(c=>unchecked((ushort)c.Value));
  var assembly=Assembly.LoadFile(System.IO.Path.GetFullPath(args[0]));int checkedCalls=0;
  foreach(var type in assembly.GetTypes())foreach(var method in type.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static|BindingFlags.DeclaredOnly).Cast<MethodBase>().Concat(type.GetConstructors(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static))) {
   var body=method.GetMethodBody();if(body==null)continue;byte[] il=body.GetILAsByteArray();int i=0;
   while(i<il.Length){ushort value=il[i++];if(value==254)value=(ushort)(0xfe00|il[i++]);var op=codes[value];
    if(op.OperandType==OperandType.InlineMethod){var called=method.Module.ResolveMethod(BitConverter.ToInt32(il,i));if(called.DeclaringType==typeof(string)&&new[]{"TrimStart","TrimEnd","Split"}.Contains(called.Name)){var parameters=called.GetParameters();if(parameters.Length==0||parameters[0].ParameterType!=typeof(char[]))throw new Exception("Unsupported string overload: "+called);checkedCalls++;}}
    switch(op.OperandType){case OperandType.InlineNone:break;case OperandType.ShortInlineBrTarget:case OperandType.ShortInlineI:case OperandType.ShortInlineVar:i++;break;case OperandType.InlineVar:i+=2;break;case OperandType.InlineI8:case OperandType.InlineR:i+=8;break;case OperandType.InlineSwitch:int count=BitConverter.ToInt32(il,i);i+=4+count*4;break;default:i+=4;break;}
   }
  }
  if(checkedCalls==0)throw new Exception("No string calls checked");Console.WriteLine("PASS: "+checkedCalls+" string calls use Framework-compatible char[] overloads.");
 }
}
