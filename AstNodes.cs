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

	public class AstNodeType : AstNode
	{
		public string TypeName;
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

		public AstNodeExpressionLiteral()
		{
		}

		public AstNodeExpressionLiteral(object Value = null)
		{
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

}
