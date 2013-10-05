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

			var TYPE_ELLIPSIS = ToTerm("...");
			var TYPE_INTEGER = ToTerm("i1") | "i2" | "i4" | "i8" | "i16" | "i32" | "i64" | "i128";
			var TYPE_FLOATING_POINT = ToTerm("half") | "float" | "double" | "x86_fp80" | "fp128" | "ppc_fp128";

			NonTerminal TYPE = new NonTerminal("TYPE");
			var TYPE_LIST = CreateZeroOrMoreList("TYPE_LIST", ToTerm(","), TYPE);
			TYPE.Rule =
				  TYPE_INTEGER
				| TYPE_FLOATING_POINT
				| TYPE_ELLIPSIS
				| (TYPE + ToTerm("*"))
				| (ToTerm("<") + NUMBER + ToTerm("x") + TYPE + ToTerm(">"))
				| (ToTerm("[") + NUMBER + ToTerm("x") + TYPE + ToTerm("]"))
				| (ToTerm("{") + TYPE_LIST + ToTerm("}"))
				| (TYPE + ToTerm("(") + TYPE_LIST + ToTerm(")"))
			;

			var ConstantTerm = BooleanTerm | NullTerm | NUMBER;

			var ValueTerm = IDENTIFIER | ConstantTerm;

			var LinkageType = ToTerm("private")
				| "linker_private"
				| "linker_private_weak"
				| "internal"
				| "available_externally"
				| "linkonce"
				| "weak"
				| "common"
				| "appending"
				| "extern_weak"
				| "linkonce_odr" | "weak_odr"
				| "linkonce_odr_auto_hide"
				| "external"
				| "dllimport"
				| "dllexport"
			;
			var VisibilityStyles = ToTerm("default")
				| "hidden"
				| "protected"
			;
			var CallingConvetion = ToTerm("ccc")
				| "fastcc"
				| "coldcc"
				| "cc10"
				| "cc11"
				| "ccn"
			;
			var FunctionAttributes = ToTerm("alignstack")
				| "alwaysinline"
				| "buildin"
				| "cold"
				| "inlinehint"
				| "minsize"
				| "naked"
				| "nobuiltin"
				| "noduplicate"
				| "noimplicitfloat"
				| "noinline"
				| "nonlazybind"
				| "noredzone"
				| "noreturn"
				| "nounwind"
				| "optnone"
				| "optsize"
				| "readnone"
				| "readonly"
				| "returns_twice"
				| "sanitize_address"
				| "sanitize_memory"
				| "sanitize_thread"
				| "ssp"
				| "sspreq"
				| "sspstrong"
				| "uwtable"
			;

			var PARAMETER_ATTRIBUTE = new NonTerminal("PARAMETER_ATTRIBUTE", ToTerm("zeroext")
				| "signext"
				| "inreg"
				| "byval"
				| "sret"
				| "noalias"
				| "nocapture"
				| "nest"
				| "returned"
			);
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

			var BinaryTerm = IntegerBinaryTerm | IntegerBinaryTermBitwise | FloatBinaryTerm;

			var TARGET = new NonTerminal("TARGET", 
				ToTerm("target") + (ToTerm("datalayout") | ToTerm("triple")) + "=" + STRING
			);

			var BinaryOpModifier = ToTerm("nuw")
				| "nsw"
			;
			var BinaryOpModifierList = CreateZeroOrMoreList("BinaryOpModifierList", BinaryOpModifier);

			var BINARY_OP = new NonTerminal("BINARY_OP",
				IDENTIFIER + "=" + BinaryTerm + BinaryOpModifierList + TYPE + ValueTerm + "," + ValueTerm
			);

			var CONSTANT_EXPRESSION = new NonTerminal("CALL_PARAMETER");
			var CONSTANT_EXPRESSION_LIST = CreateZeroOrMoreList("CONSTANT_EXPRESSION_LIST", ToTerm(","), CONSTANT_EXPRESSION);

			var ZST_TO_TYPE = ToTerm("trunc")
				| "zext"
				| "sext"
				| "fptrunc"
				| "fpext"
				| "fptoui"
				| "fptosi"
				| "uitofp"
				| "sitofp"
				| "ptrtoint"
				| "inttoptr"
				| "bitcast"
			;

			CONSTANT_EXPRESSION.Rule =
				(TYPE + IDENTIFIER)
				| (TYPE + NUMBER)
				| (TYPE + ZST_TO_TYPE + CONSTANT_EXPRESSION + ToTerm("to") + TYPE)
				| (TYPE + ToTerm("getelementptr") + ToTerm("inbounds") + ToTerm("(") + CONSTANT_EXPRESSION_LIST + ToTerm(")"))
			;

			var EXPRESSION = CONSTANT_EXPRESSION;

			var CALL_PARAMETER_WITH_TYPE =
				EXPRESSION
			;

			var CALL_PARAMETER_WITH_TYPE_LIST = CreateZeroOrMoreList("CALL_PARAMETER_LIST", ToTerm(","), CALL_PARAMETER_WITH_TYPE);

			var FUNCTION_CALL = new NonTerminal("CALL",
				IDENTIFIER + ToTerm("=") + ToTerm("tail").Q() + ToTerm("call")
				+ TYPE
				+ IDENTIFIER
				+ "("
				+ CALL_PARAMETER_WITH_TYPE_LIST
				+ ")"
				+ FunctionAttributes
			);

			var LABEL = new NonTerminal("LABEL",
				LABELNAME_TERMINAL + ":"
			);

			var RETURN = new NonTerminal("RETURN",
				(ToTerm("ret") + EXPRESSION)
				| (ToTerm("ret") + ToTerm("void"))
			);

			var STATEMENT = new NonTerminal("STATEMENT",
				BINARY_OP
				| FUNCTION_CALL
				| RETURN
				| LABEL
			);

			//this.LanguageFlags = LanguageFlags.NewLineBeforeEOF | LanguageFlags.CreateAst;

			var STATEMENT_LIST = CreateZeroOrMoreList("STATEMENTS", null, STATEMENT);

			var DEFINE_FUNCTION =
				ToTerm("define")
				+ TYPE
				+ IDENTIFIER
				+ ToTerm("(")
				+ DEFINE_PARAMETER_LIST
				+ ToTerm(")")
				+ FunctionAttributes
				+ ToTerm("{")
				+ STATEMENT_LIST
				+ ToTerm("}")
			;

			var DECLARE_FUNCTION =
				ToTerm("declare")
				+ TYPE
				+ IDENTIFIER
				+ ToTerm("(")
				+ DECLARE_PARAMETER_LIST
				+ ToTerm(")")
				+ FunctionAttributes
			;

			var PROGRAM_DECLARATION =
				TARGET
				| DEFINE_FUNCTION
				| DECLARE_FUNCTION
			;

			var PROGRAM_DECLARATION_LIST = CreateZeroOrMoreList("PROGRAM_DECLARATIONS", null, PROGRAM_DECLARATION);

			//PROGRAM.Rule = MakePlusRule(STATEMENT, NewLine);
			//PROGRAM.Rule = STATEMENTS;
			var PROGRAM = PROGRAM_DECLARATION_LIST;

			Root = PROGRAM;

			var CommentLine = new CommentTerminal("CommentLine", ";", "\r\n", "\r", "\n", "\u2085", "\u2028", "\u2029");

			var COMMENT = new NonTerminal("COMMENT");
			COMMENT.Rule = CommentLine;

			NonGrammarTerminals.Add(CommentLine);

			MarkPunctuation("(", ")", ",");
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
