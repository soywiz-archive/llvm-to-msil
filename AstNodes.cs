using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace llvm_to_msil.Nodes
{
	public class AstAnalyzeContext
	{
		public Dictionary<string, AstDeclareFunction> FunctionList = new Dictionary<string,AstDeclareFunction>();

		internal void AddFunction(AstDeclareFunction AstDeclareFunction)
		{
			FunctionList.Add(AstDeclareFunction.Name, AstDeclareFunction);
		}
	}

	public class AstGenerateContext
	{
		public readonly AssemblyName AssemblyName;
		public readonly AssemblyBuilder AssemblyBuilder;
		public readonly ModuleBuilder ModuleBuilder;
		public readonly TypeBuilder TypeBuilder;
		public ILGenerator ILGenerator { get; private set; }
		private MethodBuilder MethodBuilder;
		public Type Type;

		public AstGenerateContext()
		{
			AssemblyName = new AssemblyName("DynamicAssemblyExample");
			AssemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(AssemblyName, AssemblyBuilderAccess.RunAndSave);
			ModuleBuilder = AssemblyBuilder.DefineDynamicModule(AssemblyName.Name, AssemblyName.Name + ".dll");
			TypeBuilder = ModuleBuilder.DefineType("MyDynamicType", TypeAttributes.Public);
		}

		public void Finalize()
		{
			Type = TypeBuilder.CreateType();
		}

		public void BeginFunction(string Name, Type ReturnType, Type[] ParameterTypes, string[] Names)
		{
			Locals = new Dictionary<string, LocalBuilder>();
			Arguments = new Dictionary<string, int>();

			Console.WriteLine("BeginFunction: {0} {1}({2})", Name, ReturnType, String.Join(",", ParameterTypes.Select(Type => Type.ToString())));
			MethodBuilder = TypeBuilder.DefineMethod(Name, MethodAttributes.Final | MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, ReturnType, ParameterTypes);
			ILGenerator = MethodBuilder.GetILGenerator();
			for (int n = 0; n < Names.Length; n++)
			{
				Arguments[Names[n]] = n;
			}
		}

		public void EndFunction()
		{
			//MethodBuilder.comple
		}

		Dictionary<string, LocalBuilder> Locals;
		Dictionary<string, int> Arguments;

		public LocalBuilder CreateLocal(string Name, Type Type)
		{
			return Locals[Name] = ILGenerator.DeclareLocal(Type);
		}

		public bool IsLocal(string Name)
		{
			return Locals.ContainsKey(Name);
		}

		public LocalBuilder GetLocal(string Name)
		{
			return Locals[Name];
		}

		public int GetArgument(string Name)
		{
			return Arguments[Name];
		}

		public MethodInfo GetFunction(string FunctionName)
		{
			switch (FunctionName)
			{
				case "@printf":
					return typeof(Runtime).GetMethod("printf");
			}
			throw new NotImplementedException();
		}
	}

	unsafe public class Runtime
	{
		static public int printf(sbyte* Format, params object[] Args)
		{
			return 0;
		}
	}

	abstract public class AstNode
	{
		public AstNode()
		{
		}

		public AssemblyBuilder GenerateType()
		{
			var AstAnalyzeContext = new AstAnalyzeContext();
			this.Analyze(AstAnalyzeContext);
			var AstGenerateContext = new AstGenerateContext();
			this.Generate(AstGenerateContext);
			AstGenerateContext.Finalize();
			return AstGenerateContext.AssemblyBuilder;
		}

		virtual public void Analyze(AstAnalyzeContext Context)
		{
		}

		virtual public void Generate(AstGenerateContext Context)
		{
		}

		public string ToJson()
		{
			return JsonConvert.SerializeObject(this, Formatting.Indented);
		}
	}

	public class AstNodeModifiers : AstNode
	{
		public HashSet<string> Modifiers;

		public AstNodeModifiers(string[] Modifiers)
		{
			this.Modifiers = new HashSet<string>(Modifiers);
		}

		public bool Has(string Modifier)
		{
			return this.Modifiers.Contains(Modifier);
		}

	}

	public class AstNodeTarget : AstNode
	{
		public string Type;
		public string Value;

		public AstNodeTarget(string Type, string Value)
		{
			this.Type = Type;
			this.Value = Value;
		}
	}

	public class AstNodeContainer<TAstNode> : AstNode where TAstNode : AstNode
	{
		public TAstNode[] Items;

		public AstNodeContainer(IEnumerable<TAstNode> Items)
		{
			this.Items = Items.ToArray();
		}

		public override void Analyze(AstAnalyzeContext Context)
		{
			foreach (var Item in Items) Item.Analyze(Context);
		}

		public override void Generate(AstGenerateContext Context)
		{
			foreach (var Item in Items) Item.Generate(Context);
		}
	}

	abstract public class AstNodeType : AstNode
	{
		abstract public Type ToNativeType();
	}

	public class AstNodeTypePointer : AstNodeType
	{
		public AstNodeType PointeeType;

		public AstNodeTypePointer(AstNodeType PointeeType)
		{
			this.PointeeType = PointeeType;
		}

		public override Type ToNativeType()
		{
			return PointeeType.ToNativeType().MakePointerType();
		}
	}

	public class AstNodeTypeEllipsis : AstNodeType
	{
		public override Type ToNativeType()
		{
			throw new NotImplementedException();
		}
	}

	public class AstNodeTypeBase : AstNodeType
	{
		public string TypeName;

		public AstNodeTypeBase(string TypeName)
		{
			this.TypeName = TypeName;
		}

		public override Type ToNativeType()
		{
			switch (TypeName)
			{
				case "i8": return typeof(sbyte);
				case "i16": return typeof(short);
				case "i32": return typeof(int);
				case "i64": return typeof(long);
			}
			throw new NotImplementedException();
		}
	}

	public class AstNodeTypeArray : AstNodeType
	{
		public string Count;
		public AstNodeType ElementType;

		public AstNodeTypeArray(string Count, AstNodeType ElementType)
		{
			this.Count = Count;
			this.ElementType = ElementType;
		}

		public override Type ToNativeType()
		{
			return ElementType.ToNativeType().MakeArrayType();
		}
	}

	public class AstNodeTypeFunction : AstNodeType
	{
		public AstNodeType ReturnType;
		public AstNode ParameterType;

		public AstNodeTypeFunction(AstNodeType ReturnType, AstNode ParameterType)
		{
			this.ReturnType = ReturnType;
			this.ParameterType = ParameterType;
		}

		public override Type ToNativeType()
		{
			return typeof(Delegate);
			//throw new NotImplementedException();
		}
	}

	abstract public class AstNodeStatement : AstNode
	{
		public AstNodeStatement()
		{
		}
	}

	abstract public class AstNodeExpression : AstNode
	{
		public AstNodeExpression()
		{
		}

	}

	public class AstNodeExpressionLiteral : AstNodeExpression
	{
		//public AstNodeType AstNodeType;
		public object Value;

		public AstNodeExpressionLiteral(object Value)
		{
			this.Value = Value;
		}

		public override void Generate(AstGenerateContext Context)
		{
			//base.Generate(Context);
			throw(new NotImplementedException());
		}
	}

	public class AstNodeExpressionLiteralInteger : AstNodeExpression
	{
		//public AstNodeType AstNodeType;
		public int Value;

		public AstNodeExpressionLiteralInteger(int Value)
		{
			this.Value = Value;
		}

		public override void Generate(AstGenerateContext Context)
		{
			Context.ILGenerator.Emit(OpCodes.Ldc_I4, Value);
			//base.Generate(Context);
			//throw (new NotImplementedException());
		}
	}

	public class AstNodeExpressionTypeLiteral : AstNodeExpression
	{
		public AstNodeType AstNodeType;
		public string Name;

		public AstNodeExpressionTypeLiteral(AstNodeType AstNodeType, string Name)
		{
			this.AstNodeType = AstNodeType;
			this.Name = Name;
		}

		public override void Generate(AstGenerateContext Context)
		{
			//base.Generate(Context);
			switch (Name[0])
			{
				case '%':
					if (Context.IsLocal(Name))
					{
						Context.ILGenerator.Emit(OpCodes.Ldloc, Context.GetLocal(Name));
					}
					else
					{
						Context.ILGenerator.Emit(OpCodes.Ldarg, Context.GetArgument(Name));
					}
					break;
				case '@':
					throw (new NotImplementedException());
					break;
				default:
					Context.ILGenerator.Emit(OpCodes.Ldc_I4, Convert.ToInt32(Name));
					break;
			}
		}
	}

	public class AstNodeExpressionIdentifier : AstNodeExpression
	{
		public string Name;

		public AstNodeExpressionIdentifier()
		{
		}

		public AstNodeExpressionIdentifier(string Name = null)
		{
			this.Name = Name;
		}

		public override void Generate(AstGenerateContext Context)
		{
			switch (Name[0])
			{
				case '%':
					if (Context.IsLocal(Name))
					{
						Context.ILGenerator.Emit(OpCodes.Ldloc, Context.GetLocal(Name));
					}
					else
					{
						Context.ILGenerator.Emit(OpCodes.Ldarg, Context.GetArgument(Name));
					}
					break;
				//case '@': break;
				default:
					throw (new NotImplementedException());
			}
			//base.Generate(Context);
		}
	}


	public class AstNodeExpressionBinaryOperation : AstNodeExpression
	{
		public string Destination;
		public string Operation;
		public AstNodeType Type;
		public AstNode Left;
		public AstNode Right;

		public AstNodeExpressionBinaryOperation(string Destination, string Operation, AstNodeType Type, AstNode Left, AstNode Right)
		{
			this.Destination = Destination;
			this.Operation = Operation;
			this.Type = Type;
			this.Left = Left;
			this.Right = Right;
		}

		public override void Generate(AstGenerateContext Context)
		{
			var TypeNet = Type.ToNativeType();
			var Local = Context.CreateLocal(Destination, TypeNet);
			//var Local = Context.ILGenerator.DeclareLocal(Type.ToNetType());

			//Console.WriteLine("BinOP: {0}", Operation);

			Left.Generate(Context);
			Right.Generate(Context);

			switch (Operation)
			{
				case "add":
					Context.ILGenerator.Emit(OpCodes.Add);
					Context.ILGenerator.Emit(OpCodes.Stloc, Local);
					break;
				default:
					throw(new NotImplementedException());
			}
			//base.Generate(Context);
		}
	}

	public class AstNodeDeclareGlobal : AstNode
	{
		public string Name;
		public AstNode Modifiers;
		public string ConstantOrVariable;
		public AstNode Type;
		public AstNode Value;

		public AstNodeDeclareGlobal(string Name, AstNode Modifiers, string ConstantOrVariable, AstNode Type, AstNode Value)
		{
			this.Name = Name;
			this.Modifiers = Modifiers;
			this.ConstantOrVariable = ConstantOrVariable;
			this.Type = Type;
			this.Value = Value;
		}
	}

	public class AstDefineFunction : AstNode
	{
		public AstNodeType ReturnType;
		public string FunctionName;
		public AstNodeContainer<AstNodeParameter> ParameterList;
		public AstNode ExtraInfo;
		public AstNode Statements;

		public AstDefineFunction(AstNodeType ReturnType, string FunctionName, AstNodeContainer<AstNodeParameter> ParameterList, AstNode ExtraInfo, AstNode Statements)
		{
			this.ReturnType = ReturnType;
			this.FunctionName = FunctionName;
			this.ParameterList = ParameterList;
			this.ExtraInfo = ExtraInfo;
			this.Statements = Statements;
		}

		public override void Analyze(AstAnalyzeContext Context)
		{
			// Register function
			//base.Analyze(Context);
		}

		public override void Generate(AstGenerateContext Context)
		{
			Context.BeginFunction(
				FunctionName,
				ReturnType.ToNativeType(),
				ParameterList.Items.Select(Parameter => Parameter.Type.ToNativeType()).ToArray(),
				ParameterList.Items.Select(Parameter => Parameter.Name).ToArray()
			);
			{
				Statements.Generate(Context);
			}
			Context.EndFunction();
			//base.Generate(Context);
		}
	}

	public class AstNodeParameter : AstNode
	{
		public AstNodeType Type;
		public AstNode Attributes;
		public string Name;

		public AstNodeParameter(AstNodeType ParameterType, AstNode ParameterAttributes, string ParameterName)
		{
			this.Type = ParameterType;
			this.Attributes = ParameterAttributes;
			this.Name = ParameterName;
		}
	}

	public class AstNodeLabel : AstNode
	{
		public string Name;

		public AstNodeLabel(string Name)
		{
			this.Name = Name;
		}
	}

	public class AstNodeFunctionCall : AstNode
	{
		public string DestinationName;
		public AstNode Modifiers;
		public AstNodeType CallType;
		public string FunctionName;
		public AstNode Parameters;
		public AstNode Attributes;

		public AstNodeFunctionCall(string DestinationName, AstNode Modifiers, AstNodeType CallType, string FunctionName, AstNode Parameters, AstNode Attributes)
		{
			this.DestinationName = DestinationName;
			this.Modifiers = Modifiers;
			this.CallType = CallType;
			this.FunctionName = FunctionName;
			this.Parameters = Parameters;
			this.Attributes = Attributes;
		}

		public override void Generate(AstGenerateContext Context)
		{
			var DestinationLocal = Context.CreateLocal(DestinationName, CallType.ToNativeType());
			var Function = Context.GetFunction(FunctionName);
			Parameters.Generate(Context);
			Context.ILGenerator.Emit(OpCodes.Call, Function);
			Context.ILGenerator.Emit(OpCodes.Stloc, DestinationLocal);
		}
	}

	public class AstNodeGetElementPtr : AstNode
	{
		public AstNode Type;
		public AstNode Value;
		public AstNode IndexList;

		public AstNodeGetElementPtr(AstNode Type, AstNode Value, AstNode IndexList)
		{
			this.Type = Type;
			this.Value = Value;
			this.IndexList = IndexList;
		}
	}

	public class AstNodeReturn : AstNode
	{
		public AstNode ReturnValue;

		public AstNodeReturn(AstNode ReturnValue)
		{
			this.ReturnValue = ReturnValue;
		}

		public override void Generate(AstGenerateContext Context)
		{
			ReturnValue.Generate(Context);
			Context.ILGenerator.Emit(OpCodes.Ret);
		}
	}

	public class AstDeclareFunction : AstNode
	{
		public AstNodeType ReturnType;
		public string Name;
		public AstNode Parameters;
		public AstNode Attributes;

		public AstDeclareFunction(AstNodeType ReturnType, string Name, AstNode Parameters, AstNode Attributes)
		{
			this.ReturnType = ReturnType;
			this.Name = Name;
			this.Parameters = Parameters;
			this.Attributes = Attributes;
		}

		public override void Analyze(AstAnalyzeContext Context)
		{
			Context.AddFunction(this);
		}
	}

	public class AstNodeDeclareParameter : AstNode
	{
		public AstNodeType NodeType;
		public AstNodeModifiers Modifiers;

		public AstNodeDeclareParameter(AstNodeType astNodeType, AstNodeModifiers astNodeModifiers)
		{
			this.NodeType = astNodeType;
			this.Modifiers = astNodeModifiers;
		}
	}
}
