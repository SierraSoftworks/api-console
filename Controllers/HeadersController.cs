using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API_Console.Controllers
{
    class HeadersController : ControllerBase
    {
        public HeadersController(Engine engine)
            : base(engine)
        {

        }

        protected override System.Dynamic.ExpandoObject Initialize()
        {
            dynamic api = new ExpandoObject();

            api.list = new ExpandoObject();

            api.set = new Func<string, string, object>((key, value) =>
            {
                ((IDictionary<string, object>)api.list).Remove(key);
                ((IDictionary<string, object>)api.list).Add(key, value);
                Engine.UnregisterPreprocessor(key);
                Engine.RegisterPreprocessor(key, x => x.AddHeader(key, value));
                return api.list;
            });

            api.clear = new Func<string, object>(key =>
            {
                ((IDictionary<string, object>)api.list).Remove(key);
                Engine.UnregisterPreprocessor(key);
                return api.list;
            });

            return api;
        }
    }
}
