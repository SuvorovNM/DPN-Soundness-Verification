using DPN.Models;
using DPN.Models.Enums;

namespace DPN.SoundnessVerification.TransitionSystems;

public class ColoredConstraintGraph : ConstraintGraph
{
    public Dictionary<LtsState, CtStateColor> StateColorDictionary { get; set; }
    public ColoredConstraintGraph(DataPetriNet dataPetriNet)
        : base(dataPetriNet)
    {
        StateColorDictionary = new Dictionary<LtsState, CtStateColor>();
    }

    public override void GenerateGraph()
    {
        base.GenerateGraph();

        AddColors();
    }

    private void AddColors()
    {
        var finalStates = ConstraintStates
            .Where(x => x.Marking.CompareTo(DataPetriNet.FinalMarking) == MarkingComparisonResult.Equal);

        var statesLeadingToFinals = new List<LtsState>(finalStates);
        var intermediateStates = new List<LtsState>(finalStates);
        var stateIncidenceDict = ConstraintArcs
            .GroupBy(x => x.TargetState)
            .ToDictionary(x => x.Key, y => y.Select(x => x.SourceState).ToList());


        do
        {
            var nextStates = intermediateStates
                .Where(x => stateIncidenceDict.ContainsKey(x))
                .SelectMany(x => stateIncidenceDict[x])
                .Where(x => !statesLeadingToFinals.Contains(x))
                .Distinct();
            statesLeadingToFinals.AddRange(intermediateStates);
            intermediateStates = new List<LtsState>(nextStates);
        } while (intermediateStates.Count > 0);

        var statesNotLeadingToFinals = ConstraintStates
            .Except(statesLeadingToFinals)
            .ToList();

        foreach (var state in statesLeadingToFinals)
        {
            StateColorDictionary[state] = CtStateColor.Green;
        }
        foreach (var state in ConstraintStates.Except(statesLeadingToFinals))
        {
            StateColorDictionary[state] = CtStateColor.Red;
        }
    }
}