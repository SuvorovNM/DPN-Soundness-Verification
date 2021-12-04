using DataPetriNet.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataPetriNet.Services.SourceServices
{
    public interface ISourceService
    {
        IDefinableValue Read(string name);
        void Write(string name, IDefinableValue value);
        void Clear();
    }
}
