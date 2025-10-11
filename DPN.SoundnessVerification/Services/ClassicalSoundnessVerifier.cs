using DPN.Models;
using DPN.SoundnessVerification.TransitionSystems;
using DPN.SoundnessVerification.TransitionSystems.Converters;

namespace DPN.SoundnessVerification.Services;

public static class ClassicalSoundnessSettingsConstants
{
    public const string AlgorithmVersion = nameof(AlgorithmVersion);
    public const string DirectVersion = nameof(DirectVersion);
    public const string ImprovedVersion = nameof(ImprovedVersion);
}

public class ClassicalSoundnessVerifier : ISoundnessVerifier
{
    public VerificationResult Verify(DataPetriNet dpn, Dictionary<string, string> verificationSettings)
    {
	    verificationSettings.TryGetValue(ClassicalSoundnessSettingsConstants.AlgorithmVersion, out var algorithmVersion);

        if (algorithmVersion == ClassicalSoundnessSettingsConstants.ImprovedVersion)
        {
            return VerifyImproved(dpn);
        }

        if (algorithmVersion is ClassicalSoundnessSettingsConstants.DirectVersion or null)
        {
	        return VerifyClassical(dpn);
        }
        
        throw new ArgumentException($"{nameof(ClassicalSoundnessVerifier)} does not support version {algorithmVersion}");
    }

    private static VerificationResult VerifyClassical(DataPetriNet dpn)
    {
        var dpnTransformation = new TransformerToRefined();
        var (refinedDpn, _) = dpnTransformation.TransformUsingLts(dpn);

        var cg = new ConstraintGraph(refinedDpn);
        cg.GenerateGraph();
        var soundnessProperties = SoundnessAnalyzer.CheckSoundness(dpn, cg);
        
        return new VerificationResult(ToStateSpaceConverter.Convert(cg), soundnessProperties);
    }

    private static VerificationResult VerifyImproved(DataPetriNet dpn)
    {
        var lts = new ClassicalLabeledTransitionSystem(dpn);
        lts.GenerateGraph();
        var soundnessProperties = SoundnessAnalyzer.CheckSoundness(dpn, lts);

        if (!soundnessProperties.Soundness)
        {
            return new VerificationResult(ToStateSpaceConverter.Convert(lts), soundnessProperties);
        }
            
        var cg = new ConstraintGraph(dpn);
        cg.GenerateGraph();
        soundnessProperties = SoundnessAnalyzer.CheckSoundness(dpn, cg);
                
        return soundnessProperties.Soundness 
            ? VerifyClassical(dpn) 
            : new VerificationResult(ToStateSpaceConverter.Convert(cg), soundnessProperties);
    }
}