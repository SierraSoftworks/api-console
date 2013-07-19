using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace API_Console
{
    static class Extensions
    {
        public static string ToJson(this object property)
        {
            return JsonConvert.SerializeObject(
                    property, Formatting.Indented,
                    new JsonConverter[] { new StringEnumConverter() });
        }

        public static IEnumerable<T> And<T>(this IEnumerable<T> source, T and)
        {
            using (var e = source.GetEnumerator())
                while (e.MoveNext())
                    yield return e.Current;
            yield return and;
        }
        public static IEnumerable<T> ButFirst<T>(this IEnumerable<T> source, T and)
        {
            yield return and;
            using (var e = source.GetEnumerator())
                while (e.MoveNext())
                    yield return e.Current;
        }

        public static object GetDefaultValue(this Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        public static bool IsParams(this ParameterInfo parameter)
        {
            return Attribute.IsDefined(parameter, typeof(ParamArrayAttribute));
        }

        public static string ToGenericTypeString(this Type t)
        {
            string genericTypeName = null;
            if (!t.IsGenericType && t.Name.IndexOf('`') != -1)
                genericTypeName = t.Name;
            else if (t.IsGenericType)
                genericTypeName = t.GetGenericTypeDefinition().Name;
            else
                return t.Name;

            genericTypeName = genericTypeName.Substring(0, genericTypeName.IndexOf('`'));
            string genericArgs = string.Join(",", t.GetGenericArguments().Select(ta => ToGenericTypeString(ta)).ToArray());
            return genericTypeName + "<" + genericArgs + ">";
        }

        public static BindingRestrictions MergeTypeRestrictions(DynamicMetaObject dmo1, DynamicMetaObject[] dmos)
        {
            var newDmos = new DynamicMetaObject[dmos.Length + 1];
            newDmos[0] = dmo1;
            Array.Copy(dmos, 0, newDmos, 1, dmos.Length);
            return MergeTypeRestrictions(newDmos);
        }

        public static BindingRestrictions MergeTypeRestrictions(params DynamicMetaObject[] dmos)
        {
            var restrictions = BindingRestrictions.Combine(dmos);

            foreach (var dmo in dmos)
            {
                if (dmo.HasValue && dmo.Value == null)
                    restrictions = restrictions.Merge(BindingRestrictions.GetInstanceRestriction(dmo.Expression, dmo.Value));
                else
                    restrictions = restrictions.Merge(BindingRestrictions.GetTypeRestriction(dmo.Expression, dmo.LimitType));
            }

            return restrictions;
        }

        public static BindingRestrictions MergeInstanceRestrictions(params DynamicMetaObject[] dmos)
        {
            var restrictions = BindingRestrictions.Combine(dmos);
            return dmos.Aggregate(
                restrictions,
                (current, dmo) => current.Merge(BindingRestrictions.GetInstanceRestriction(dmo.Expression, dmo.Value)));
        }
    }

    public class ConsoleForeground : IDisposable
    {
        public ConsoleForeground(ConsoleColor color)
        {
            originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
        }
        
        ConsoleColor originalColor;

        public void Dispose()
        {
            Console.ForegroundColor = originalColor;
        }
    }

    public class ConsoleBackground : IDisposable
    {
        public ConsoleBackground(ConsoleColor color)
        {
            originalColor = Console.BackgroundColor;
            Console.BackgroundColor = color;
        }

        ConsoleColor originalColor;

        public void Dispose()
        {
            Console.BackgroundColor = originalColor;
        }
    }
}
