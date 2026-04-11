using DPN.Parsers;
using DPN.Soundness.Transformations;
using DPN.Soundness.TransitionSystems;
using FluentAssertions;
using Microsoft.Z3;

namespace DPN.Tests;

public class StateSpaceConstructor_Test
{
	[TestCase(false, false, 5, 5)]
	[TestCase(true, false, 6, 8)]
	[TestCase(false, true, 7, 11)]
	[TestCase(true, true, 10, 21)]
	public void ConstructReachabilityGraph_ForFiniteStateSpace_Should_Construct(bool isTau, bool isRefined, int nodesCount, int arcsCount)
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\Livelock.pnmlx", FileMode.Open);
		using var context = new Context();
		var livelockDpn = pnmlxParser.Deserialize(fs, context);
		
		if (isRefined)
		{
			(livelockDpn,_) = new TransformerToRefined().Transform(livelockDpn, new Dictionary<string, string>());
		}
		
		if (isTau)
		{
			livelockDpn = new TransformerToTau().Transform(livelockDpn);
		}
		
		var stateSpace = StateSpaceConstructor.ConstructReachabilityGraph(livelockDpn);
		stateSpace.IsFullGraph.Should().BeTrue();
		stateSpace.DpnTransitions.Should().BeEquivalentTo(livelockDpn.Transitions);
		stateSpace.FinalDpnMarking.Should().BeEquivalentTo(livelockDpn.FinalMarking);
		stateSpace.TypedVariables.Should().BeEquivalentTo(livelockDpn.Variables.GetAllVariables().ToDictionary(v=>v.name,v=>v.domain));
		
		stateSpace.Nodes.Should().HaveCount(nodesCount);
		stateSpace.Nodes.All(n => n.StateConstraint != null && !n.StateConstraint.IsTrue && n.Marking.Any(p => p.Value > 0)).Should().BeTrue();
		stateSpace.Arcs.Should().HaveCount(arcsCount);

		if (isRefined)
		{
			stateSpace.Arcs.Should().Contain(a => a.BaseTransitionId != a.Label);
		}

