using Microsoft.Z3;

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
