using Irony.Ast;
using Irony.Parsing;
using llvm_to_msil.Nodes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace llvm_to_msil
{
	class LLVMParser
	{
		private Parser Parser = new Parser(new LanguageData(new LLVMGrammar()));

		public AstNode Parse(string String)
		{
			var Tree = Parser.Parse(String);
			if (Tree.HasErrors())
			{
				var Output = new List<string>();
				foreach (var Message in Tree.ParserMessages)
				{
					Output.Add(String.Format("Error: {0}: {1}: {2} : @'{3}'", Message.Location, Message.ParserState, Message.Message, String.Substring(Message.Location.Position, 10)));
					
				}
				throw (new Exception(String.Join("\r\n", Output)));
			}

			return new LlvmAstBuilder().Build(Tree.Root);
		}
	}

	public class Program
	{
		// clang -O3 -emit-llvm test.c -c -o test.bc
		// llvm-dis test.bc
		static void Main(string[] args)
		{
			var LLVMParser = new LLVMParser();
			//var Tree = LLVMParser.Parse("%a = add i32 1, 2");
			var Tree = LLVMParser.Parse(File.ReadAllText("test.ll"));
			var AssemblyBuilder = Tree.GenerateType();
			AssemblyBuilder.Save(@"DynamicAssemblyExample.dll");
			var Method = AssemblyBuilder.GetModules()[1].GetTypes()[0].GetMethods()[0];
			Method.Invoke(null, new object[] { 0, IntPtr.Zero });
			//Console.WriteLine();

			//var Tree = LLVMParser.Parse("target triple = \"i686-pc-mingw32\"\ntarget triple = \"i686-pc-mingw32\"\n");
			//Console.WriteLine("{0}", Tree.ToJson());
			Console.ReadKey();
		}
	}
}
