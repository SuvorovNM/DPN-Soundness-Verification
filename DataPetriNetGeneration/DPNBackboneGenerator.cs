using DataPetriNetOnSmt;
using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using Microsoft.Z3;

namespace DataPetriNetGeneration
{
    public class DPNBackboneGenerator
    {
        public Context Context { get; private set; }
        public DPNBackboneGenerator(Context context)
        {
            Context = context;
        }

        private readonly int minTransitionPerPlace = 1;
        private readonly Random random = new Random();

        // To async?
        public DataPetriNet GenerateBackbone(int placesCount, int transitionsCount, int extraArcsCount)
            // Is there any maximum for additionalArcsCount?
        {
            var dpn = GenerateSoundBackbone(placesCount, transitionsCount);

            var arcsRemained = extraArcsCount;

            // We need additional Transition -> Place and Place -> Transition arcs
            while (arcsRemained > 0)
            {
                var arcTypeChosen = (ArcType)random.Next(0, 1);
                var placeChosen = dpn.Places[random.Next(0, dpn.Places.Count)];
                var transitionChosen = dpn.Transitions[random.Next(0, dpn.Transitions.Count)];

                if (arcTypeChosen == ArcType.PlaceTransition)
                {
                    AddArc(dpn, placeChosen, transitionChosen);
                }
                else
                {
                    AddArc(dpn, transitionChosen, placeChosen);
                }

                arcsRemained--;
            }

            return dpn;
        }

        private void AddArc(DataPetriNet dpn, Node source, Node target)
        {
            var existentArc = dpn.Arcs.FirstOrDefault(x=> x.Source == source && x.Destination == target);
            if (existentArc != null)
            {
                existentArc.Weight++;
            }
            else
            {
                dpn.Arcs.Add(new Arc(source, target));
            }
        }

        public DataPetriNet GenerateSoundBackbone(int placesCount, int transitionsCount)
        {
            if (placesCount < 2)
            {
                throw new ArgumentException("Number of places cannot be less than 2");
            }
            if (transitionsCount < 1)
            {
                throw new ArgumentException("Number of transitions cannot be less than 1");
            }

            var maxTransitionPerPlace = transitionsCount / (placesCount - 1);
            var transitionsRemained = transitionsCount;
            var placesRemained = placesCount;

            var dpn = new DataPetriNet(Context);

            var initialPlace = new Place("i", PlaceType.Initial); // TODO: Maybe add inheritance instead of PlaceType enum
            dpn.Places.Add(initialPlace);
            placesRemained -= 1;

            int chosenTransitionsNumber;
            GenerateIntermediaryTransitions(initialPlace, transitionsCount - transitionsRemained, dpn, maxTransitionPerPlace, out chosenTransitionsNumber);
            transitionsRemained -= chosenTransitionsNumber;         

            while (placesRemained > 1)
            {
                GenerateIntermediaryPlace(placesCount - placesRemained, dpn);
                placesRemained -= 1;

                GenerateIntermediaryTransitions(dpn.Places.Last(), transitionsCount - transitionsRemained, dpn, maxTransitionPerPlace, out chosenTransitionsNumber);
                transitionsRemained -= chosenTransitionsNumber;
            };

            while (transitionsRemained > 0)
            {
                var chosenPlace = dpn.Places[random.Next(dpn.Places.Count)];
                var maxTransitions = Math.Min(maxTransitionPerPlace, transitionsRemained);
                GenerateIntermediaryTransitions(chosenPlace, transitionsCount - transitionsRemained, dpn, maxTransitions, out chosenTransitionsNumber);
                transitionsRemained -= chosenTransitionsNumber;
            }

            var outputPlace = new Place("o", PlaceType.Final);
            dpn.Places.Add(outputPlace);

            var transitionsWithoutOutgoingArcs = dpn.Transitions.Except(dpn.Arcs.Select(x => x.Source));
            foreach (var transition in transitionsWithoutOutgoingArcs)
            {
                dpn.Arcs.Add(new Arc(transition, outputPlace));
            }

            return dpn;
        }

        private void GenerateIntermediaryPlace(int id, DataPetriNet dpn)
        {
            var availableTransitions = dpn.Transitions.Except(dpn.Arcs.Select(x => x.Source)).ToList();
            var transitionWithFollowingPlace = random.Next(0, availableTransitions.Count);
            var place = new Place($"p{id}", PlaceType.Intermediary);
            dpn.Places.Add(place);
            dpn.Arcs.Add(new Arc(availableTransitions[transitionWithFollowingPlace], place));
        }

        private void GenerateIntermediaryTransitions(
            Place sourcePlace,
            int startTransitionId, 
            DataPetriNet dpn, 
            int maxTransitionPerPlace, 
            out int chosenTransitionsNumber)
        {
            chosenTransitionsNumber = random.Next(minTransitionPerPlace, maxTransitionPerPlace + 1);
            for (int i = 0; i < chosenTransitionsNumber; i++)
            {
                var transition = new Transition($"t{startTransitionId + i}");
                dpn.Transitions.Add(transition);
                dpn.Arcs.Add(new Arc(sourcePlace, transition));
            }
        }
    }
}