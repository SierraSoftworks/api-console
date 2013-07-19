using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace API_Console.Controllers
{
    class CookiesController : ControllerBase
    {
        public CookiesController(Engine engine)
            : base(engine)
        {

        }

        private dynamic cookies;

        protected override ExpandoObject Initialize()
        {
            cookies = new ExpandoObject();

            cookies.list = new Func<string, object>(domain =>
            {
                CookieContainer cc = null;
                Engine.UpdateClient(x => cc = x.CookieContainer);

                return cc.GetCookieHeader(new Uri(domain));
            });

            cookies.add = new Func<string, string, string, object>((domain, name, value) =>
            {
                CookieContainer cc = null;
                Engine.UpdateClient(x => x.CookieContainer = x.CookieContainer ?? new CookieContainer());
                Engine.UpdateClient(x => x.CookieContainer.Add(new Uri(domain), new System.Net.Cookie(name, value)));
                Engine.UpdateClient(x => cc = x.CookieContainer);

                return cc.GetCookieHeader(new Uri(domain));
            });

            cookies.set = new Func<string, string, object>((domain, cookie) =>
            {
                CookieContainer cc = null;
                Engine.UpdateClient(x => x.CookieContainer = x.CookieContainer ?? new CookieContainer());
                Engine.UpdateClient(x => x.CookieContainer.SetCookies(new Uri(domain), cookie));
                Engine.UpdateClient(x => cc = x.CookieContainer);

                return cc.GetCookieHeader(new Uri(domain));
            });

            return cookies;
        }
    }
}
