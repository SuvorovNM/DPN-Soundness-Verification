using DPN.Models;
using DPN.Models.DPNElements;

namespace DPN.Soundness.Services;

public class TransformerToTau
{
    public DataPetriNet Transform(DataPetriNet sourceDpn)
    {
        var tauTransitions = sourceDpn
            .Transitions
            .Select(t => t.MakeTau())
            .Where(t => t != null);

        var sourceTransitions = sourceDpn
            .Transitions
            .ToDictionary(t => t.Id);

        var incomingArcs = sourceDpn.Arcs
            .GroupBy(a => a.Destination)
            .ToDictionary(a => a.Key, a => a.Select(a => a).ToArray());

        var tauArcs = new List<Arc>(sourceDpn.Arcs.Count);

        foreach (var tauTransition in tauTransitions)
        {
            var sourceTransition = sourceTransitions[tauTransition!.BaseTransitionId];
            if (incomingArcs.TryGetValue(sourceTransition, out var incomingArcsToTransition))
            {
                foreach (var incomingArc in incomingArcsToTransition)
                {
                    tauArcs.Add(new Arc(incomingArc.Source, tauTransition, incomingArc.Weight));
                    tauArcs.Add(new Arc(tauTransition,incomingArc.Source, incomingArc.Weight));
                }
            }
        }


        var resultingDpn = (DataPetriNet)sourceDpn.Clone();
        resultingDpn.Transitions = resultingDpn.Transitions.Union(tauTransitions).ToList()!;
        resultingDpn.Arcs = resultingDpn.Arcs.Union(tauArcs).ToList();

        return resultingDpn;
    }
}