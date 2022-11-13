using DataPetriNetOnSmt;
using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using Microsoft.Z3;
using System.Runtime.Intrinsics.Arm;

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
            var existentArc = dpn.Arcs.FirstOrDefault(x => x.Source == source && x.Destination == target);
            if (existentArc != null)
            {
                existentArc.Weight++;
            }
            else
            {
                dpn.Arcs.Add(new Arc(source, target));
            }
        }

        // TODO: Allow to work if |P| > |T|
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

            // if transitionsCount + 1 >= placesCount
            if (transitionsCount + 1 >= placesCount)
                return GenerateWithPrevailingTransitions(placesCount, transitionsCount);
            else
                return GenerateWithPrevailingPlaces(placesCount, transitionsCount);
        }

        private DataPetriNet GenerateWithPrevailingPlaces(int placesCount, int transitionsCount)
        {
            var transitionsRemained = transitionsCount;
            var placesRemained = placesCount;

            var dpn = new DataPetriNet(Context);

            var initialPlace = new Place("i", PlaceType.Initial);
            dpn.Places.Add(initialPlace);
            placesRemained -= 1;

            while (transitionsRemained > 1)
            {
                GenerateIntermediaryTransitions(dpn.Places.Last(), transitionsCount - transitionsRemained, dpn);
                transitionsRemained -= 1;

                GenerateIntermediaryPlace(placesCount - placesRemained, dpn);
                placesRemained -= 1;
            }

            GenerateIntermediaryTransitions(dpn.Places.Last(), transitionsCount - transitionsRemained, dpn);

            var outputPlace = new Place("o", PlaceType.Final);
            dpn.Places.Add(outputPlace);
            placesRemained -= 1;
            dpn.Arcs.Add(new Arc(dpn.Transitions.Last(), outputPlace));

            while (placesRemained > 0)
            {
                var transitionId = random.Next(dpn.Transitions.Count - 1);

                var chosenTransition1 = dpn.Transitions[transitionId];
                var chosenTransition2 = dpn.Transitions[transitionId + 1];

                var place = new Place($"p{placesCount - placesRemained}", PlaceType.Intermediary);
                dpn.Places.Add(place);
                dpn.Arcs.Add(new Arc(chosenTransition1, place));
                dpn.Arcs.Add(new Arc(place, chosenTransition2));

                placesRemained -= 1;
            }

            return dpn;
        }

        private DataPetriNet GenerateWithPrevailingTransitions(int placesCount, int transitionsCount)
        {
            var transitionsRemained = transitionsCount;
            var placesRemained = placesCount;

            var dpn = new DataPetriNet(Context);

            var initialPlace = new Place("i", PlaceType.Initial);
            dpn.Places.Add(initialPlace);
            placesRemained -= 1;

            GenerateIntermediaryTransitions(initialPlace, transitionsCount - transitionsRemained, dpn);
            transitionsRemained -= 1;

            while (placesRemained > 1)
            {
                GenerateIntermediaryPlace(placesCount - placesRemained, dpn);
                placesRemained -= 1;

                GenerateIntermediaryTransitions(dpn.Places.Last(), transitionsCount - transitionsRemained, dpn);
                transitionsRemained -= 1;
            };

            var outputPlace = new Place("o", PlaceType.Final);
            dpn.Places.Add(outputPlace);
            dpn.Arcs.Add(new Arc(dpn.Transitions.Last(), outputPlace));

            while (transitionsRemained > 0)
            {
                var chosenPlace1 = dpn.Places[random.Next(dpn.Places.Count)];
                var chosenPlace2 = dpn.Places[random.Next(dpn.Places.Count)];

                var transition = new Transition($"t{transitionsCount - transitionsRemained}", new Guard(Context));
                dpn.Transitions.Add(transition);
                dpn.Arcs.Add(new Arc(chosenPlace1, transition));
                dpn.Arcs.Add(new Arc(transition, chosenPlace2));

                transitionsRemained -= 1;
            }

            return dpn;
        }

        private void GenerateIntermediaryPlace(int id, DataPetriNet dpn)
        {
            //var availableTransitions = dpn.Transitions.Except(dpn.Arcs.Select(x => x.Source)).ToList();
            //var transitionWithFollowingPlace = random.Next(0, availableTransitions.Count);
            var place = new Place($"p{id}", PlaceType.Intermediary);
            dpn.Places.Add(place);
            dpn.Arcs.Add(new Arc(dpn.Transitions.Last(), place));
        }

        private void GenerateIntermediaryTransitions(
            Place sourcePlace,
            int transitionId,
            DataPetriNet dpn)
        {
            var transition = new Transition($"t{transitionId}", new Guard(Context));
            dpn.Transitions.Add(transition);
            dpn.Arcs.Add(new Arc(sourcePlace, transition));
        }
    }
}