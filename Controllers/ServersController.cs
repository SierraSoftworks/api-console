using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API_Console.Controllers
{
    class ServersController : ControllerBase
    {
        public ServersController(Engine engine)
            : base(engine)
        {

        }

        protected override System.Dynamic.ExpandoObject Initialize()
        {
            dynamic api = new ExpandoObject();

            api.current = null;
            api.list = new ExpandoObject();
            api.add = new Func<string, string, object>((name, address) =>
            {
                (api.list as IDictionary<string, object>).Add(name, new { name = name, address = address });

                if (api.current == null)
                {
                    api.current = (api.list as IDictionary<string, object>)[name];
                    Engine.UpdateClient(x => x.BaseUrl = address);
                }

                return (api.list as IDictionary<string, object>)[name];
            });

            api.use = new Func<string,object>((name) =>
            {
                api.current = (api.list as IDictionary<string, object>)[name];
                Engine.UpdateClient(x => x.BaseUrl = api.current.address.ToString());

                return Engine.NoOutput;
            });

            api.rm = new Func<string,object>(name => {
                (api.list as IDictionary<string, object>).Remove(name);

                return Engine.NoOutput;
            });
            
            return api;
        }
    }
}
