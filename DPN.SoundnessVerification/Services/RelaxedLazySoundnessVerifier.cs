using DPN.Models;
using DPN.SoundnessVerification.TransitionSystems;
using DPN.SoundnessVerification.TransitionSystems.Converters;

namespace DPN.SoundnessVerification.Services;

public class RelaxedLazySoundnessVerifier : ISoundnessVerifier
{
    public VerificationResult Verify(DataPetriNet dpn, Dictionary<string, string> verificationSettings)
    {
	    var cg = new CoverabilityGraph(dpn, stopOnCoveringFinalPosition: true);
	    cg.GenerateGraph();
	    var soundnessProperties = RelaxedLazySoundnessAnalyzer.CheckSoundness(dpn, cg);

	    return new VerificationResult(ToStateSpaceConverter.Convert(cg), soundnessProperties);
    }
}