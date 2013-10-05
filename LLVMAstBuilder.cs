using Irony.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace llvm_to_msil.Nodes
{
	public class LlvmAstBuilder
	{
		public TAstNode Build<TAstNode>(ParseTreeNode ParseNode) where TAstNode : AstNode
		{
			return (TAstNode)Build(ParseNode);
		}
		public AstNode Build(ParseTreeNode ParseNode)
		{
			/*
			while (ParseNode != null && ParseNode.ChildNodes != null && ParseNode.ChildNodes.Count == 1)
			{
				ParseNode = ParseNode.ChildNodes[0];
			}
			*/
			//Console.WriteLine("{0}", ParseNode);
			var TermName = ParseNode.Term.Name;
			switch (TermName)
			{
				case "BINARY_OP": return BuildBinaryOp(ParseNode);
				case "i8":
				case "i16":
				case "i32":
					return new AstNodeTypeBase(TermName);
				case "NUMBER": return new AstNodeExpressionLiteral(ParseNode.Token.Value);
				case "IDENTIFIER": return new AstNodeExpressionIdentifier(ParseNode.Token.ValueString);
				case "TARGET": return BuildAstTarget(ParseNode);
				case "DEFINE_FUNCTION": return BuildAstDefineFunction(ParseNode);
				case "DECLARE_FUNCTION": return BuildAstDeclareFunction(ParseNode);
				case "DECLARE_GLOBAL": return BuildAstDeclareGlobal(ParseNode);
				case "PROGRAM_DECLARATION_LIST": return BuildAstContainer<AstNode>(ParseNode);

				case "STATEMENT_LIST": return BuildAstContainer<AstNode>(ParseNode);
				case "STATEMENT": return _ReturnSingleChild(ParseNode);
				case "LABEL": return new AstNodeLabel(GetStringTermValue(ParseNode));
				case "VALUE_TERM": return new AstNodeExpressionLiteral(GetStringTermValue(ParseNode));

				case "DEFINE_PARAMETER_LIST": return BuildAstContainer<AstNode>(ParseNode);
				case "DEFINE_PARAMETER": return BuildAstDefineParameter(ParseNode);

				case "CALL": return BuildAstCall(ParseNode);
				case "CONSTANT_EXPRESSION_LIST": return BuildAstContainer<AstNode>(ParseNode);
				case "CONSTANT_EXPRESSION": return _ReturnSingleChild(ParseNode);
				case "CONSTANT_EXPRESSION_GETELEMENTPTR": return BuildConstantExpressionGetElementPtr(ParseNode);
				case "CONSTANT_EXPRESSION_IDENTIFIER": return BuildConstantExpressionIdentifier(ParseNode);
				case "CALL_PARAMETER_WITH_TYPE_LIST": return BuildAstContainer<AstNode>(ParseNode);

				case "DECLARE_GLOBAL_MODIFIERS": return BuildAstNodeModifiers(ParseNode);
				case "PARAMETER_ATTRIBUTE_LIST": return BuildAstNodeModifiers(ParseNode);
				case "FUNCTION_ATTRIBUTE_LIST": return BuildAstNodeModifiers(ParseNode);
				case "STATEMENT_BINARY_OP_MODIFIER_LIST": return BuildAstNodeModifiers(ParseNode);
				case "CALL_MODIFIER_LIST": return BuildAstNodeModifiers(ParseNode);

				case "RETURN": return new AstNodeReturn(Build(ParseNode.ChildNodes[1]));

				case "DECLARE_PARAMETER_LIST": return BuildAstContainer<AstNode>(ParseNode);
				case "DECLARE_PARAMETER": return new AstNodeDeclareParameter(Build<AstNodeType>(ParseNode.ChildNodes[0]), BuildAstNodeModifiers(ParseNode.ChildNodes[1]));

				case "TYPE": return _ReturnSingleChild(ParseNode);
				case "TYPE_ARRAY": return BuildAstTypeArray(ParseNode);
				case "TYPE_ELLIPSIS": return new AstNodeTypeEllipsis();
				case "TYPE_INTEGER": return _ReturnSingleChild(ParseNode);
				case "TYPE_POINTER": return new AstNodeTypePointer(Build<AstNodeType>(ParseNode.ChildNodes[0]));
				case "TYPE_FUNCTION": return new AstNodeTypeFunction(Build<AstNodeType>(ParseNode.ChildNodes[0]), Build(ParseNode.ChildNodes[0]));
				case "LITERAL": return _ReturnSingleChild(ParseNode);
				case "LITERAL_STRING": return BuildAstLiteralString(ParseNode);
				case "PROGRAM_DECLARATION": return BuildAstContainer<AstNode>(ParseNode);
			}
			throw (new NotImplementedException("Don't know how to handle : " + TermName));
		}

		private AstNode BuildConstantExpressionIdentifier(ParseTreeNode ParseNode)
		{
			var Children = new ChildReader(this, ParseNode);
			var Type = Children.ReadAstNode<AstNodeType>();
			var Value = Children.ReadAstNode();
			return new AstNodeExpressionTypeLiteral(Type, Value);
		}

		private AstNode BuildConstantExpressionGetElementPtr(ParseTreeNode ParseNode)
		{
			var Children = new ChildReader(this, ParseNode);
			var Type = Children.ReadAstNode();
			Children.ReadExpectString("getelementptr");
			Children.ReadExpectString("inbounds");
			var Value = Children.ReadAstNode();
			var IndexList = Children.ReadAstNode();
			return new AstNodeGetElementPtr(Type, Value, IndexList);
		}

		private AstNode BuildAstCall(ParseTreeNode ParseNode)
		{
			var Children = new ChildReader(this, ParseNode);
			var DestinationName = Children.ReadString();
			var Modifiers = Children.ReadAstNode();
			Children.ReadExpectString("call");
			var CallType = Children.ReadAstNode();
			var FunctionName = Children.ReadString();
			var Parameters = Children.ReadAstNode();
			var Attributes = Children.ReadAstNode();
			return new AstNodeFunctionCall(DestinationName, Modifiers, CallType, FunctionName, Parameters, Attributes);
		}

		private AstNode BuildAstDefineParameter(ParseTreeNode ParseNode)
		{
			var Children = new ChildReader(this, ParseNode);
			var ParameterType = Children.ReadAstNode();
			var ParameterAttributes = Children.ReadAstNode();
			var ParameterName = Children.ReadString();
			return new AstParameter(ParameterType, ParameterAttributes, ParameterName);
		}

		private AstNode _ReturnSingleChild(ParseTreeNode ParseNode)
		{
			if (ParseNode.ChildNodes.Count != 1) throw(new Exception("Expected one child"));
			return Build(ParseNode.ChildNodes[0]);
		}

		private AstNode BuildAstDeclareFunction(ParseTreeNode ParseNode)
		{
			var Children = new ChildReader(this, ParseNode);
			Children.ReadExpectString("declare");
			var ReturnType = Children.ReadAstNode<AstNodeType>();
			var FunctionName = Children.ReadString();
			var ParameterList = Children.ReadAstNode();
			var FunctionAttributeList = Children.ReadAstNode();
			return new AstDeclareFunction(ReturnType, FunctionName, ParameterList, FunctionAttributeList);
		}

		private AstNode BuildAstDefineFunction(ParseTreeNode ParseNode)
		{
			var Children = new ChildReader(this, ParseNode);
			Children.ReadExpectString("define");
			var ReturnType = Children.ReadAstNode<AstNodeType>();
			var FunctionName = Children.ReadString();
			var ParameterList = Children.ReadAstNode();
			var ExtraInfo = Children.ReadAstNode();
			var Statements = Children.ReadAstNode();
			return new AstDefineFunction(ReturnType, FunctionName, ParameterList, ExtraInfo, Statements);
		}

		private AstNodeExpressionLiteral BuildAstLiteralString(ParseTreeNode ParseNode)
		{
			return new AstNodeExpressionLiteral(ParseNode.ChildNodes[1].Token.Value);
		}

		private AstNodeType BuildAstTypeArray(ParseTreeNode ParseNode)
		{
			var Children = new ChildReader(this, ParseNode);
			var Count = Children.ReadString();
			Children.ReadExpectString("x");
			var ElementType = Children.ReadAstNode();
			return new AstNodeTypeArray(Count, (AstNodeType)ElementType);
		}

		private AstNodeModifiers BuildAstNodeModifiers(ParseTreeNode ParseNode)
		{
			return new AstNodeModifiers(ParseNode.ChildNodes.Select(Node => GetStringTermValue(Node)).ToArray());
		}

		private AstNode BuildAstDeclareGlobal(ParseTreeNode ParseNode)
		{
			var Children = new ChildReader(this, ParseNode);
			var Name = Children.ReadString();
			var Modifiers = Children.ReadAstNode();
			var ConstantOrVariable = Children.ReadString();
			var Type = Children.ReadAstNode();
			var Value = Children.ReadAstNode();
			//return new AstNodeDeclareGlobal();
			return new AstNodeDeclareGlobal(Name, Modifiers, ConstantOrVariable, Type, Value);
		}

		private AstNodeTarget BuildAstTarget(ParseTreeNode ParseNode)
		{
			var Children = new ChildReader(this, ParseNode);
			Children.ReadExpectString("target");
			var Type = Children.ReadString();
			var Value = (String)(Children.ReadParserNode().Token.Value);
			return new AstNodeTarget(Type, Value);
		}

		private AstNodeContainer<TAstNode> BuildAstContainer<TAstNode>(ParseTreeNode ParseNode) where TAstNode : AstNode
		{
			return new AstNodeContainer<TAstNode>(ParseNode.ChildNodes.Select(Item => Build(Item)).Cast<TAstNode>());
		}

		private string GetStringTermValue(ParseTreeNode ParseNode)
		{
			while (ParseNode != null && ParseNode.ChildNodes != null && ParseNode.ChildNodes.Count == 1)
			{
				ParseNode = ParseNode.ChildNodes[0];
			}

			return ParseNode.Term.Name;
		}

		private AstNode BuildIgnore(ParseTreeNode ParseNode)
		{
			if (ParseNode.ChildNodes.Count != 1) throw (new Exception("Can't ignore a node with several childs"));
			return Build(ParseNode.ChildNodes[0]);
		}

		private AstNode BuildBinaryOp(ParseTreeNode ParseNode)
		{
			var Children = new ChildReader(this, ParseNode);
			var Destination = Children.ReadAstNode();
			var Operation = Children.ReadString();
			var Type = Children.ReadAstNode();
			var Left = Children.ReadAstNode();
			var Right = Children.ReadAstNode();
			return new AstNodeExpressionBinaryOperation(Destination, Operation, Type, Left, Right);
		}

		public class ChildReader
		{
			int ChildIndex = 0;
			ParseTreeNode ParseNode;
			LlvmAstBuilder LlvmAstBuilder;

			public ChildReader(LlvmAstBuilder LlvmAstBuilder, ParseTreeNode ParseNode)
			{
				this.LlvmAstBuilder = LlvmAstBuilder;
				this.ParseNode = ParseNode;
			}

			public ParseTreeNode ReadParserNode()
			{
				return ParseNode.ChildNodes[ChildIndex++];
			}

			public AstNode ReadAstNode()
			{
				return LlvmAstBuilder.Build(ReadParserNode());
			}

			public TAstNode ReadAstNode<TAstNode>() where TAstNode: AstNode
			{
				return LlvmAstBuilder.Build<TAstNode>(ReadParserNode());
			}

			public String ReadString()
			{
				return LlvmAstBuilder.GetStringTermValue(ParseNode.ChildNodes[ChildIndex++]);
			}

			public void ReadExpectString(string ExpectedString)
			{
				var String = ReadString();
				if (String != ExpectedString)
				{
					throw (new InvalidOperationException(String.Format("Found '{0}' but expected '{1}'", String, ExpectedString)));
				}
			}
		}
	}
}
