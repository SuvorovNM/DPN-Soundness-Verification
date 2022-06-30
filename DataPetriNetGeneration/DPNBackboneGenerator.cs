using DataPetriNetOnSmt;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;

namespace DataPetriNetGeneration
{
    public class DPNBackboneGenerator
    {
        private int minTransitionPerPlace = 1;
        private Random random = new Random();

        public DataPetriNet Generate(int placesCount, int transitionsCount)
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

            var dpn = new DataPetriNet();

            var initialPlace = new Place("i", PlaceType.Initial);
            dpn.Places.Add(initialPlace);
            placesRemained -= 1;

            int chosenTransitionsNumber;
            GenerateIntermediaryTransitions(initialPlace, transitionsCount - transitionsRemained, dpn, maxTransitionPerPlace, out chosenTransitionsNumber);
            transitionsRemained -= chosenTransitionsNumber;         

            while (placesRemained > 1)
            {
                GenerateIntermediaryPlace(placesCount - placesRemained + 1, dpn);
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