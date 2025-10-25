using DPN.Models.Enums;
using DPN.Soundness.TransitionSystems;

namespace DPN.Soundness;

public record SoundnessProperties(
    SoundnessType SoundnessType,
    Dictionary<int, StateType> StateTypes,
    bool Boundedness,
    string[] DeadTransitions,
    bool Deadlocks,
    bool Soundness);