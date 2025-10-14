using DPN.Models;
using DPN.Models.Enums;

namespace DPN.Soundness.TransitionSystems.Reachability;

internal class ColoredConstraintGraph(DataPetriNet dataPetriNet) : ConstraintGraph(dataPetriNet)
{
	public Dictionary<LtsState, CtStateColor> StateColorDictionary { get; } = new();

	public override void GenerateGraph()
	{
		base.GenerateGraph();

		AddColors();
	}

	private void AddColors()
	{
		var finalStates = ConstraintStates
			.Where(x => x.Marking.CompareTo(DataPetriNet.FinalMarking) == MarkingComparisonResult.Equal)
			.ToArray();

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