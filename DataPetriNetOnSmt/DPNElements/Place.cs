using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.Enums;

namespace DataPetriNetOnSmt.DPNElements
{
    public class Place : Node
    {
        public int Tokens { get; set; }
        public bool IsFinal { get; set; }

        public Place()
        {

        }

        public Place(string label, PlaceType placeType)
        {
            Label = label;
            Id = label;
            IsFinal = placeType == PlaceType.Final;
            Tokens = placeType == PlaceType.Initial ? 1 : 0;
        }
    }
}