		if (isTau)
		{
			stateSpace.Arcs.Should().Contain(a => a.IsSilent);
		}
	}
	
	[Test]
	public void ConstructReachabilityGraph_ForInfiniteStateSpace_Should_ConstructNotFull()
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\Unbounded.pnmlx", FileMode.Open);
		using var context = new Context();
		var unboundedDpn = pnmlxParser.Deserialize(fs, context);
		
		var stateSpace = StateSpaceConstructor.ConstructReachabilityGraph(unboundedDpn);
		stateSpace.IsFullGraph.Should().BeFalse();
		stateSpace.DpnTransitions.Should().BeEquivalentTo(unboundedDpn.Transitions);
		stateSpace.FinalDpnMarking.Should().BeEquivalentTo(unboundedDpn.FinalMarking);
		stateSpace.TypedVariables.Should().BeEquivalentTo(unboundedDpn.Variables.GetAllVariables().ToDictionary(v=>v.name,v=>v.domain));
		
		stateSpace.Nodes.Should().HaveCount(6);
		stateSpace.Nodes.All(n => n.StateConstraint != null && !n.StateConstraint.IsTrue && n.Marking.Any(p => p.Value > 0)).Should().BeTrue();
		stateSpace.Arcs.Should().HaveCount(5);
	}
	
	[TestCase(false, 6, 7)]
	[TestCase(true, 10, 16)]
	public void ConstructConstraintGraph_ForFiniteStateSpace_Should_Construct(bool isRefined, int nodesCount, int arcsCount)
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\Livelock.pnmlx", FileMode.Open);
		using var context = new Context();
		var livelockDpn = pnmlxParser.Deserialize(fs, context);
		
		if (isRefined)
		{
			(livelockDpn,_) = new TransformerToRefined().Transform(livelockDpn, new Dictionary<string, string>());
		}
		
		var stateSpace = StateSpaceConstructor.ConstructConstraintGraph(livelockDpn);
		stateSpace.IsFullGraph.Should().BeTrue();
		stateSpace.DpnTransitions.Where(t=>!t.IsTau).Should().BeEquivalentTo(livelockDpn.Transitions);
		stateSpace.DpnTransitions.Should().ContainSingle(t=>t.IsTau);
		
		stateSpace.FinalDpnMarking.Should().BeEquivalentTo(livelockDpn.FinalMarking);
		stateSpace.TypedVariables.Should().BeEquivalentTo(livelockDpn.Variables.GetAllVariables().ToDictionary(v=>v.name,v=>v.domain));
		
		stateSpace.Nodes.Should().HaveCount(nodesCount);
		stateSpace.Nodes.All(n => n.StateConstraint != null && !n.StateConstraint.IsTrue && n.Marking.Any(p => p.Value > 0)).Should().BeTrue();
		stateSpace.Arcs.Should().HaveCount(arcsCount);
		stateSpace.Arcs.Should().Contain(a => a.IsSilent);

		if (isRefined)
		{
			stateSpace.Arcs.Should().Contain(a => a.BaseTransitionId != a.Label);
		}
		else
		{
			stateSpace.DpnTransitions.Should().ContainSingle(t=>t.IsTau);
		}
	}
	
	[Test]
	public void ConstructConstraintGraph_ForInfiniteStateSpace_Should_ConstructNotFull()
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\Unbounded.pnmlx", FileMode.Open);
		using var context = new Context();
		var unboundedDpn = pnmlxParser.Deserialize(fs, context);
		
		var stateSpace = StateSpaceConstructor.ConstructConstraintGraph(unboundedDpn);
		stateSpace.IsFullGraph.Should().BeFalse();
		stateSpace.DpnTransitions.Should().BeEquivalentTo(unboundedDpn.Transitions);
		stateSpace.FinalDpnMarking.Should().BeEquivalentTo(unboundedDpn.FinalMarking);
		stateSpace.TypedVariables.Should().BeEquivalentTo(unboundedDpn.Variables.GetAllVariables().ToDictionary(v=>v.name,v=>v.domain));
		
		stateSpace.Nodes.Should().HaveCount(6);
		stateSpace.Nodes.All(n => n.StateConstraint != null && !n.StateConstraint.IsTrue && n.Marking.Any(p => p.Value > 0)).Should().BeTrue();
		stateSpace.Arcs.Should().HaveCount(5);
	}
	
	[TestCase(TransitionSystemType.AbstractCoverabilityGraph, 5, 5)]
	[TestCase(TransitionSystemType.AbstractCoverabilityTree, 6, 5)]
	public void ConstructCoverabilityStructure_ForFiniteStateSpace_Should_Construct(TransitionSystemType transitionSystemType, int nodesCount, int arcsCount)
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\Livelock.pnmlx", FileMode.Open);
		using var context = new Context();
		var livelockDpn = pnmlxParser.Deserialize(fs, context);
		
		var stateSpace = transitionSystemType == TransitionSystemType.AbstractCoverabilityGraph
			? StateSpaceConstructor.ConstructCoverabilityGraph(livelockDpn, continueBranchIfUnboundedPlaceFound: true, stopOnCoveringFinalPosition: false)
			: StateSpaceConstructor.ConstructCoverabilityTree(livelockDpn, stopOnCoveringFinalPosition: false);
		stateSpace.IsFullGraph.Should().BeTrue();
		stateSpace.DpnTransitions.Should().BeEquivalentTo(livelockDpn.Transitions);
		stateSpace.FinalDpnMarking.Should().BeEquivalentTo(livelockDpn.FinalMarking);
		stateSpace.TypedVariables.Should().BeEquivalentTo(livelockDpn.Variables.GetAllVariables().ToDictionary(v=>v.name,v=>v.domain));
		
		stateSpace.Nodes.Should().HaveCount(nodesCount);
		stateSpace.Nodes.All(n => n.StateConstraint != null && !n.StateConstraint.IsTrue && n.Marking.Any(p => p.Value > 0)).Should().BeTrue();
		stateSpace.Arcs.Should().HaveCount(arcsCount);
	}
	
	[TestCase(TransitionSystemType.AbstractCoverabilityGraph, 7, 6)]
	[TestCase(TransitionSystemType.AbstractCoverabilityTree, 7, 6)]
	public void ConstructCoverabilityStructure_ForInfiniteStateSpace_Should_ConstructFull(TransitionSystemType transitionSystemType, int nodesCount, int arcsCount)
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\Unbounded.pnmlx", FileMode.Open);
		using var context = new Context();
		var unboundedDpn = pnmlxParser.Deserialize(fs, context);
		
		var stateSpace = transitionSystemType == TransitionSystemType.AbstractCoverabilityGraph
			? StateSpaceConstructor.ConstructCoverabilityGraph(unboundedDpn, continueBranchIfUnboundedPlaceFound: false, stopOnCoveringFinalPosition: false)
			: StateSpaceConstructor.ConstructCoverabilityTree(unboundedDpn, stopOnCoveringFinalPosition: false);
		stateSpace.IsFullGraph.Should().BeTrue();
		stateSpace.DpnTransitions.Should().BeEquivalentTo(unboundedDpn.Transitions);
		stateSpace.FinalDpnMarking.Should().BeEquivalentTo(unboundedDpn.FinalMarking);
		stateSpace.TypedVariables.Should().BeEquivalentTo(unboundedDpn.Variables.GetAllVariables().ToDictionary(v=>v.name,v=>v.domain));
		
		stateSpace.Nodes.Should().HaveCount(nodesCount);
		stateSpace.Nodes.All(n => n.StateConstraint != null && !n.StateConstraint.IsTrue && n.Marking.Any(p => p.Value > 0)).Should().BeTrue();
		stateSpace.Arcs.Should().HaveCount(arcsCount);
	}
	
	[Test]
	public void ConstructCoverabilityStructure_ForInfiniteStateSpace_WithFlagContinueIfUnboundedPlaceFound_Should_ConstructFull()
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\Unbounded.pnmlx", FileMode.Open);
		using var context = new Context();
		var unboundedDpn = pnmlxParser.Deserialize(fs, context);
		
		var stateSpace = StateSpaceConstructor.ConstructCoverabilityGraph(unboundedDpn, continueBranchIfUnboundedPlaceFound: true, stopOnCoveringFinalPosition: false);
		stateSpace.IsFullGraph.Should().BeTrue();
		stateSpace.DpnTransitions.Should().BeEquivalentTo(unboundedDpn.Transitions);
		stateSpace.FinalDpnMarking.Should().BeEquivalentTo(unboundedDpn.FinalMarking);
		stateSpace.TypedVariables.Should().BeEquivalentTo(unboundedDpn.Variables.GetAllVariables().ToDictionary(v=>v.name,v=>v.domain));
		
		stateSpace.Nodes.Should().HaveCount(9);
		stateSpace.Nodes.All(n => n.StateConstraint != null && !n.StateConstraint.IsTrue && n.Marking.Any(p => p.Value > 0)).Should().BeTrue();
		stateSpace.Arcs.Should().HaveCount(9);
	}
	
	[TestCase(TransitionSystemType.AbstractCoverabilityGraph, 6, 5)]
	[TestCase(TransitionSystemType.AbstractCoverabilityTree, 6, 5)]
	public void ConstructCoverabilityStructure_ForInfiniteStateSpaceWithOutputCovering_Should_ConstructNotFull(TransitionSystemType transitionSystemType, int nodesCount, int arcsCount)
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\Unbounded_covering_output.pnmlx", FileMode.Open);
		using var context = new Context();
		var unboundedDpn = pnmlxParser.Deserialize(fs, context);
		
		var stateSpace = transitionSystemType == TransitionSystemType.AbstractCoverabilityGraph
			? StateSpaceConstructor.ConstructCoverabilityGraph(unboundedDpn, continueBranchIfUnboundedPlaceFound: false, stopOnCoveringFinalPosition: true)
			: StateSpaceConstructor.ConstructCoverabilityTree(unboundedDpn, stopOnCoveringFinalPosition: true);
		stateSpace.IsFullGraph.Should().BeFalse();
		stateSpace.DpnTransitions.Should().BeEquivalentTo(unboundedDpn.Transitions);
		stateSpace.FinalDpnMarking.Should().BeEquivalentTo(unboundedDpn.FinalMarking);
		stateSpace.TypedVariables.Should().BeEquivalentTo(unboundedDpn.Variables.GetAllVariables().ToDictionary(v=>v.name,v=>v.domain));
		
		stateSpace.Nodes.Should().HaveCount(nodesCount); // This model has true constraints in the state space structures since ^w variables are free in guard(t2)
		stateSpace.Arcs.Should().HaveCount(arcsCount);
	}
}