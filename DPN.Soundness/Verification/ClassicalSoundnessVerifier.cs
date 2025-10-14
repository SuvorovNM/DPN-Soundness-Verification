using System.Diagnostics;
using DPN.Models;
using DPN.Soundness.Transformations;
using DPN.Soundness.TransitionSystems.Converters;
using DPN.Soundness.TransitionSystems.Reachability;

namespace DPN.Soundness.Verification;

public static class ClassicalVerificationSettingsConstants
{
	public const string AlgorithmVersion = nameof(AlgorithmVersion);
	public const string DirectVersion = nameof(DirectVersion);
	public const string ImprovedVersion = nameof(ImprovedVersion);
}

public class ClassicalSoundnessVerifier : ISoundnessVerifier
{
	public VerificationResult Verify(DataPetriNet dpn, Dictionary<string, string> verificationSettings)
	{
		verificationSettings.TryGetValue(ClassicalVerificationSettingsConstants.AlgorithmVersion, out var algorithmVersion);

		if (algorithmVersion == ClassicalVerificationSettingsConstants.ImprovedVersion)
		{
			return VerifyImproved(dpn);
		}

		if (algorithmVersion is ClassicalVerificationSettingsConstants.DirectVersion or null)
		{
			return VerifyClassical(dpn);
		}

		throw new ArgumentException($"{nameof(ClassicalSoundnessVerifier)} does not support version {algorithmVersion}");
	}

	private static VerificationResult VerifyClassical(DataPetriNet dpn)
	{
		var stopWatch = Stopwatch.StartNew();
		var dpnTransformation = new TransformerToRefined();
		var (refinedDpn, stateSpace) = dpnTransformation.Transform(
			dpn,
			new Dictionary<string, string>
			{
				{RefinementSettingsConstants.BaseStructure, RefinementSettingsConstants.FiniteReachabilityGraph}
			});
		
		SoundnessProperties soundnessProperties;
		if (stateSpace.IsFullGraph)
		{
			var constraintGraph = new ConstraintGraph(refinedDpn);
			constraintGraph.GenerateGraph();
			soundnessProperties = ClassicalSoundnessAnalyzer.CheckSoundness(dpn, constraintGraph);
			stopWatch.Stop();
			return new VerificationResult(ToStateSpaceConverter.Convert(constraintGraph), soundnessProperties, stopWatch.Elapsed);
		}

		soundnessProperties = ClassicalSoundnessAnalyzer.CheckSoundness(stateSpace);
		stopWatch.Stop();
		return new VerificationResult(stateSpace, soundnessProperties, stopWatch.Elapsed);
	}

	private static VerificationResult VerifyImproved(DataPetriNet dpn)
	{
		var stopWatch = Stopwatch.StartNew();
		var lts = new ReachabilityGraph(dpn);
		lts.GenerateGraph();
		var soundnessProperties = ClassicalSoundnessAnalyzer.CheckSoundness(dpn, lts);

		if (!soundnessProperties.Soundness)
		{
			stopWatch.Stop();
			return new VerificationResult(ToStateSpaceConverter.Convert(lts), soundnessProperties,  stopWatch.Elapsed);
		}

		var cg = new ConstraintGraph(dpn);
		cg.GenerateGraph();
		soundnessProperties = ClassicalSoundnessAnalyzer.CheckSoundness(dpn, cg);
		stopWatch.Stop();

		if (soundnessProperties.Soundness)
		{
			var verificationResult = VerifyClassical(dpn);
			return new VerificationResult(verificationResult.StateSpaceGraph, verificationResult.SoundnessProperties, stopWatch.Elapsed + verificationResult.VerificationTime);
		}

		return new VerificationResult(ToStateSpaceConverter.Convert(cg), soundnessProperties, stopWatch.Elapsed);
	}
}