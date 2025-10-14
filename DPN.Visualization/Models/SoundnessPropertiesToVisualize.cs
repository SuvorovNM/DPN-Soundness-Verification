namespace DPN.Visualization.Models;

public class SoundnessPropertiesToVisualize(
	bool boundedness,
	string[] deadTransitions,
	bool? classicalSoundness,
	bool? relaxedLazySoundness)
{
	public bool Boundedness { get; init; } = boundedness;
    public string[] DeadTransitions { get; } = deadTransitions;
    public bool? ClassicalSoundness { get; } = classicalSoundness;
    public bool? RelaxedLazySoundness { get; } = relaxedLazySoundness;
}