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
		public AstNode Build(ParseTreeNode ParseNode)
		{
			while (ParseNode != null && ParseNode.ChildNodes != null && ParseNode.ChildNodes.Count == 1)
			{
				ParseNode = ParseNode.ChildNodes[0];
			}
			//Console.WriteLine("{0}", ParseNode);
			var TermName = ParseNode.Term.Name;
			switch (TermName)
			{
				case "BINARY_OP": return BuildBinaryOp(ParseNode);
				case "i32":
				case "i8":
					return new AstNodeType() { TypeName = TermName };
				case "NUMBER": return new AstNodeExpressionLiteral(ParseNode.Token.Value);
				case "IDENTIFIER": return new AstNodeExpressionIdentifier(ParseNode.Token.ValueString);
			}
			throw (new NotImplementedException("Don't know how to handle : " + TermName));
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
			Children.ReadExpectString("=");
			var Operation = Children.ReadString();
			var Type = Children.ReadAstNode();
			var Left = Children.ReadAstNode();
			Children.ReadExpectString(",");
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

			public AstNode ReadAstNode()
			{
				return LlvmAstBuilder.Build(ParseNode.ChildNodes[ChildIndex++]);
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
