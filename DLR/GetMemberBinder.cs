using Microsoft.Scripting.Actions;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace API_Console.DLR
{
    class GetMemberBinder : System.Dynamic.GetMemberBinder
    {
        public GetMemberBinder(string name)
            : base(name, true)
        {

        }

        public static readonly object MissingMember = new object();

        private DynamicMetaObject WrapToObject(DynamicMetaObject obj)
        {
            if (obj.LimitType != typeof(object))
                return new DynamicMetaObject(Expression.Convert(obj.Expression, typeof(object)), obj.Restrictions, obj.Value as object);
            return obj;
        }

        public override System.Dynamic.DynamicMetaObject FallbackGetMember(System.Dynamic.DynamicMetaObject target, 
            System.Dynamic.DynamicMetaObject errorSuggestion)
        {
            if (!target.HasValue)
                return Defer(target);


            if (target.LimitType == typeof(IDynamicMetaObjectProvider))
                return new DefaultBinder().GetMember(Name, target, true,
                    new DynamicMetaObject(Expression.Constant(null, typeof(object)), BindingRestrictions.Empty, null));


            return WrapToObject(APIBinder.Instance.GetMember(Name, target, true,
                new DynamicMetaObject(Expression.Constant(null, typeof(object)), BindingRestrictions.Empty, null)));  
        }
    }
}
