using System;
using System.Collections.Generic;
using System.Linq;

namespace DataPetriNetIterativeVerificationApplication.Extensions
{
    public static class EnumExtension
    {
        public static IEnumerable<Enum> GetFlags(this Enum e)
        {
            return Enum.GetValues(e.GetType()).Cast<Enum>().Where(e.HasFlag);
        }
    }
}
