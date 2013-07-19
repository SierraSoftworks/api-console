using Irony.Ast;
using Irony.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Linq.Expressions;
using Expr = System.Linq.Expressions.Expression;
using API_Console.DLR;
using System.Dynamic;
using Irony;
using System.Reflection;

namespace API_Console.AST
{
    static class Generators
    {
        #region Binder Caching

        static Dictionary<CallInfo, API_Console.DLR.InvokeBinder> InvokeBinders = new Dictionary<CallInfo, DLR.InvokeBinder>();

        static API_Console.DLR.InvokeBinder GetInvokeBinder(CallInfo callInfo)
        {
            if (!InvokeBinders.ContainsKey(callInfo))
                InvokeBinders.Add(callInfo, new DLR.InvokeBinder(callInfo));
            return InvokeBinders[callInfo];
        }

        #endregion

        public static void VisitString(AstContext context, ParseTreeNode parseNode)
        {
            var value = parseNode.Term is StringLiteral ? (parseNode.Term as StringLiteral).TokenToString(parseNode.Token) : parseNode.Token.ValueString;
            if(value[0] == '"' || value[0] == '\'')
                value = value.Remove(value.Length - 1).Remove(0, 1); //Remove quotes
            value = value.Replace("\\\"", "\"")
                         .Replace("\\'","'");


            parseNode.AstNode = Expr.Constant(value, typeof(string));
        }

        public static void VisitRelativeUri(AstContext context, ParseTreeNode parseNode)
        {
            parseNode.AstNode = Expr.Constant(new Uri(parseNode.Token.Value.ToString(), UriKind.Relative), typeof(Uri));
        }

        public static void VisitAbsoluteUri(AstContext context, ParseTreeNode parseNode)
        {
            parseNode.AstNode = Expr.Constant(new Uri(parseNode.Token.Value.ToString(), UriKind.Absolute), typeof(Uri));
        }

        public static void VisitDouble(AstContext context, ParseTreeNode parseNode)
        {
            parseNode.AstNode = Expr.Convert(Expr.Constant(parseNode.Token.Value), typeof(double));
        }

        static readonly ConstructorInfo KVPConstructor = typeof(KeyValuePair<string, object>).GetConstructor(new[] { typeof(string), typeof(object) });

        public static void VisitKeyValuePair(AstContext context, ParseTreeNode parseNode)
        {
            parseNode.AstNode = Expr.New(
                KVPConstructor,
                Expr.Constant(parseNode.ChildNodes[0].Token.Value, typeof(string)),
                Expr.Convert(parseNode.ChildNodes[2].AstNode as Expr, typeof(object))
                );
        }

        public static void VisitParameters(AstContext context, ParseTreeNode parseNode)
        {
            parseNode.AstNode = parseNode.ChildNodes.Select(x => x.AstNode as Expr);
        }

        public static void VisitFunctionCall(AstContext context, ParseTreeNode parseNode)
        {
            var source = parseNode.ChildNodes[0].AstNode as Expr;
            var parameters = (parseNode.ChildNodes[1].AstNode as IEnumerable<Expr>) ?? new Expr[0];

            if (source == null)
            {
                return;
            }

            parseNode.AstNode = Expr.Dynamic(
                GetInvokeBinder(new CallInfo(parameters.Count())),
                typeof(object),
                parameters.ButFirst(source));
        }

        public static void VisitBuiltInFunction(AstContext context, ParseTreeNode parseNode)
        {
            var lang = context.Language.Grammar as Language;
            if (lang == null)            
                throw new InvalidOperationException("Invalid Parser Configuration: Bad Grammar");

            if (!(lang.BuiltInFunctions as IDictionary<string, object>).ContainsKey(parseNode.ChildNodes[0].Token.Value.ToString()))
            {
                context.AddMessage(ErrorLevel.Error, parseNode.Span.Location, "Call to unknown function '{0}'", parseNode.ChildNodes[0].Token.Value);
                return;
            }

            var target = (lang.BuiltInFunctions as IDictionary<string, object>)[parseNode.ChildNodes[0].Token.Value.ToString()];

            parseNode.AstNode = Expr.Constant(target, target == null ? typeof(object) : target.GetType());
        }

        public static void VisitExternalFunction(AstContext context, ParseTreeNode parseNode)
        {
            var lang = context.Language.Grammar as Language;
            if (lang == null)
                throw new InvalidOperationException("Invalid Parser Configuration: Bad Grammar");

            if (!lang.FunctionProviders.ContainsKey(parseNode.ChildNodes[0].Token.Value.ToString()))
            {
                context.AddMessage(ErrorLevel.Error, parseNode.ChildNodes[0].Span.Location, "Call to unknown function provider '{0}'", parseNode.ChildNodes[0].Token.Value);
                return;
            }

            var provider = lang.FunctionProviders[parseNode.ChildNodes[0].Token.Value.ToString()];

            if (!(provider as IDictionary<string, object>).ContainsKey(parseNode.ChildNodes[1].Token.Value.ToString()))
            {
                context.AddMessage(ErrorLevel.Error, parseNode.ChildNodes[0].Span.Location, "Call to unknown function '{0}' on provider '{1}'", parseNode.ChildNodes[1].Token.Value, parseNode.ChildNodes[0].Token.Value);
                return;
            }

            var target = (provider as IDictionary<string, object>)[parseNode.ChildNodes[1].Token.Value.ToString()];

            parseNode.AstNode = Expr.Constant(target, target == null ? typeof(object) : target.GetType());
        }

        public static void VisitExpressions(AstContext context, ParseTreeNode parseNode)
        {
            var expressions = parseNode.ChildNodes.Select(x => x.AstNode as Expr).Where(x => x != null);

            if (expressions.Any())
                parseNode.AstNode = Expr.Lambda(Expr.Block(expressions));

            else
            {
                context.AddMessage(Irony.ErrorLevel.Error, parseNode.Span.Location, "No code compiled in run. Review other errors for more information.");
                parseNode.AstNode = Expr.Lambda(Expr.Constant(null, typeof(object)));
            }

        }

    }
}
