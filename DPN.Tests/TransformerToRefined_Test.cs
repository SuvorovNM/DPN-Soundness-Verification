using DPN.Models;
using DPN.Models.DPNElements;
using DPN.Models.Extensions;
using DPN.Parsers;
using DPN.Soundness;
using DPN.Soundness.Transformations;
using DPN.Soundness.TransitionSystems;
using FluentAssertions;
using Microsoft.Z3;

namespace DPN.Tests;

public class TransformerToRefined_Test
{
	[Test]
	public void TransformWhenNoRefinedTransitionsCanBeAdded_Should_ReturnTheSameModel()
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\Unbounded.pnmlx", FileMode.Open);
		using var context = new Context();
		var unboundedDpn = pnmlxParser.Deserialize(fs, context);

		var transformerToRefined = new TransformerToRefined();
		var transformedDpn = transformerToRefined.Transform(unboundedDpn, new Dictionary<string, string>());

		transformedDpn.RefinedDpn.Should().BeEquivalentTo(unboundedDpn);

		var baseDpnStateSpace = StateSpaceConstructor.ConstructCoverabilityGraph(unboundedDpn, false, false);
		transformedDpn.StateSpaceStructure.Should().BeEquivalentTo(baseDpnStateSpace, o => o.Excluding(ss => ss.Nodes).Excluding(ss => ss.Arcs));
		transformedDpn.StateSpaceStructure.Nodes.Should().BeEquivalentTo(baseDpnStateSpace.Nodes, o => o.Excluding(ss => ss.Id));
		transformedDpn.StateSpaceStructure.Arcs.Should().BeEquivalentTo(baseDpnStateSpace.Arcs, o => o.Excluding(ss => ss.SourceNodeId).Excluding(ss => ss.TargetNodeId));
	}

	[Test]
	public void TransformWhenSingleTransitionRefinedOnce_Should_ReturnTransformedModel()
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\Livelock.pnmlx", FileMode.Open);
		using var context = new Context();
		var livelockDpn = pnmlxParser.Deserialize(fs, context);

		var transformerToRefined = new TransformerToRefined();
		var transformedDpn = transformerToRefined.Transform(livelockDpn, new Dictionary<string, string>());

		transformedDpn.RefinedDpn.Should().BeEquivalentTo(livelockDpn, o => o.Excluding(dpn => dpn.Transitions).Excluding(dpn => dpn.Arcs));

		var baseTransition = livelockDpn.Transitions.Single(t => t.Id == "t1");
		transformedDpn.RefinedDpn.Transitions.Where(t => t.BaseTransitionId != baseTransition.Id).Should().BeEquivalentTo(livelockDpn.Transitions.Where(t => t.Id != baseTransition.Id));
		transformedDpn.RefinedDpn.Arcs.Where(a => !a.Destination.Id.Contains(baseTransition.Id) && !a.Source.Id.Contains(baseTransition.Id)).Should()
			.BeEquivalentTo(livelockDpn.Arcs.Where(a => a.Destination.Id != baseTransition.Id && a.Source.Id != baseTransition.Id));

		var refinedTransitions = transformedDpn.RefinedDpn.Transitions.Where(t => t.IsSplit).ToArray();
		refinedTransitions.Should().HaveCount(2);
		refinedTransitions.All(t => t.BaseTransitionId == baseTransition.Id).Should().BeTrue();
		VerifyArcs(baseTransition, refinedTransitions.ToArray(), livelockDpn, transformedDpn);

		context.AreEqual(context.MkOr(refinedTransitions.Select(t => t.Guard.ActualConstraintExpression)), baseTransition.Guard.ActualConstraintExpression).Should().BeTrue();
		context.CanBeSatisfied(context.MkAnd(refinedTransitions.Select(t => t.Guard.ActualConstraintExpression))).Should().BeFalse();
	}

	[Test]
	public void TransformWhenSingleTransitionsRefinedMoreThanOnce_Should_ReturnTransformedModel()
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\HospitalBilling.pnmlx", FileMode.Open);
		using var context = new Context();
		var hospitalBillingDpn = pnmlxParser.Deserialize(fs, context);

		var transformerToRefined = new TransformerToRefined();
		var transformedDpn = transformerToRefined.Transform(hospitalBillingDpn, new Dictionary<string, string>());

		transformedDpn.RefinedDpn.Should().BeEquivalentTo(hospitalBillingDpn, o => o.Excluding(dpn => dpn.Transitions).Excluding(dpn => dpn.Arcs));
		var baseTransition = hospitalBillingDpn.Transitions.Single(t => t.Id == "n47");
		transformedDpn.RefinedDpn.Transitions.Where(t => t.BaseTransitionId != baseTransition.Id).Should().BeEquivalentTo(hospitalBillingDpn.Transitions.Where(t => t.Id != baseTransition.Id));
		transformedDpn.RefinedDpn.Arcs.Where(a => !a.Destination.Id.Contains(baseTransition.Id) && !a.Source.Id.Contains(baseTransition.Id)).Should()
			.BeEquivalentTo(hospitalBillingDpn.Arcs.Where(a => a.Destination.Id != baseTransition.Id && a.Source.Id != baseTransition.Id));

		var refinedTransitions = transformedDpn.RefinedDpn.Transitions.Where(t => t.IsSplit && t.BaseTransitionId == baseTransition.Id).ToArray();
		refinedTransitions.Should().HaveCount(9);
		context.AreEqual(context.MkOr(refinedTransitions.Select(t => t.Guard.ActualConstraintExpression)), baseTransition.Guard.ActualConstraintExpression).Should().BeTrue();
		context.CanBeSatisfied(context.MkAnd(refinedTransitions.Select(t => t.Guard.ActualConstraintExpression))).Should().BeFalse();
		
		VerifyArcs(baseTransition, refinedTransitions.ToArray(), hospitalBillingDpn, transformedDpn);
	}

	[Test]
	public void TransformWhenMultipleTransitionsRefined_Should_ReturnTransformedModel()
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\LivelockWithMultipleTransitionsToRefine.pnmlx", FileMode.Open);
		using var context = new Context();
		var livelockDpn = pnmlxParser.Deserialize(fs, context);

		var transformerToRefined = new TransformerToRefined();
		var transformedDpn = transformerToRefined.Transform(livelockDpn, new Dictionary<string, string>());

		transformedDpn.RefinedDpn.Should().BeEquivalentTo(livelockDpn, o => o.Excluding(dpn => dpn.Transitions).Excluding(dpn => dpn.Arcs));
		var t11 = livelockDpn.Transitions.Single(t => t.Id == "t1_1");
		var t12 = livelockDpn.Transitions.Single(t => t.Id == "t1_2");
		transformedDpn.RefinedDpn.Transitions.Where(t => t.BaseTransitionId != t11.Id && t.BaseTransitionId != t12.Id).Should()
			.BeEquivalentTo(livelockDpn.Transitions.Where(t => t.Id != t11.Id && t.Id != t12.Id));
		transformedDpn.RefinedDpn.Arcs.Where(a => !a.Destination.Id.Contains(t11.Id) && !a.Source.Id.Contains(t11.Id) && !a.Destination.Id.Contains(t12.Id) && !a.Source.Id.Contains(t12.Id)).Should()
			.BeEquivalentTo(livelockDpn.Arcs.Where(a => a.Destination.Id != t11.Id && a.Source.Id != t11.Id &&  a.Source.Id != t12.Id && a.Destination.Id != t12.Id));
		
		transformedDpn.RefinedDpn.Transitions.Where(t => !t.IsSplit).Should().HaveCount(2);
		transformedDpn.RefinedDpn.Transitions.Where(t => t.IsSplit).Should().HaveCount(4);
		
		var t11Refined = transformedDpn.RefinedDpn.Transitions.Where(t => t.BaseTransitionId == t11.Id).ToArray();
		var t12Refined = transformedDpn.RefinedDpn.Transitions.Where(t => t.BaseTransitionId == t12.Id).ToArray();

		t11Refined.Should().HaveCount(2);
		t12Refined.Should().HaveCount(2);

		context.AreEqual(context.MkOr(t11Refined.Select(t => t.Guard.ActualConstraintExpression)), t11.Guard.ActualConstraintExpression).Should().BeTrue();
		context.CanBeSatisfied(context.MkAnd(t11Refined.Select(t => t.Guard.ActualConstraintExpression))).Should().BeFalse();
		context.AreEqual(context.MkOr(t12Refined.Select(t => t.Guard.ActualConstraintExpression)), t12.Guard.ActualConstraintExpression).Should().BeTrue();
		context.CanBeSatisfied(context.MkAnd(t12Refined.Select(t => t.Guard.ActualConstraintExpression))).Should().BeFalse();

		VerifyArcs(t11, t11Refined, livelockDpn, transformedDpn);
		VerifyArcs(t12, t12Refined, livelockDpn, transformedDpn);
	}

	private static void VerifyArcs(Transition baseTransition, Transition[] refinedTransitions, DataPetriNet baseDpn, RefinementResult transformedDpn)
	{
		foreach (var refinedTransition in refinedTransitions)
		{
			transformedDpn.RefinedDpn.Arcs.Where(a => a.Source.Id == refinedTransition.Id).Should()
				.BeEquivalentTo(baseDpn.Arcs.Where(a => a.Source.Id == baseTransition.Id), o => o.Excluding(a => a.Source));
			transformedDpn.RefinedDpn.Arcs.Where(a => a.Destination.Id == refinedTransition.Id).Should()
				.BeEquivalentTo(baseDpn.Arcs.Where(a => a.Destination.Id == baseTransition.Id), o => o.Excluding(a => a.Destination));
		}
	}
}