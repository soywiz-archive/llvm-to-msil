using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace llvm_to_msil.Nodes
{
	public class AstNode
	{
		public AstNode()
		{
		}

		public string ToJson()
		{
			return JsonConvert.SerializeObject(this, Formatting.Indented);
		}
	}

	public class AstNodeModifiers : AstNode
	{
		public string[] Modifiers;

		public AstNodeModifiers(string[] Modifiers)
		{
			this.Modifiers = Modifiers;
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
	}

	abstract public class AstNodeType : AstNode
	{
	}

	public class AstNodeTypePointer : AstNodeType
	{
		public AstNodeType PointeeType;

		public AstNodeTypePointer(AstNodeType PointeeType)
		{
			this.PointeeType = PointeeType;
		}
	}

	public class AstNodeTypeEllipsis : AstNodeType
	{
	}

	public class AstNodeTypeBase : AstNodeType
	{
		public string TypeName;

		public AstNodeTypeBase(string TypeName)
		{
			this.TypeName = TypeName;
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
	}

	public class AstNodeTypeFunction : AstNodeType
	{
		public AstNodeType astNodeType;
		public AstNode astNode;

		public AstNodeTypeFunction(AstNodeType ReturnType, AstNode ParameterType)
		{
			this.astNodeType = ReturnType;
			this.astNode = ParameterType;
		}
	}

	public class AstNodeStatement : AstNode
	{
		public AstNodeStatement()
		{
		}
	}

	public class AstNodeExpression : AstNode
	{
		public AstNodeExpression()
		{
		}

	}

	public class AstNodeExpressionLiteral : AstNodeExpression
	{
		public object Value;

		public AstNodeExpressionLiteral(object Value = null)
		{
			this.Value = Value;
		}
	}

	public class AstNodeExpressionTypeLiteral : AstNodeExpression
	{
		public AstNodeType AstNodeType;
		public object Value;

		public AstNodeExpressionTypeLiteral(AstNodeType AstNodeType, object Value = null)
		{
			this.AstNodeType = AstNodeType;
			this.Value = Value;
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
	}


	public class AstNodeExpressionBinaryOperation : AstNodeExpression
	{
		public AstNode Destination;
		public string Operation;
		public AstNode Type;
		public AstNode Left;
		public AstNode Right;

		public AstNodeExpressionBinaryOperation()
		{
		}

		public AstNodeExpressionBinaryOperation(AstNode Destination, string Operation, AstNode Type, AstNode Left, AstNode Right)
		{
			this.Destination = Destination;
			this.Operation = Operation;
			this.Type = Type;
			this.Left = Left;
			this.Right = Right;
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
		public AstNode ReturnType;
		public string FunctionName;
		public AstNode ParameterList;
		public AstNode ExtraInfo;
		public AstNode Statements;

		public AstDefineFunction(AstNode ReturnType, string FunctionName, AstNode ParameterList, AstNode ExtraInfo, AstNode Statements)
		{
			this.ReturnType = ReturnType;
			this.FunctionName = FunctionName;
			this.ParameterList = ParameterList;
			this.ExtraInfo = ExtraInfo;
			this.Statements = Statements;
		}
	}

	public class AstParameter : AstNode
	{
		public AstNode ParameterType;
		public AstNode ParameterAttributes;
		public string ParameterName;

		public AstParameter(AstNode ParameterType, AstNode ParameterAttributes, string ParameterName)
		{
			this.ParameterType = ParameterType;
			this.ParameterAttributes = ParameterAttributes;
			this.ParameterName = ParameterName;
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
		public AstNode CallType;
		public string FunctionName;
		public AstNode Parameters;
		public AstNode Attributes;

		public AstNodeFunctionCall(string DestinationName, AstNode Modifiers, AstNode CallType, string FunctionName, AstNode Parameters, AstNode Attributes)
		{
			this.DestinationName = DestinationName;
			this.Modifiers = Modifiers;
			this.CallType = CallType;
			this.FunctionName = FunctionName;
			this.Parameters = Parameters;
			this.Attributes = Attributes;
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
	}

	public class AstDeclareFunction : AstNode
	{
		public AstNodeType ReturnType;
		public string FunctionName;
		public AstNode ParameterList;
		public AstNode FunctionAttributeList;

		public AstDeclareFunction(AstNodeType ReturnType, string FunctionName, AstNode ParameterList, AstNode FunctionAttributeList)
		{
			this.ReturnType = ReturnType;
			this.FunctionName = FunctionName;
			this.ParameterList = ParameterList;
			this.FunctionAttributeList = FunctionAttributeList;
		}
	}

	public class AstNodeDeclareParameter : AstNode
	{
		public AstNodeType astNodeType;
		public AstNodeModifiers astNodeModifiers;

		public AstNodeDeclareParameter(AstNodeType astNodeType, AstNodeModifiers astNodeModifiers)
		{
			// TODO: Complete member initialization
			this.astNodeType = astNodeType;
			this.astNodeModifiers = astNodeModifiers;
		}
	}
}
