using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API_Console.Controllers
{
    abstract class ControllerBase
    {
        public ControllerBase(Engine engine)
        {
            Engine = engine;
        }

        private ExpandoObject _api;
        public ExpandoObject API
        {
            get
            {
                if (_api == null)
                    _api = Initialize();
                return _api;
            }
        }

        protected Engine Engine
        { get; private set; }

        protected abstract ExpandoObject Initialize();

        
    }
}
