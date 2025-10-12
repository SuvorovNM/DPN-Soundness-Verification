using DPN.Models.Enums;

namespace DPN.Soundness;

public record SoundnessProperties(
    SoundnessType SoundnessType,
    Dictionary<int, ConstraintStateType> StateTypes,
    bool Boundedness,
    string[] DeadTransitions,
    bool Deadlocks,
    bool Soundness);