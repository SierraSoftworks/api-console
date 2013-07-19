using Microsoft.Scripting.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API_Console.DLR
{
    class APIBinder : DefaultBinder
    {
        private static APIBinder _instance;
        public static APIBinder Instance
        {
            get { _instance = _instance ?? new APIBinder(); return _instance; }
        }

        public override bool CanConvertFrom(Type fromType, Type toType, bool toNotNullable, Microsoft.Scripting.Actions.Calls.NarrowingLevel level)
        {
            return false;
        }

        public override Microsoft.Scripting.Actions.Calls.Candidate PreferConvert(Type t1, Type t2)
        {
            return Microsoft.Scripting.Actions.Calls.Candidate.Equivalent;
        }
    }
}
