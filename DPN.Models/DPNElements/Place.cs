using DPN.Models.Abstractions;
using DPN.Models.Enums;

namespace DPN.Models.DPNElements
{
    public class Place : Node, ICloneable
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

        public object Clone()
        {
            return new Place
            {
                Label = this.Label,
                Id = this.Id,
                Tokens = this.Tokens,
                IsFinal = this.IsFinal,
            };
        }
    }
}
