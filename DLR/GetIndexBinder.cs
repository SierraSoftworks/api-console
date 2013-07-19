using System.Diagnostics.Contracts;
using System.Dynamic;
using System.Reflection;
using System.Linq;
using System;
using Microsoft.Scripting.Actions;
using System.Linq.Expressions;
using API_Console.DLR;

namespace API_Console.DLR
{
    class GetIndexBinder : System.Dynamic.GetIndexBinder
    {
        public GetIndexBinder(CallInfo callInfo)
            : base(callInfo)
        {

        }

        public GetIndexBinder()
            : this( new CallInfo(1))
        {  
          
        }

        public static readonly object MissingIndex = new object();

        private DynamicMetaObject WrapToObject(DynamicMetaObject obj)
        {
            if (obj.LimitType != typeof(object))
                return new DynamicMetaObject(Expression.Convert(obj.Expression, typeof(object)), obj.Restrictions, obj.Value as object);
            return obj;
        }

        public override DynamicMetaObject FallbackGetIndex(DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject errorSuggestion)
        {
            if (target.LimitType.GetMethods().Any(x => x.Name == "get_Item" && x.GetParameters().Length == CallInfo.ArgumentCount))
            {
                DynamicMetaObject[] args = new DynamicMetaObject[indexes.Length + 1];
                args[0] = target;
                Array.Copy(indexes, 0, args, 1, indexes.Length);

                var method = target.LimitType.GetMethods().Where(x => x.Name.Equals("get_Item") && x.GetParameters().Length == CallInfo.ArgumentCount).First();

                var getExpr = APIBinder.Instance.MakeCallExpression(DefaultOverloadResolver.Factory, method, args);

                var temp = Expression.Variable(typeof(object));
                var catchExpr = Expression.Block(
                    new[] { temp },
                    new Expression[] {
                        Expression.TryCatch(
                            Expression.Assign(temp, getExpr.Expression),
                            Expression.Catch(typeof(Exception), Expression.Assign(temp, Expression.Constant(null)))
                            ),
                        temp
                        }
                    );

                var bindingRestrictions = BindingRestrictions.GetInstanceRestriction(target.Expression, target.Value);
                bindingRestrictions = bindingRestrictions.Merge(BindingRestrictions.GetTypeRestriction(indexes[0].Expression, indexes[0].LimitType));

                return WrapToObject(new DynamicMetaObject(catchExpr, bindingRestrictions));
            }

            return base.FallbackGetIndex(target, indexes);            
        }
    }
}