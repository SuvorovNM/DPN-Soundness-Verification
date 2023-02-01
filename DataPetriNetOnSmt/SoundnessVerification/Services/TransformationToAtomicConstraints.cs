using DataPetriNetOnSmt;
using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;
using System.Diagnostics;

namespace DataPetriNetOnSmt.SoundnessVerification.Services
{
    internal class TransitionInfo
    {
        public Transition Transition { get; set; }
        public List<(Place place, int weight)> Preset { get; set; }
        public List<(Place place, int weight)> Postset { get; set; }
        public TransitionInfo(Transition transition, List<(Place place, int weight)> preset, List<(Place place, int weight)> postset)
        {
            Transition = transition;
            Preset = preset;
            Postset = postset;
        }
    }

    internal class SortedConjuncts
    {
        public List<IConstraintExpression> ReadExpressions { get; set; }
        public List<IConstraintExpression> WriteExpressions { get; set; }
        public SortedConjuncts()
        {
            ReadExpressions = new List<IConstraintExpression>();
            WriteExpressions = new List<IConstraintExpression>();
        }
    }

    public class TransformationToAtomicConstraints
    {
        private int overallPlaceIndex = 0;
        private Place lockingPlace = null;

        public DataPetriNet Transform(DataPetriNet sourceDpn)
        {
            if (sourceDpn == null)
            {
                throw new ArgumentNullException(nameof(sourceDpn));
            }

            var newDPN = (DataPetriNet)sourceDpn.Clone();
            var dpnTransitionsToConsider = newDPN.Transitions.ToArray();

            var transitionsPreset = new Dictionary<Transition, List<(Place place, int weight)>>();
            var transitionsPostset = new Dictionary<Transition, List<(Place place, int weight)>>();
            foreach (var transition in newDPN.Transitions)
            {
                transitionsPreset.Add(transition, new List<(Place place, int weight)>());
                transitionsPostset.Add(transition, new List<(Place place, int weight)>());
            }

            foreach (var arc in newDPN.Arcs)
            {
                if (arc.Type == ArcType.PlaceTransition)
                {
                    transitionsPreset[(Transition)arc.Destination].Add(((Place)arc.Source, arc.Weight));
                }
                else
                {
                    transitionsPostset[(Transition)arc.Source].Add(((Place)arc.Destination, arc.Weight));
                }
            }

            lockingPlace = new Place
            {
                Label = "locker",
                Id = "locker",
                IsFinal = true,
                Tokens = 1
            };
            newDPN.Places.Add(lockingPlace);

            foreach (var sourceTransition in dpnTransitionsToConsider)
            {
                var transitionInfo = new TransitionInfo(
                    sourceTransition,
                    transitionsPreset[sourceTransition],
                    transitionsPostset[sourceTransition]);

                TryExpand(newDPN, transitionInfo);
            }

            return newDPN;
        }

        private void TryExpand(DataPetriNet dpn, TransitionInfo transitionInfo)
        {
            var overallTransitionIndex = 0;

            if (transitionInfo == null)
            {
                throw new ArgumentNullException(nameof(transitionInfo));
            }
            if (dpn == null)
            {
                throw new ArgumentNullException(nameof(dpn));
            }

            var transitionConstraint = transitionInfo.Transition.Guard.BaseConstraintExpressions;

            if (transitionConstraint.Count <= 1)
            {
                dpn.Arcs.Add(new Arc(lockingPlace, transitionInfo.Transition));
                dpn.Arcs.Add(new Arc(transitionInfo.Transition, lockingPlace));
                return;
            }

            var intermediaryArcWeight = transitionInfo.Preset.Sum(x => x.weight);

            // Отсортировать конъюнкты так, чтобы операции записи были в конце

            var sortedExpressions = GetSortedExpressions(transitionConstraint);

            foreach (var expressionList in sortedExpressions)
            {
                bool lastExpression = false;
                var currentIndex = 0;
                do
                {
                    var needPreset = currentIndex == 0;
                    var needPostset = expressionList[currentIndex].ConstraintVariable.VariableType == VariableType.Written ||
                                        currentIndex == expressionList.Count - 1;

                    var guard = new Guard(dpn.Context, GetConditions(expressionList, currentIndex));
                    var newTransition =
                        new Transition(transitionInfo.Transition.Id + "_st_" + overallTransitionIndex++.ToString(), guard);
                    
                    dpn.Transitions.Add(newTransition);

                    if (needPreset)
                    {
                        foreach (var presetPlace in transitionInfo.Preset)
                        {
                            dpn.Arcs.Add(new Arc(presetPlace.place, newTransition, presetPlace.weight));
                        }
                        dpn.Arcs.Add(new Arc(lockingPlace, newTransition));
                    }
                    else
                    {
                        dpn.Arcs.Add(new Arc(dpn.Places.Last(), newTransition, intermediaryArcWeight));
                    }

                    if (needPostset)
                    {
                        foreach (var postsetPlace in transitionInfo.Postset)
                        {
                            dpn.Arcs.Add(new Arc(newTransition, postsetPlace.place, postsetPlace.weight));
                        }
                        dpn.Arcs.Add(new Arc(newTransition, lockingPlace));
                        lastExpression = true;
                    }
                    else
                    {
                        var intermediaryPlace = new Place("ap_" + overallPlaceIndex++.ToString(), PlaceType.Intermediary);
                        dpn.Places.Add(intermediaryPlace);
                        dpn.Arcs.Add(new Arc(newTransition, intermediaryPlace));
                        //previousPlace = intermediaryPlace;
                        currentIndex++;
                    }


                } while (!lastExpression);
            }

            dpn.Transitions.Remove(transitionInfo.Transition);
            dpn.Arcs.RemoveAll(x => x.Destination == transitionInfo.Transition || x.Source == transitionInfo.Transition);
        }

        private static List<IConstraintExpression> GetConditions(List<IConstraintExpression> expressionList, int currentIndex)
        {
            if (expressionList[currentIndex].ConstraintVariable.VariableType == VariableType.Read)
            {
                var addedExpression = expressionList[currentIndex];
                addedExpression.LogicalConnective = LogicalConnective.Empty;
                return new List<IConstraintExpression> { addedExpression };
            }
            else
            {
                var addedExpressions = expressionList.Skip(currentIndex).ToList();
                addedExpressions[0].LogicalConnective = LogicalConnective.Empty;
                return addedExpressions;
            }
        }

        private List<List<IConstraintExpression>> GetSortedExpressions(List<IConstraintExpression> expression)
        {
            var resultList = new List<List<IConstraintExpression>>();
            resultList.Add(new List<IConstraintExpression>());
            var currentIndex = 0;

            foreach (var expressionItem in expression)
            {
                if (expressionItem.LogicalConnective != LogicalConnective.Or)
                {
                    if (expressionItem.ConstraintVariable.VariableType == VariableType.Written)
                    {
                        resultList[currentIndex].Add(expressionItem.Clone());
                    }
                    else
                    {
                        resultList[currentIndex].Insert(0, expressionItem.Clone());
                    }
                }
                else
                {
                    resultList.Add(new List<IConstraintExpression> { expressionItem.Clone() });
                    currentIndex++;
                }
            }

            for (int i = 0; i < resultList.Count; i++)
            {

            }

            return resultList;
        }
    }
}