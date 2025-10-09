using DPN.Models.Enums;

namespace DPN.Models.DPNElements
{
    public sealed class Marking// : Dictionary<Node, int>
    {
        private IDictionary<Place, int> markingDictionary;
        private Marking(IDictionary<Place, int> marking)
        {
            this.markingDictionary = marking;
        }
        public Marking(Marking marking)
        {
            markingDictionary = new Dictionary<Place, int>(marking.markingDictionary);
        }
        public Marking()
        {
            markingDictionary= new Dictionary<Place, int>();
        }

        public int this[Place place]
        {
            get { return markingDictionary[place]; }
            set { markingDictionary[place] = value; }
        }

        public ICollection<Place> Keys
        {
            get
            {
                return markingDictionary.Keys;
            }
        }

        public Dictionary<string, int> AsDictionary()
        {
            return markingDictionary.ToDictionary(x=> x.Key.Id, y=>y.Value);
        }

        public static Marking FromDpnPlaces(List<Place> places)
        {
            return new Marking(places.ToDictionary(x => x, y => y.Tokens));
        }

        public static Marking FinalMarkingFromDpnPlaces(List<Place> places)
        {
            return new Marking(places.ToDictionary(x => x, y => y.IsFinal ? 1 : 0));
        }

        public override string ToString()
        {
            return string.Join(", ", markingDictionary
                    .Where(x => x.Value > 0)
                    .Select(x => x.Value > 1
                        ? x.Value.ToString() + x.Key.Label
                        : x.Key.Label));
        }


        public List<Transition> GetEnabledTransitions(DataPetriNet dpn)
        {
            var transitionsWhichCanFire = new List<Transition>();

            foreach (var transition in dpn.Transitions)
            {
                var preSetArcs = dpn.Arcs.Where(x => x.Destination == transition).ToList();

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
