using DPN.Models.Enums;
using DPN.SoundnessVerification.TransitionSystems;

namespace DPN.Visualization.Models;

public class StateToVisualize
{
    public int Id { get; set; }
    public Dictionary<string, int> Tokens { get; set; }
    public string ConstraintFormula { get; set; }
    public ConstraintStateType StateType { get; set; }

    public static StateToVisualize FromNode<AbsState>(AbsState state, ConstraintStateType stateType)
        where AbsState : AbstractState
    {
        return new StateToVisualize
        {
            Id = state.Id,
            ConstraintFormula = state.Constraints.ToString(),
            Tokens = state.Marking.AsDictionary(),
            StateType = stateType
        };
    }
}