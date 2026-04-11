using System.Diagnostics;
using DPN.Models;
using DPN.Soundness.Transformations;
using DPN.Soundness.TransitionSystems;
using DPN.Soundness.TransitionSystems.Converters;
using DPN.Soundness.TransitionSystems.Coverability;
using DPN.Soundness.TransitionSystems.Reachability;
using static DPN.Soundness.Transformations.RefinementSettingsConstants;

namespace DPN.Soundness.Verification;

public static class ClassicalVerificationSettingsConstants
{
	public const string AlgorithmVersion = nameof(AlgorithmVersion);
	public const string DirectVersion = nameof(DirectVersion);
	public const string ImprovedVersion = nameof(ImprovedVersion); // TODO: rename
	public const string ConstructFullGraph = nameof(ConstructFullGraph);
}

public class ClassicalSoundnessVerifier : ISoundnessVerifier
{
	public VerificationResult Verify(DataPetriNet dpn, Dictionary<string, string> verificationSettings)
	{
		var constructFullGraph = false;
		if (verificationSettings.TryGetValue(ClassicalVerificationSettingsConstants.ConstructFullGraph, out var constructFullGraphAsString))
		{
			constructFullGraph = bool.Parse(constructFullGraphAsString);
		}
		
		verificationSettings.TryGetValue(ClassicalVerificationSettingsConstants.AlgorithmVersion, out var algorithmVersion);

		if (algorithmVersion is ClassicalVerificationSettingsConstants.ImprovedVersion)
		{
			return VerifyImproved(dpn, constructFullGraph);
		}

		if (algorithmVersion is ClassicalVerificationSettingsConstants.DirectVersion or null)
		{
			return VerifyClassical(dpn, constructFullGraph);
		}

		throw new ArgumentException($"{nameof(ClassicalSoundnessVerifier)} does not support version {algorithmVersion}");
	}

	private static VerificationResult VerifyClassical(DataPetriNet dpn, bool constructFullGraph)
	{
		var stopWatch = Stopwatch.StartNew();
		var dpnTransformation = new TransformerToRefined();
		var (refinedDpn, stateSpace) = dpnTransformation.TransformAndReturnLts(
			dpn,
			new Dictionary<string, string>
			{
				{ BaseStructure, constructFullGraph ? RefinementSettingsConstants.CoverabilityGraph : FiniteReachabilityGraph }
			},
			out var lts);

		SoundnessProperties soundnessProperties;
		if (stateSpace.IsFullGraph)
		{
			var canExtendLts = refinedDpn.Transitions.Count == dpn.Transitions.Count;
			var tauRefinedStateSpace = GetTauStateSpace(constructFullGraph, refinedDpn);

			if (canExtendLts)
			{
				tauRefinedStateSpace.GenerateGraph(lts);
			}
			else
			{
				tauRefinedStateSpace.GenerateGraph();
			}

			soundnessProperties = ClassicalSoundnessAnalyzer.CheckSoundness(dpn, tauRefinedStateSpace);
			stopWatch.Stop();
			return new VerificationResult(
				ToStateSpaceConverter.Convert(tauRefinedStateSpace),
				soundnessProperties,
				canExtendLts ? tauRefinedStateSpace.ConstraintArcs.Count : stateSpace.Arcs.Length + tauRefinedStateSpace.ConstraintArcs.Count,
				refinedDpn.Transitions.Count - dpn.Transitions.Count,
				stopWatch.Elapsed);
		}

		soundnessProperties = ClassicalSoundnessAnalyzer.CheckSoundness(stateSpace);
		stopWatch.Stop();
		return new VerificationResult(
			stateSpace,
			soundnessProperties,
			stateSpace.Arcs.Length,
			refinedDpn.Transitions.Count - dpn.Transitions.Count,
			stopWatch.Elapsed);
	}

	private static VerificationResult VerifyImproved(DataPetriNet dpn, bool constructFullGraph)
	{
		var stopWatch = Stopwatch.StartNew();
		LabeledTransitionSystem lts = constructFullGraph ? new CoverabilityGraph(dpn, true) : new ReachabilityGraph(dpn);
		lts.GenerateGraph();
		var soundnessProperties = ClassicalSoundnessAnalyzer.CheckSoundness(dpn, lts);

		if (!soundnessProperties.Soundness)
		{
			stopWatch.Stop();
			return new VerificationResult(
				ToStateSpaceConverter.Convert(lts),
				soundnessProperties,
				lts.ConstraintArcs.Count,
				0,
				stopWatch.Elapsed);
		}

		var tauStateSpace = GetTauStateSpace(constructFullGraph, dpn);
		tauStateSpace.GenerateGraph(lts);
		soundnessProperties = ClassicalSoundnessAnalyzer.CheckSoundness(dpn, tauStateSpace);

		if (soundnessProperties.Soundness)
		{
			var dpnTransformation = new TransformerToRefined();
			var (refinedDpn, stateSpace) = dpnTransformation.Transform(
				dpn,
				lts);

			if (refinedDpn.Transitions.Count == dpn.Transitions.Count)
			{
				stopWatch.Stop();
				return new VerificationResult(
					ToStateSpaceConverter.Convert(tauStateSpace),
					soundnessProperties,
					tauStateSpace.ConstraintArcs.Count,
					0,
					stopWatch.Elapsed);
			}

			var tauRefinedStateSpace = GetTauStateSpace(constructFullGraph, refinedDpn);
			tauRefinedStateSpace.GenerateGraph();

			soundnessProperties = ClassicalSoundnessAnalyzer.CheckSoundness(dpn, tauRefinedStateSpace);
			stopWatch.Stop();
			return new VerificationResult(
				ToStateSpaceConverter.Convert(tauRefinedStateSpace),
				soundnessProperties,
				tauStateSpace.ConstraintArcs.Count + tauRefinedStateSpace.ConstraintArcs.Count,
				refinedDpn.Transitions.Count - dpn.Transitions.Count,
				stopWatch.Elapsed);
		}

		stopWatch.Stop();
		return new VerificationResult(
			ToStateSpaceConverter.Convert(tauStateSpace),
			soundnessProperties,
			tauStateSpace.ConstraintArcs.Count,
			0,
			stopWatch.Elapsed);
	}

	private static LabeledTransitionSystem GetTauStateSpace(bool constructFullGraph, DataPetriNet dpn)
	{
		return constructFullGraph
			? new CoverabilityGraph(dpn, true, false, false, true)
			: new ConstraintGraph(dpn);
	}
}