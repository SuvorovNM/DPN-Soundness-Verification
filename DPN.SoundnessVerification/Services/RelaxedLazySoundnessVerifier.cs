using System.Diagnostics;
using DPN.Models;
using DPN.SoundnessVerification.TransitionSystems;
using DPN.SoundnessVerification.TransitionSystems.Converters;

namespace DPN.SoundnessVerification.Services;

public class RelaxedLazySoundnessVerifier : ISoundnessVerifier
{
    public VerificationResult Verify(DataPetriNet dpn, Dictionary<string, string> verificationSettings)
    {
	    var stopWatch = Stopwatch.StartNew();
	    verificationSettings.TryGetValue(VerificationSettingsConstants.BaseStructure, out var baseStructure);

	    if (baseStructure is VerificationSettingsConstants.CoverabilityGraph or null)
	    {
		    var cg = new CoverabilityGraph(dpn, stopOnCoveringFinalPosition: true);
		    cg.GenerateGraph();
		    var soundnessProperties = RelaxedLazySoundnessAnalyzer.CheckSoundness(dpn, cg);

		    stopWatch.Stop();
		    return new VerificationResult(ToStateSpaceConverter.Convert(cg), soundnessProperties, stopWatch.Elapsed);
	    }

	    if (baseStructure == VerificationSettingsConstants.CoverabilityTree)
	    {
		    var ct = new CoverabilityTree(dpn, stopOnCoveringFinalPosition: true);
		    ct.GenerateGraph();
		    var soundnessProperties = RelaxedLazySoundnessAnalyzer.CheckSoundness(dpn, ct);

		    stopWatch.Stop();
		    return new VerificationResult(ToStateSpaceConverter.Convert(ct), soundnessProperties, stopWatch.Elapsed);
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