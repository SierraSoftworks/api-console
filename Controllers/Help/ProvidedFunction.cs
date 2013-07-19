using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace API_Console.Controllers.Help
{
    public sealed class ProvidedFunction
    {
        #region Statics

        public static IEnumerable<ProvidedFunction> GetFunctions(IDictionary<string,object> container, bool extendedInfo = false)
        {
            foreach (var name in container.Keys)
            {
                ProvidedFunction value = null;

                value = new ProvidedFunction(container, name, extendedInfo);

                yield return value;
            }
        }

        #endregion

        public ProvidedFunction(IDictionary<string,object> container, string name, bool extendedInfo)
        {
            Name = name;
            HasExtendedInfo = extendedInfo;
            try
            {
                dynamic function = container[name];
                dynamic fnMethodInfo = function.Method;

                dynamic parameters = fnMethodInfo.GetParameters();

                List<string> paramNames = new List<string>();
                foreach (var param in parameters)
                {
                    var p = param as ParameterInfo;
                    var pstring = "";

                    if (extendedInfo)                    
                        pstring += p.ParameterType.ToGenericTypeString() + " ";
                    
                    pstring += p.Name;

                    if (p.IsParams())
                        pstring += "[]";

                    if (p.HasDefaultValue)
                        pstring = "[" + pstring + "]";

                    paramNames.Add(pstring);
                }
                Parameters = paramNames.ToArray();
            }
            catch
            {
                Parameters = new string[0];
            }
        }

        public string Name { get; private set; }
        public string[] Parameters { get; private set; }
        public bool HasExtendedInfo { get; private set; }

        public string Format()
        {
            return string.Format("{0} {1}", Name, Parameters.Length > 0 ? "(" + Parameters.Aggregate((x, y) => x + "," + y) + ")" : "=");
        }

        private string FormatObject(object property)
        {
            return JsonConvert.SerializeObject(
                property, Formatting.Indented,
                new JsonConverter[] { new StringEnumConverter() });
        }
    }
}
