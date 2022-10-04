using DataPetriNetOnSmt;
using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.DPNElements;
using DataPetriNetOnSmt.Enums;

namespace DataPetriNetTransformation
{
    internal class TransitionInfo
    {
        public Transition Transition { get; set; }
        public List<Place> Preset { get; set; }
        public List<Place> Postset { get; set; }
        public TransitionInfo(Transition transition, List<Place> preset, List<Place> postset)
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

    public class TransformationToAtomicConstraints : ITransformation
    {
        private int overallPlaceIndex = 0;

        public DataPetriNet Transform(DataPetriNet sourceDpn)
        {
            if (sourceDpn == null)
            {
                throw new ArgumentNullException(nameof(sourceDpn));
            }

            var newDPN = (DataPetriNet)sourceDpn.Clone();
            var dpnTransitionsToConsider = newDPN.Transitions.ToArray();

            var transitionsPreset = new Dictionary<Transition, List<Place>>();
            var transitionsPostset = new Dictionary<Transition, List<Place>>();
            foreach(var transition in newDPN.Transitions)
            {
                transitionsPreset.Add(transition, new List<Place>());
                transitionsPostset.Add(transition, new List<Place>());
            }

            foreach(var arc in newDPN.Arcs)
            {
                if (arc.Type == ArcType.PlaceTransition)
                {
                    transitionsPreset[(Transition)arc.Destination].Add((Place)arc.Source);
                }
                else
                {
                    transitionsPostset[(Transition)arc.Source].Add((Place)arc.Destination);
                }
            }

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

            var transitionConstraint = transitionInfo.Transition.Guard.ConstraintExpressions;

            if (transitionConstraint.Count <= 1)
            {
                return;
            }

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

                    var newTransition =
                        new Transition(transitionInfo.Transition.Id +"_" + overallTransitionIndex++.ToString());

                    newTransition.Guard = new Guard
                    {
                        ConstraintExpressions = GetConditions(expressionList, currentIndex)
                    };
                    dpn.Transitions.Add(newTransition);

                    if (needPreset)
                    {
                        foreach (var presetPlace in transitionInfo.Preset)
                        {
                            dpn.Arcs.Add(new Arc(presetPlace, newTransition));
                        }
                    }
                    else
                    {
                        dpn.Arcs.Add(new Arc(dpn.Places.Last(), newTransition));
                    }

                    if (needPostset)
                    {
                        foreach (var postsetPlace in transitionInfo.Postset)
                        {
                            dpn.Arcs.Add(new Arc(newTransition, postsetPlace));
                        }
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