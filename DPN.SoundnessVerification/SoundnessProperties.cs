using DPN.Models.Enums;
using DPN.SoundnessVerification.TransitionSystems;

namespace DPN.SoundnessVerification;

public record SoundnessProperties(
    SoundnessType SoundnessType,
    Dictionary<int, ConstraintStateType> StateTypes,
    bool Boundedness,
    string[] DeadTransitions,
    bool Deadlocks,
    bool Soundness);