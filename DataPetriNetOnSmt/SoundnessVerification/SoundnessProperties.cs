using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;

namespace DataPetriNetOnSmt.SoundnessVerification;

public record SoundnessProperties(
    SoundnessType SoundnessType,
    Dictionary<AbstractState, ConstraintStateType> StateTypes,
    bool Boundedness,
    string[] DeadTransitions,
    bool Deadlocks,
    bool Soundness);