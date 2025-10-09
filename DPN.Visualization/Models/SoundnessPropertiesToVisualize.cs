using DPN.SoundnessVerification;

namespace DPN.Visualization.Models;

public class SoundnessPropertiesToVisualize
{
    public SoundnessPropertiesToVisualize(
        bool boundedness,
        string[] deadTransitions,
        bool? classicalSoundness,
        bool? relaxedLazySoundness)
    {
        Boundedness = boundedness;
        DeadTransitions = deadTransitions;
        ClassicalSoundness = classicalSoundness;
        RelaxedLazySoundness = relaxedLazySoundness;
    }

    public bool Boundedness { get; init; }
    public string[] DeadTransitions { get; init; }
    public bool? ClassicalSoundness { get; init; }
    public bool? RelaxedLazySoundness { get; init; }
    public bool Soundness => ClassicalSoundness.HasValue 
        ? ClassicalSoundness.Value 
        : RelaxedLazySoundness.HasValue
            ? RelaxedLazySoundness.Value
            : throw new ArgumentException("Cannot determine whether the model is sound");

    public static SoundnessPropertiesToVisualize FromSoundnessProperties(
        params SoundnessProperties[] soundnessProperties)
    {
        if (soundnessProperties.Length == 0)
        {
            throw new ArgumentException("Must have at least one soundness property", nameof(soundnessProperties));
        }

        if (soundnessProperties.Length == 1)
        {
            bool? classicalSoundness = soundnessProperties[0].SoundnessType == SoundnessType.Classical
                ? soundnessProperties[0].Soundness
                : null;
            bool? relaxedLazySoundness = soundnessProperties[0].SoundnessType == SoundnessType.RelaxedLazy
                ? soundnessProperties[0].Soundness
                : null;

            return new SoundnessPropertiesToVisualize(
                soundnessProperties[0].Boundedness,
                soundnessProperties[0].DeadTransitions,
                classicalSoundness,
                relaxedLazySoundness);
        }

        if (soundnessProperties.Length == 2)
        {
            var (classicalSoundnessProperties, relaxedLazySoundness) =
                soundnessProperties[0].SoundnessType == SoundnessType.Classical
                    ? (soundnessProperties[0], soundnessProperties[1])
                    : (soundnessProperties[1], soundnessProperties[0]);

            return new SoundnessPropertiesToVisualize(
                classicalSoundnessProperties.Boundedness,
                classicalSoundnessProperties.DeadTransitions,
                classicalSoundnessProperties.Soundness,
                relaxedLazySoundness.Soundness);
        }

        throw new ArgumentException("Too many soundness properties", nameof(soundnessProperties));
    }
}