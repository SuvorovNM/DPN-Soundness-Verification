using DataPetriNet.Abstractions;

namespace DataPetriNet.DPNElements
{
    public class Place : Node // TODO: define the necessity of using Ids
    {
        public int Tokens { get; set; }
    }
}
