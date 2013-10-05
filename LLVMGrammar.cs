using Irony.Ast;
using Irony.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace llvm_to_msil
{
	/// <summary>
	/// 
	/// </summary>
	/// <see cref="http://llvm.org/docs/LangRef.html"/>
	[Language("LLVM", "0.01", "LLVM assembler grammar")]
	public class LLVMGrammar : Grammar
	{
		private readonly Terminal NUMBER = new NumberLiteral("NUMBER", NumberOptions.AllowStartEndDot);
		private readonly Terminal STRING = new StringLiteral("STRING", "\"");
		private readonly Terminal IDENTIFIER = new LLVMIdentifier("IDENTIFIER");
		private readonly Terminal LABELNAME_TERMINAL = new IdentifierTerminal("LABEL", IdOptions.None);

		public LLVMGrammar() : base(caseSensitive: true)
		{
			GrammarComments = "LLVM 3.3";

			var BooleanTerm = ToTerm("true") | "false";
			var NullTerm = ToTerm("null");

			var TYPE = new NonTerminal("TYPE");
			var TYPE_LIST = CreateZeroOrMoreList("TYPE_LIST", ToTerm(","), TYPE);

			var TYPE_ELLIPSIS = new NonTerminal("TYPE_ELLIPSIS", ToTerm("..."));
			var TYPE_INTEGER = new NonTerminal("TYPE_INTEGER", ToTerm("i1") | "i2" | "i4" | "i8" | "i16" | "i32" | "i64" | "i128");
			var TYPE_FLOATING_POINT = new NonTerminal("TYPE_FLOATING_POINT", ToTerm("half") | "float" | "double" | "x86_fp80" | "fp128" | "ppc_fp128");
			var TYPE_POINTER = new NonTerminal("TYPE_POINTER", (TYPE + ToTerm("*")));
			var TYPE_VECTOR = new NonTerminal("TYPE_VECTOR", (ToTerm("<") + NUMBER + ToTerm("x") + TYPE + ToTerm(">")));
			var TYPE_ARRAY = new NonTerminal("TYPE_ARRAY", (ToTerm("[") + NUMBER + ToTerm("x") + TYPE + ToTerm("]")));
			var TYPE_STRUCT = new NonTerminal("TYPE_STRUCT", (ToTerm("{") + TYPE_LIST + ToTerm("}")));
			var TYPE_FUNCTION = new NonTerminal("TYPE_FUNCTION", (TYPE + ToTerm("(") + TYPE_LIST + ToTerm(")")));
			TYPE.Rule =
				  TYPE_INTEGER
				| TYPE_FLOATING_POINT
				| TYPE_ELLIPSIS
				| TYPE_POINTER
				| TYPE_VECTOR
				| TYPE_ARRAY
				| TYPE_STRUCT
				| TYPE_FUNCTION
			;

			var ConstantTerm = BooleanTerm | NullTerm | NUMBER;

			var VALUE_TERM = new NonTerminal("VALUE_TERM",
				IDENTIFIER | ConstantTerm
			);

			var LinkageType =
				ToTerm("private") | "linker_private" | "linker_private_weak" | "internal" | "available_externally" | "linkonce" | "weak"
				| "common" | "appending" | "extern_weak" | "linkonce_odr" | "weak_odr" | "linkonce_odr_auto_hide" | "external" | "dllimport" | "dllexport"
			;
			var VisibilityStyles = ToTerm("default") | "hidden" | "protected";
			var CallingConvetion = ToTerm("ccc") | "fastcc" | "coldcc" | "cc10" | "cc11" | "ccn";
			var FUNCTION_ATTRIBUTE_LIST = new NonTerminal("FUNCTION_ATTRIBUTE_LIST",
				ToTerm("alignstack") | "alwaysinline" | "buildin" | "cold" | "inlinehint" | "minsize" | "naked" | "nobuiltin" | "noduplicate"
				| "noimplicitfloat" | "noinline" | "nonlazybind" | "noredzone" | "noreturn" | "nounwind" | "optnone" | "optsize" | "readnone"
				| "readonly" | "returns_twice" | "sanitize_address" | "sanitize_memory" | "sanitize_thread" | "ssp" | "sspreq" | "sspstrong" | "uwtable"
			);




			var PARAMETER_ATTRIBUTE = new NonTerminal("PARAMETER_ATTRIBUTE", ToTerm("zeroext") | "signext" | "inreg" | "byval" | "sret" | "noalias" | "nocapture" | "nest" | "returned");
			var BinaryOpModifier = ToTerm("nuw") | "nsw" | "nnan" | "ninf" | "nsz" | "arcp" | "fast";
			var ZST_TO_TYPE = ToTerm("trunc") | "zext" | "sext" | "fptrunc" | "fpext" | "fptoui" | "fptosi" | "uitofp" | "sitofp" | "ptrtoint" | "inttoptr" | "bitcast";

			var PARAMETER_ATTRIBUTE_LIST = CreateZeroOrMoreList("PARAMETER_ATTRIBUTE_LIST", PARAMETER_ATTRIBUTE);

			var DEFINE_PARAMETER = new NonTerminal("DEFINE_PARAMETER", 
				TYPE + PARAMETER_ATTRIBUTE_LIST + IDENTIFIER
			);

			var DECLARE_PARAMETER = new NonTerminal("DECLARE_PARAMETER",
				TYPE + PARAMETER_ATTRIBUTE_LIST
			);

			var DEFINE_PARAMETER_LIST = CreateZeroOrMoreList("DEFINE_PARAMETER_LIST", ToTerm(","), DEFINE_PARAMETER);
			var DECLARE_PARAMETER_LIST = CreateZeroOrMoreList("DECLARE_PARAMETER_LIST", ToTerm(","), DECLARE_PARAMETER);

			//define [linkage] [visibility] [cconv] [ret attrs] <ResultType> @<FunctionName> ([argument list]) [fn Attrs] [section "name"] [align N] [gc] [prefix Constant] { ... }

			//var Add = ToTerm("add");

			var IntegerBinaryTerm = ToTerm("add") | "sub" | "mul" | "udiv" | "sdiv" | "urem" | "srem" | "shl";
			var IntegerBinaryTermBitwise = ToTerm("shl") | "lshr" | "ashr" | "and" | "or" | "xor";
			var FloatBinaryTerm = ToTerm("fadd") | "fsub" | "fmul" | "fdiv" | "frem";

			var BINARY_TERM = new NonTerminal("BINARY_TERM",
				IntegerBinaryTerm | IntegerBinaryTermBitwise | FloatBinaryTerm
			);

			var TARGET = new NonTerminal("TARGET", 
				ToTerm("target") + (ToTerm("datalayout") | ToTerm("triple")) + "=" + STRING
			);

			var STATEMENT_BINARY_OP_MODIFIER_LIST = CreateZeroOrMoreList("STATEMENT_BINARY_OP_MODIFIER_LIST", BinaryOpModifier);

			var STATEMENT_BINARY_OP = new NonTerminal("BINARY_OP",
				IDENTIFIER + "=" + BINARY_TERM + STATEMENT_BINARY_OP_MODIFIER_LIST + TYPE + VALUE_TERM + "," + VALUE_TERM
			);

			var CONSTANT_EXPRESSION = new NonTerminal("CONSTANT_EXPRESSION");
			var CONSTANT_EXPRESSION_LIST = CreateZeroOrMoreList("CONSTANT_EXPRESSION_LIST", ToTerm(","), CONSTANT_EXPRESSION);
			var CONSTANT_EXPRESSION_IDENTIFIER = new NonTerminal("CONSTANT_EXPRESSION_IDENTIFIER", (TYPE + IDENTIFIER));
			var CONSTANT_EXPRESSION_NUMBER = new NonTerminal("CONSTANT_EXPRESSION_IDENTIFIER", (TYPE + NUMBER));
			var CONSTANT_EXPRESSION_CST_TO = new NonTerminal("CONSTANT_EXPRESSION_CST_TO", (TYPE + ZST_TO_TYPE + CONSTANT_EXPRESSION + ToTerm("to") + TYPE));
			var CONSTANT_EXPRESSION_GETELEMENTPTR = new NonTerminal("CONSTANT_EXPRESSION_GETELEMENTPTR", (TYPE + ToTerm("getelementptr") + ToTerm("inbounds") + ToTerm("(") + CONSTANT_EXPRESSION + "," + CONSTANT_EXPRESSION_LIST + ToTerm(")")));

			CONSTANT_EXPRESSION.Rule =
				CONSTANT_EXPRESSION_IDENTIFIER
				| CONSTANT_EXPRESSION_NUMBER
				| CONSTANT_EXPRESSION_CST_TO
				| CONSTANT_EXPRESSION_GETELEMENTPTR
			;

			var EXPRESSION = CONSTANT_EXPRESSION;

			var CALL_PARAMETER_WITH_TYPE =
				EXPRESSION
			;

			var CALL_PARAMETER_WITH_TYPE_LIST = CreateZeroOrMoreList("CALL_PARAMETER_WITH_TYPE_LIST", ToTerm(","), CALL_PARAMETER_WITH_TYPE);

			var CALL_MODIFIER = new NonTerminal("CALL_MODIFIERS",
				ToTerm("tail")
			);

			var CALL_MODIFIER_LIST = CreateZeroOrMoreList("CALL_MODIFIER_LIST", CALL_MODIFIER);

			var STATEMENT_FUNCTION_CALL = new NonTerminal("CALL",
				IDENTIFIER + ToTerm("=") + CALL_MODIFIER_LIST + ToTerm("call")
				+ TYPE
				+ IDENTIFIER
				+ "("
				+ CALL_PARAMETER_WITH_TYPE_LIST
				+ ")"
				+ FUNCTION_ATTRIBUTE_LIST
			);

			var STATEMENT_LABEL = new NonTerminal("LABEL",
				LABELNAME_TERMINAL + ":"
			);

			var STATEMENT_RETURN = new NonTerminal("RETURN",
				(ToTerm("ret") + EXPRESSION)
				| (ToTerm("ret") + ToTerm("void"))
			);

			var STATEMENT = new NonTerminal("STATEMENT",
				STATEMENT_BINARY_OP
				| STATEMENT_FUNCTION_CALL
				| STATEMENT_RETURN
				| STATEMENT_LABEL
			);

			//this.LanguageFlags = LanguageFlags.NewLineBeforeEOF | LanguageFlags.CreateAst;

			var STATEMENT_LIST = CreateZeroOrMoreList("STATEMENT_LIST", null, STATEMENT);

			var DEFINE_FUNCTION = new NonTerminal("DEFINE_FUNCTION",
				ToTerm("define")
				+ TYPE
				+ IDENTIFIER
				+ ToTerm("(")
				+ DEFINE_PARAMETER_LIST
				+ ToTerm(")")
				+ FUNCTION_ATTRIBUTE_LIST
				+ ToTerm("{")
				+ STATEMENT_LIST
				+ ToTerm("}")
			);

			var DECLARE_FUNCTION = new NonTerminal("DECLARE_FUNCTION",
				ToTerm("declare")
				+ TYPE
				+ IDENTIFIER
				+ ToTerm("(")
				+ DECLARE_PARAMETER_LIST
				+ ToTerm(")")
				+ FUNCTION_ATTRIBUTE_LIST
			);

			var unnamed_addr = ToTerm("unnamed_addr");

			var LITERAL_STRING = new NonTerminal("LITERAL_STRING",
				(ToTerm("c") + STRING)
			);

			var LITERAL_NUMBER = new NonTerminal("LITERAL_NUMBER",
				NUMBER
			);

			var LITERAL = new NonTerminal("LITERAL",
				LITERAL_STRING
				| LITERAL_NUMBER
			);

			var DECLARE_GLOBAL_MODIFIERS = new NonTerminal("DECLARE_GLOBAL_MODIFIERS",
				LinkageType + unnamed_addr.Q()
			);

			var DECLARE_GLOBAL = new NonTerminal("DECLARE_GLOBAL",
				IDENTIFIER
				+ ToTerm("=")
				+ DECLARE_GLOBAL_MODIFIERS
				+ ToTerm("constant")
				+ TYPE
				+ LITERAL
				+ (ToTerm(",") + ToTerm("align") + NUMBER).Q()
			);

			var PROGRAM_DECLARATION = new NonTerminal("PROGRAM_DECLARATION",
				TARGET
				| DEFINE_FUNCTION
				| DECLARE_FUNCTION
				| DECLARE_GLOBAL
			);

			var PROGRAM_DECLARATION_LIST = CreateZeroOrMoreList("PROGRAM_DECLARATION_LIST", PROGRAM_DECLARATION);

			//PROGRAM.Rule = MakePlusRule(STATEMENT, NewLine);
			//PROGRAM.Rule = STATEMENTS;
			var PROGRAM = PROGRAM_DECLARATION_LIST;

			Root = PROGRAM;

			var COMMENT_LINE = new CommentTerminal("COMMENT_LINE", ";", "\r\n", "\r", "\n", "\u2085", "\u2028", "\u2029");

			var COMMENT = new NonTerminal("COMMENT",
				COMMENT_LINE
			);

			NonGrammarTerminals.Add(COMMENT_LINE);

			MarkPunctuation("(", ")", ",", "=", "{", "}", "[", "]", "<", ">");
			MarkTransient(COMMENT);
		}

		private NonTerminal CreateZeroOrMoreList(string Name, BnfTerm Element)
		{
			var NonTerminal = new NonTerminal(Name);
			NonTerminal.Rule = MakeStarRule(NonTerminal, null, Element);
			return NonTerminal;
		}

		private NonTerminal CreateZeroOrMoreList(string Name, BnfTerm Delimiter, BnfTerm Element)
		{
			var NonTerminal = new NonTerminal(Name);
			NonTerminal.Rule = MakeStarRule(NonTerminal, Delimiter, Element);
			return NonTerminal;
		}
	}

	public class LLVMIdentifier : Terminal
	{
		private string p;

		public LLVMIdentifier(string p) : base (p)
		{
			// TODO: Complete member initialization
			this.p = p;
		}

		Regex MatchRegex = new Regex("[%@][a-zA-Z\\$\\._][a-zA-Z\\$\\._0-9]*");
		public override Token TryMatch(ParsingContext context, ISourceStream source)
		{
			var Match = MatchRegex.Match(source.Text, source.PreviewPosition);
			if (!Match.Success || Match.Index != source.PreviewPosition) return null;
			source.PreviewPosition += Match.Length;
			return source.CreateToken(this.OutputTerminal); 
		}
	}
}
