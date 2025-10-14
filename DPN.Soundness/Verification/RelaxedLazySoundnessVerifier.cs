using System.Diagnostics;
using DPN.Models;
using DPN.Soundness.TransitionSystems.Converters;
using DPN.Soundness.TransitionSystems.Coverability;

namespace DPN.Soundness.Verification;

public static class RelaxedLazyVerificationSettingsConstants
{
	public const string BaseStructure = nameof(BaseStructure);
	public const string CoverabilityGraph = nameof(CoverabilityGraph);
	public const string CoverabilityTree = nameof(CoverabilityTree);

	public const string StopOnCoveringFinalPosition = nameof(StopOnCoveringFinalPosition);
	public const string True = nameof(True);
	public const string False = nameof(False);
}

public class RelaxedLazySoundnessVerifier : ISoundnessVerifier
{
	public VerificationResult Verify(DataPetriNet dpn, Dictionary<string, string> verificationSettings)
	{
		var stopWatch = Stopwatch.StartNew();
		verificationSettings.TryGetValue(RelaxedLazyVerificationSettingsConstants.BaseStructure, out var baseStructure);

		var stopOnCoveringFinalPosition = true;
		if (verificationSettings.TryGetValue(RelaxedLazyVerificationSettingsConstants.StopOnCoveringFinalPosition, out var stopOnCoveringFinalPositionString))
		{
			if (!bool.TryParse(stopOnCoveringFinalPositionString, out stopOnCoveringFinalPosition))
			{
				throw new ArgumentException($"Invalid value for parameter {nameof(RelaxedLazyVerificationSettingsConstants.StopOnCoveringFinalPosition)}");
			}
		}

		if (baseStructure is RelaxedLazyVerificationSettingsConstants.CoverabilityGraph or null)
		{
			var cg = new CoverabilityGraph(dpn, stopOnCoveringFinalPosition);
			cg.GenerateGraph();
			var soundnessProperties = RelaxedLazySoundnessAnalyzer.CheckSoundness(dpn, cg);

			stopWatch.Stop();
			return new VerificationResult(ToStateSpaceConverter.Convert(cg), soundnessProperties, stopWatch.Elapsed);
		}

		if (baseStructure == RelaxedLazyVerificationSettingsConstants.CoverabilityTree)
		{
			var ct = new CoverabilityTree(dpn, stopOnCoveringFinalPosition);
			ct.GenerateGraph();
			var soundnessProperties = RelaxedLazySoundnessAnalyzer.CheckSoundness(dpn, ct);

			stopWatch.Stop();
			return new VerificationResult(ToStateSpaceConverter.Convert(ct), soundnessProperties, stopWatch.Elapsed);
		}

		throw new ArgumentException($"{nameof(RelaxedLazySoundnessVerifier)} does not support base structure {baseStructure}");
	}
}