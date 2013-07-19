using SierraSoftworks.PackageServer.API;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API_Console.Controllers
{
    class AuthController : ControllerBase
    {
        public AuthController(Engine engine)
            : base(engine)
        {

        }

        protected override System.Dynamic.ExpandoObject Initialize()
        {
            dynamic api = new ExpandoObject();

            api.publickey = null;
            api.privatekey = null;

            api.set = new Func<string, string, object>((publickey, privatekey) =>
            {
                api.publickey = publickey;
                api.privatekey = privatekey;

                Engine.UpdateClient(client =>
                {
                    client.Authenticator = new Authenticator(publickey, privatekey);
                });

                return new { publickey = publickey, privatekey = privatekey };
            });

            return api;
        }
    }
}
