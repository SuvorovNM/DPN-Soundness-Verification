using DPN.Models.Enums;

namespace DPN.Models.DPNElements
{
    public sealed class Marking
    {
        private Dictionary<string, int> placeIdToTokens;
        private Marking(Dictionary<string, int> marking)
        {
            this.placeIdToTokens = marking;
        }
        public Marking(Marking marking)
        {
	        placeIdToTokens = new Dictionary<string, int>(marking.placeIdToTokens);
        }
        public Marking()
        {
	        placeIdToTokens= new Dictionary<string, int>();
        }

        public int this[Place place]
        {
            get { return placeIdToTokens[place.Id]; }
            set { placeIdToTokens[place.Id] = value; }
        }
        
        public int this[string placeId]
        {
	        get { return placeIdToTokens[placeId]; }
	        set { placeIdToTokens[placeId] = value; }
        }

        public ICollection<string> Keys
        {
            get
            {
                return placeIdToTokens.Keys;
            }
        }

        public Dictionary<string, int> AsDictionary()
        {
            return placeIdToTokens;
        }

        public static Marking FromDpnPlaces(List<Place> places)
        {
            return new Marking(places.ToDictionary(x => x.Id, y => y.Tokens));
        }

        public static Marking FinalMarkingFromDpnPlaces(List<Place> places)
        {
            return new Marking(places.ToDictionary(x => x.Id, y => y.IsFinal ? 1 : 0));
        }

        public override string ToString()
        {
            return string.Join(", ", placeIdToTokens
                    .Where(x => x.Value > 0)
                    .Select(x => x.Value > 1
                        ? x.Value.ToString() + x.Key
                        : x.Key));
        }


        public List<Transition> GetEnabledTransitions(DataPetriNet dpn)
        {
            var transitionsWhichCanFire = new List<Transition>();

            foreach (var transition in dpn.Transitions)
            {
                var preSetArcs = dpn.Arcs.Where(x => x.Destination.Id == transition.Id).ToList();

                var canFire = preSetArcs.All(x => this[(Place)x.Source] >= x.Weight);

                if (canFire)
                {
                    transitionsWhichCanFire.Add(transition);
                }
            }

            return transitionsWhichCanFire;
        }

        public MarkingComparisonResult CompareTo(Marking? other)
        {
            if (other == null)
                return MarkingComparisonResult.Incomparable;

            if (other.Keys.Count != this.Keys.Count || other.Keys.Intersect(this.Keys).Count() != this.Keys.Count)
                return MarkingComparisonResult.Incomparable;

            var strictlyGreaterExists = false;
            var strictlyLessExists = false;

            foreach (var place in this.Keys)
            {
                var comparisonResult = this[place].CompareTo(other[place]);
                if ((strictlyLessExists |= comparisonResult == -1) && strictlyGreaterExists)
                    return MarkingComparisonResult.Incomparable;
                if ((strictlyGreaterExists |= comparisonResult == 1) && strictlyLessExists)
                    return MarkingComparisonResult.Incomparable;
            }

            if (strictlyGreaterExists)
                return MarkingComparisonResult.GreaterThan;
            if (strictlyLessExists)
                return MarkingComparisonResult.LessThan;

            return MarkingComparisonResult.Equal;
        }
    }
}
