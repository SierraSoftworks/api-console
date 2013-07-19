using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Irony.Parsing;
using System.Dynamic;
using System.Text.RegularExpressions;

namespace API_Console.AST
{
    [Language("API Console", "1.0", "Simple REPL for interacting with web services")]
    public class Language : Grammar
    {
        public Language()
            : this(new ExpandoObject(), new Dictionary<string, object>())
        {
            (BuiltInFunctions as IDictionary<string, object>).Add("exit", null);
            (BuiltInFunctions as IDictionary<string, object>).Add("quit", null);
            (BuiltInFunctions as IDictionary<string, object>).Add("help", null);
        }

        public Language(ExpandoObject builtInFunctions, IDictionary<string,object> functionProviders)
        {
            BuiltInFunctions = builtInFunctions;
            FunctionProviders = functionProviders;
            
            UsesNewLine = false;
            
            var identifier = new IdentifierTerminal("Identifier", IdOptions.IsNotKeyword);

            var p_path = new RegexBasedTerminal("Path", @"(?<=\s|^|"")[^""=\s]+(?:(?<!(?:\s|^)[\w\d]+)=\S+)?(?=\s|$)");

            var p_string = new StringLiteral("String", "\"", StringOptions.AllowsLineBreak | StringOptions.AllowsAllEscapes | StringOptions.AllowsDoubledQuote, Generators.VisitString);
            var p_double = new NumberLiteral("Double", NumberOptions.AllowSign | NumberOptions.AllowStartEndDot, Generators.VisitDouble);
            var p_kvp = new NonTerminal("KeyValuePair", Generators.VisitKeyValuePair);
            var parameter = new NonTerminal("Parameter");
            var parameters = new NonTerminal("Parameters", Generators.VisitParameters);

            var functionCall = new NonTerminal("Function Call", Generators.VisitFunctionCall);

            var function = new NonTerminal("Function");
            var externalFunction = new NonTerminal("External Function", Generators.VisitExternalFunction);
            var builtInFunction = new NonTerminal("Built-in Function", Generators.VisitBuiltInFunction);

            var expression = new NonTerminal("Expression");
            var expressions = new NonTerminal("Expressions", Generators.VisitExpressions);

            p_string.EscapeChar = '\\';
            p_string.AddStartEnd("'", StringOptions.AllowsAllEscapes);

            p_kvp.Rule = identifier + "=" + parameter;
            p_kvp.SetFlag(TermFlags.IsConstant);

            p_path.SetFlag(TermFlags.IsLiteral);
            p_path.AstConfig.NodeCreator = Generators.VisitString;


            parameter.Rule = p_kvp | p_path | p_double | p_string;
            MarkTransient(parameter);
            parameters.Rule =  MakeStarRule(parameters, parameter);

            functionCall.Rule = function + parameters;
            function.Rule = builtInFunction | externalFunction;
            MarkTransient(function);

            identifier.SetFlag(TermFlags.IsMemberSelect);
            builtInFunction.SetFlag(TermFlags.IsKeyword);
            
            foreach (var key in (BuiltInFunctions as IDictionary<string, object>).Keys)
            {
                if (builtInFunction.Rule == null)
                    builtInFunction.Rule = ToTerm(key);
                else
                    builtInFunction.Rule |= key;
            }
            if (builtInFunction.Rule == null)
                builtInFunction.Rule = ToTerm("exit") | "quit" | "help";

            externalFunction.Rule = identifier + NewLineStar + identifier;

            expression.Rule = functionCall;
            MarkTransient(expression);
            expressions.Rule = MakeListRule(expressions, NewLinePlus, expression, TermListOptions.AllowTrailingDelimiter);
                        
            #region AST Ignores

            identifier.SetFlag(TermFlags.NoAstNode);
            
            #endregion
            
            Root = expressions;
            LanguageFlags |= Irony.Parsing.LanguageFlags.CreateAst | Irony.Parsing.LanguageFlags.SupportsCommandLine;
        }
        
        public ExpandoObject BuiltInFunctions
        { get; private set; }

        public IDictionary<string, object> FunctionProviders
        { get; private set; }

        public IDictionary<string, object> CompiledCache
        { get; private set; }
    }
}
