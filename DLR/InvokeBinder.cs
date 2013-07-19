using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Utils;
using Expr = System.Linq.Expressions.Expression;
using Microsoft.Scripting.Actions;

namespace API_Console.DLR
{
    class InvokeBinder : System.Dynamic.InvokeBinder
    {

        public InvokeBinder(CallInfo callInfo)
            : base(callInfo)
        {

        }
        public override DynamicMetaObject FallbackInvoke(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
        {
            if (!target.HasValue || args.Any(a => !a.HasValue))
                return Defer(target, args);

            var restrictions = Extensions.MergeTypeRestrictions(target);
            
            restrictions = restrictions.Merge(
                Extensions.MergeTypeRestrictions(args).Merge(
                    Extensions.MergeInstanceRestrictions(target)));

            Delegate function = null;
            DynamicMetaObject actualTarget = null;
            if (target.LimitType.IsSubclassOf(typeof(Delegate)))
            {
                function = (Delegate)target.Value;
                actualTarget = target;
            }

            var methodInfo = function.Method;

            bool toss = false;
            return GetInvoker(actualTarget, args, methodInfo, out toss, ref restrictions);
        }

        private DynamicMetaObject GetInvoker(DynamicMetaObject target, DynamicMetaObject[] args, MethodInfo methodInfo, out bool success, ref BindingRestrictions restrictions)
        {
            Expr failExpr;
            List<Expr> sideEffects;

            var mappedArgs = MapArguments(args, methodInfo, ref restrictions, out sideEffects, out failExpr);

            success = failExpr == null;

            if (!success)
                return new DynamicMetaObject(Expr.Block(failExpr, Expr.Default(typeof(object))), restrictions);


            var invokeExpr = InvokeExpression(target, mappedArgs, methodInfo);

            // Execute overflowing arguments for side effects
            Expr expr;
            if (sideEffects.Count == 0)
            {
                expr = invokeExpr;
            }
            else
            {
                var tempVar = Expr.Variable(typeof(object));
                var assign = Expr.Assign(tempVar, invokeExpr);
                sideEffects.Insert(0, assign);
                sideEffects.Add(tempVar);
                expr = Expr.Block(new[] { tempVar }, sideEffects);
            }

            return new DynamicMetaObject(expr, restrictions);
        }

        Expr InvokeExpression(DynamicMetaObject target, IEnumerable<Expr> mappedArgs, MethodInfo methodInfo)
        {
            var invokeExpr = Expr.Invoke(
                Expr.Convert(target.Expression, target.LimitType),
                mappedArgs);

            Expr expr = null;

            if (methodInfo.ReturnType == typeof(void))
                expr = Expr.Block(invokeExpr, Expr.Default(typeof(object)));
            else
                expr = Expr.Convert(invokeExpr, typeof(object));

            return expr;
        }

        IEnumerable<Expr> MapArguments(DynamicMetaObject[] args, MethodInfo methodInfo, ref BindingRestrictions restrictions, out List<Expr> sideEffects, out Expr failExpr)
        {
            var parameters = methodInfo.GetParameters();
            var arguments = args.Select(arg => new Argument(arg.Expression, arg.LimitType)).ToList();

            // Remove closure
            if (parameters.Length > 0 && parameters[0].ParameterType == typeof(Closure))
            {
                var tempParameters = new ParameterInfo[parameters.Length - 1];
                Array.Copy(parameters, 1, tempParameters, 0, tempParameters.Length);
                parameters = tempParameters;
            }

            DefaultParamValues(arguments, parameters);
            OverflowIntoParams(arguments, parameters);
            TrimArguments(arguments, parameters, out sideEffects);
            DefaultParamTypeValues(arguments, parameters);
            ConvertArgumentToParamType(arguments, parameters, out failExpr);
            if (failExpr == null)
                CheckNumberOfArguments(arguments, parameters, out failExpr);

            return arguments.Select(arg => arg.Expression);
        }

        void DefaultParamValues(List<Argument> arguments, ParameterInfo[] parameters)
        {
            var defaultArgs = parameters
                .Skip(arguments.Count)
                .Where(param => param.IsOptional)
                .Select(param => new Argument(Expr.Constant(param.DefaultValue), param.ParameterType));

            arguments.AddRange(defaultArgs);
        }

        void OverflowIntoParams(List<Argument> arguments, ParameterInfo[] parameters)
        {
            if (arguments.Count == 0 || parameters.Length == 0)
                return;

            var overflowingArgs = arguments.Skip(parameters.Length - 1).ToList();
            var lastParam = parameters.Last();

            if (overflowingArgs.Count == 1 && overflowingArgs[0].Type == lastParam.ParameterType)
                return;

            Expr argExpr;
            if (lastParam.IsParams())
            {
                var elementType = lastParam.ParameterType.GetElementType();
                if (overflowingArgs.Any(arg => arg.Type != elementType && !arg.Type.IsSubclassOf(elementType)))
                    return;

                argExpr = Expr.NewArrayInit(
                    elementType,
                    overflowingArgs.Select(arg => Expr.Convert(arg.Expression, elementType)));
            }
            else
            {
                return;
            }

            arguments.RemoveRange(arguments.Count - overflowingArgs.Count, overflowingArgs.Count);
            arguments.Add(new Argument(argExpr, lastParam.ParameterType));
        }

        void TrimArguments(List<Argument> arguments, ParameterInfo[] parameters, out List<Expr> sideEffects)
        {
            if (arguments.Count <= parameters.Length)
            {
                sideEffects = new List<Expr>();
                return;
            }

            sideEffects = arguments
                .Skip(parameters.Length)
                .Select(arg => arg.Expression)
                .ToList();
            arguments.RemoveRange(parameters.Length, arguments.Count - parameters.Length);
        }

        void DefaultParamTypeValues(List<Argument> arguments, ParameterInfo[] parameters)
        {
            var typeDefaultArgs = parameters
                .Skip(arguments.Count)
                .Select(param => new Argument(Expr.Constant(param.ParameterType.GetDefaultValue()), param.ParameterType));
            arguments.AddRange(typeDefaultArgs);
        }

        void ConvertArgumentToParamType(List<Argument> arguments, ParameterInfo[] parameters, out Expr failExpr)
        {
            failExpr = null;

            for (int i = 0; i < arguments.Count; i++)
            {
                var arg = arguments[i];
                var param = parameters[i];

                if (arg.Type == param.ParameterType || arg.Type.IsSubclassOf(param.ParameterType))
                {
                    arg.Expression = Expr.Convert(arg.Expression, param.ParameterType);
                }
                else                
                    throw new InvalidCastException(string.Format("Cannot convert from {0} to {1} for parameter '{2}'", arg.Type, param.ParameterType, param.Name));                
            }
        }

        static readonly ConstructorInfo InvalidOperationException = typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) });

        void CheckNumberOfArguments(List<Argument> arguments, ParameterInfo[] parameters, out Expr failExpr)
        {
            failExpr = null;
            Debug.Assert(arguments.Count <= parameters.Length);

            if (arguments.Count < parameters.Length)
            {
                failExpr = Expr.Throw(
                    Expr.New(
                        InvalidOperationException,
                        Expr.Constant("Not enough arguments provided to call the function.")));
            }
        }

        class Argument
        {
            public Expr Expression { get; set; }
            public Type Type { get; private set; }

            public Argument(Expr expression, Type type)
            {
                Expression = expression;
                Type = type;
            }
        }
    }
}