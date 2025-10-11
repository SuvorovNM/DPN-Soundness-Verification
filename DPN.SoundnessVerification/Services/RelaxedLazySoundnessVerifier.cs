using DPN.Models;
using DPN.SoundnessVerification.TransitionSystems;
using DPN.SoundnessVerification.TransitionSystems.Converters;

namespace DPN.SoundnessVerification.Services;

public class RelaxedLazySoundnessVerifier : ISoundnessVerifier
{
    public VerificationResult Verify(DataPetriNet dpn, Dictionary<string, string> verificationSettings)
    {
	    verificationSettings.TryGetValue(VerificationSettingsConstants.BaseStructure, out var baseStructure);

	    if (baseStructure is VerificationSettingsConstants.CoverabilityGraph or null)
	    {
		    var cg = new CoverabilityGraph(dpn, stopOnCoveringFinalPosition: true);
		    cg.GenerateGraph();
		    var soundnessProperties = RelaxedLazySoundnessAnalyzer.CheckSoundness(dpn, cg);

		    return new VerificationResult(ToStateSpaceConverter.Convert(cg), soundnessProperties);
	    }

	    if (baseStructure == VerificationSettingsConstants.CoverabilityTree)
	    {
		    var ct = new CoverabilityTree(dpn);
		    ct.GenerateGraph();
		    var soundnessProperties = RelaxedLazySoundnessAnalyzer.CheckSoundness(dpn, ct);

		    return new VerificationResult(ToStateSpaceConverter.Convert(ct), soundnessProperties);
	    }
	    
        throw new ArgumentException($"{nameof(RelaxedLazySoundnessVerifier)} does not support base structure {baseStructure}");
    }
    
    public static class VerificationSettingsConstants
    {
	    public const string BaseStructure = nameof(BaseStructure);
	    public const string CoverabilityGraph = nameof(CoverabilityGraph);
	    public const string CoverabilityTree = nameof(CoverabilityTree);
    }
}