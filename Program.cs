using API_Console.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API_Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var e = new Engine();
            e.RegisterController("cookies", new CookiesController(e));
            e.RegisterController("headers", new HeadersController(e));
            e.RegisterController("servers", new ServersController(e));
            e.RegisterController("http", new HttpController(e));
            e.RegisterController("auth", new AuthController(e));
            e.Run();
        }
    }
}
