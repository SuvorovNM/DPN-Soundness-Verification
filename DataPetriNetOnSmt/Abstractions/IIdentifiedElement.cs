using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNetOnSmt.Abstractions
{
    interface IIdentifiedElement
    {
        // According to current information, ids can be string values
        string Id { get; set; }
    }
}
