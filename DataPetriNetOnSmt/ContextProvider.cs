using Microsoft.Z3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt
{
    public static class ContextProvider
    {
        public static Context Context;
        static ContextProvider()
        {
            Context = new Context();
        }
    }
}
