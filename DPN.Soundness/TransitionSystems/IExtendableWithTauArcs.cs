using DPN.Soundness.TransitionSystems.Reachability;

namespace DPN.Soundness.TransitionSystems;

internal interface IExtendableWithTauArcs
{
	void GenerateGraph(LabeledTransitionSystem baseLts);
}