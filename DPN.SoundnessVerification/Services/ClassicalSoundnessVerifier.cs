using System.Diagnostics;
using DPN.Models;
using DPN.SoundnessVerification.TransitionSystems;
using DPN.SoundnessVerification.TransitionSystems.Converters;

namespace DPN.SoundnessVerification.Services;

public class ClassicalSoundnessVerifier : ISoundnessVerifier
{
	public VerificationResult Verify(DataPetriNet dpn, Dictionary<string, string> verificationSettings)
	{
		verificationSettings.TryGetValue(VerificationSettingsConstants.AlgorithmVersion, out var algorithmVersion);

		if (algorithmVersion == VerificationSettingsConstants.ImprovedVersion)
		{
			return VerifyImproved(dpn);
		}

		if (algorithmVersion is VerificationSettingsConstants.DirectVersion or null)
		{
			return VerifyClassical(dpn);
		}

		throw new ArgumentException($"{nameof(ClassicalSoundnessVerifier)} does not support version {algorithmVersion}");
	}

	private static VerificationResult VerifyClassical(DataPetriNet dpn)
	{
		var stopWatch = Stopwatch.StartNew();
		var dpnTransformation = new TransformerToRefined();
		var (refinedDpn, lts) = dpnTransformation.TransformUsingLts(dpn);

		SoundnessProperties soundnessProperties;
		if (lts.IsFullGraph)
		{
			var constraintGraph = new ConstraintGraph(refinedDpn);
			constraintGraph.GenerateGraph();
			soundnessProperties = SoundnessAnalyzer.CheckSoundness(dpn, constraintGraph);
			stopWatch.Stop();
			return new VerificationResult(ToStateSpaceConverter.Convert(constraintGraph), soundnessProperties, stopWatch.Elapsed);
		}

		soundnessProperties = SoundnessAnalyzer.CheckSoundness(dpn, lts);
		stopWatch.Stop();
		return new VerificationResult(ToStateSpaceConverter.Convert(lts), soundnessProperties, stopWatch.Elapsed);
	}

	private static VerificationResult VerifyImproved(DataPetriNet dpn)
	{
		var stopWatch = Stopwatch.StartNew();
		var lts = new ReachabilityGraph(dpn);
		lts.GenerateGraph();
		var soundnessProperties = SoundnessAnalyzer.CheckSoundness(dpn, lts);

		if (!soundnessProperties.Soundness)
		{
			stopWatch.Stop();
			return new VerificationResult(ToStateSpaceConverter.Convert(lts), soundnessProperties,  stopWatch.Elapsed);
		}

		var cg = new ConstraintGraph(dpn);
		cg.GenerateGraph();
		soundnessProperties = SoundnessAnalyzer.CheckSoundness(dpn, cg);
		stopWatch.Stop();

		if (soundnessProperties.Soundness)
		{
			var verificationResult = VerifyClassical(dpn);
			return new VerificationResult(verificationResult.StateSpaceAbstraction, verificationResult.SoundnessProperties, stopWatch.Elapsed + verificationResult.VerificationTime);
		}

		return new VerificationResult(ToStateSpaceConverter.Convert(cg), soundnessProperties, stopWatch.Elapsed);
	}

	public static class VerificationSettingsConstants
	{
		public const string AlgorithmVersion = nameof(AlgorithmVersion);
		public const string DirectVersion = nameof(DirectVersion);
		public const string ImprovedVersion = nameof(ImprovedVersion);
	}
}