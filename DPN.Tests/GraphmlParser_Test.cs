using DPN.Models.Extensions;
using DPN.Parsers;
using DPN.Soundness.Transformations;
using DPN.Soundness.TransitionSystems;
using FluentAssertions;
using Microsoft.Z3;

namespace DPN.Tests;

public class GraphmlParser_Test
{
	[TestCase("TestData\\ACT.graphml", TransitionSystemType.AbstractCoverabilityTree, 30)]
	[TestCase("TestData\\ACG.graphml", TransitionSystemType.AbstractCoverabilityGraph, 31)]
	[TestCase("TestData\\ARG.graphml",  TransitionSystemType.AbstractReachabilityGraph, 5)]
	public void DeserializeFromGraphml_Should_ReturnValidStateSpace(string fileName, TransitionSystemType stateSpaceType, int nodesCount)
	{
		var graphmlParser = new GraphmlParser();
		
		using var fs = new FileStream(fileName, FileMode.Open);
		using var context = new Context();
		var stateSpace = graphmlParser.Deserialize(fs, context);
		
		stateSpace.StateSpaceType.Should().Be(stateSpaceType);
		stateSpace.Nodes.Should().HaveCount(nodesCount);
		stateSpace.Nodes.All(n=>n.StateConstraint != null && !n.StateConstraint.IsTrue && n.Marking.Any()).Should().BeTrue();
		stateSpace.Arcs.Length.Should().BeGreaterThanOrEqualTo(nodesCount - 1);
		stateSpace.Arcs.Should().NotContain(a => a.IsSilent);
		stateSpace.IsFullGraph.Should().BeTrue(); // all considered graphs are fully constructed
		stateSpace.FinalDpnMarking.Should().Contain(m=>m.Key == "o" && m.Value == 1);
		stateSpace.DpnTransitions.All(t=>t.Id != string.Empty && t.Label != string.Empty && t.BaseTransitionId != string.Empty && !t.IsSplit && !t.IsTau).Should().BeTrue();
		stateSpace.DpnTransitions.Should().Contain(t => !t.Guard.ActualConstraintExpression.IsTrue);
	}
	
	[Test]
	public void DeserializeTauGraphFromGraphml_Should_ReturnValidStateSpace()
	{
		var graphmlParser = new GraphmlParser();
		
		using var fs = new FileStream("TestData\\Tau_ARG.graphml", FileMode.Open);
		using var context = new Context();
		var stateSpace = graphmlParser.Deserialize(fs, context);
		
		stateSpace.StateSpaceType.Should().Be(TransitionSystemType.AbstractReachabilityGraph);
		stateSpace.Nodes.Should().HaveCount(6);
		stateSpace.Nodes.All(n=>n.StateConstraint != null && !n.StateConstraint.IsTrue && n.Marking.Any()).Should().BeTrue();
		stateSpace.IsFullGraph.Should().BeTrue();
		stateSpace.FinalDpnMarking.Should().Contain(m=>m.Key == "o" && m.Value == 1);
		stateSpace.Arcs.Should().Contain(a => a.IsSilent);
		
		stateSpace.DpnTransitions.All(t=>t.Id != string.Empty && t.Label != string.Empty && t.BaseTransitionId != string.Empty && !t.IsSplit).Should().BeTrue();
		stateSpace.DpnTransitions.Should().Contain(t => t.IsTau);
	}
	
	[Test]
	public void DeserializeTauRefinedGraphFromGraphml_Should_ReturnValidStateSpace()
	{
		var graphmlParser = new GraphmlParser();
		
		using var fs = new FileStream("TestData\\Tau_refined_ARG.graphml", FileMode.Open);
		using var context = new Context();
		var stateSpace = graphmlParser.Deserialize(fs, context);
		
		stateSpace.StateSpaceType.Should().Be(TransitionSystemType.AbstractReachabilityGraph);
		stateSpace.Nodes.Should().HaveCount(10);
		stateSpace.Nodes.All(n=>n.StateConstraint != null && !n.StateConstraint.IsTrue && n.Marking.Any()).Should().BeTrue();
		stateSpace.IsFullGraph.Should().BeTrue();
		stateSpace.FinalDpnMarking.Should().Contain(m=>m.Key == "o" && m.Value == 1);
		stateSpace.Arcs.Should().Contain(a => a.IsSilent);
		
		stateSpace.DpnTransitions.All(t=>t.Id != string.Empty && t.Label != string.Empty && t.BaseTransitionId != string.Empty).Should().BeTrue();
		stateSpace.DpnTransitions.Should().Contain(t => t.IsTau);
		stateSpace.DpnTransitions.Should().Contain(t => t.IsSplit);
	}

	[TestCase(TransitionSystemType.AbstractReachabilityGraph)]
	[TestCase(TransitionSystemType.AbstractCoverabilityGraph)]
	[TestCase(TransitionSystemType.AbstractCoverabilityTree)]
	public void SerializeToGraphml_ForUnboundedDpn_Should_ReturnConstructCorrectFile(TransitionSystemType stateSpaceType)
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\Unbounded.pnmlx", FileMode.Open);
		var unboundedDpn = pnmlxParser.Deserialize(fs);

		var stateSpace = stateSpaceType switch
		{
			TransitionSystemType.AbstractReachabilityGraph => StateSpaceConstructor.ConstructReachabilityGraph(unboundedDpn),
			TransitionSystemType.AbstractCoverabilityGraph => StateSpaceConstructor.ConstructCoverabilityGraph(unboundedDpn, false, false),
			TransitionSystemType.AbstractCoverabilityTree => StateSpaceConstructor.ConstructCoverabilityTree(unboundedDpn, false),
			_ => throw new ArgumentOutOfRangeException(nameof(stateSpaceType), stateSpaceType, "Unsupported state space type")
		};
		
		var graphmlParser = new GraphmlParser();
		var memoryStream = new MemoryStream();
		graphmlParser.Serialize(stateSpace, memoryStream);
		memoryStream.Seek(0, SeekOrigin.Begin);
		var savedStateSpace = graphmlParser.Deserialize(memoryStream, unboundedDpn.Context);

		savedStateSpace.Arcs.Should().BeEquivalentTo(stateSpace.Arcs);
		savedStateSpace.FinalDpnMarking.Should().BeEquivalentTo(stateSpace.FinalDpnMarking);
		savedStateSpace.IsFullGraph.Should().Be(savedStateSpace.IsFullGraph);
		savedStateSpace.TypedVariables.Should().BeEquivalentTo(stateSpace.TypedVariables);
		savedStateSpace.StateSpaceType.Should().Be(stateSpaceType);
		
		savedStateSpace.Nodes.Select(n=>n.Marking).Should().BeEquivalentTo(stateSpace.Nodes.Select(n=>n.Marking));
		savedStateSpace.Nodes.Join(stateSpace.Nodes, s=>s.Id, s=>s.Id, (s1,s2)=>(s1.StateConstraint,s2.StateConstraint))
			.All(formulas=> unboundedDpn.Context.AreEqual(formulas.Item1, formulas.Item2)).Should().BeTrue();
	}
	
	[TestCase(true, false)]
	[TestCase(false, true)]
	[TestCase(true, true)]
	public void SerializeToGraphml_ForLivelockDpn_Should_ReturnConstructCorrectFile(bool isTau, bool isRefined)
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\Livelock.pnmlx", FileMode.Open);
		var livelockDpn = pnmlxParser.Deserialize(fs);
		
		if (isTau)
		{
			livelockDpn = new TransformerToTau().Transform(livelockDpn);
		}
		
		if (isRefined)
		{
			(livelockDpn,_) = new TransformerToRefined().Transform(livelockDpn, new Dictionary<string, string>());
		}

		if (isTau)
		{
			livelockDpn = new TransformerToTau().Transform(livelockDpn);
		}
		
		var stateSpace = StateSpaceConstructor.ConstructReachabilityGraph(livelockDpn);
		
		var graphmlParser = new GraphmlParser();
		var memoryStream = new MemoryStream();
		graphmlParser.Serialize(stateSpace, memoryStream);
		memoryStream.Seek(0, SeekOrigin.Begin);
		var savedStateSpace = graphmlParser.Deserialize(memoryStream, livelockDpn.Context);

		savedStateSpace.Arcs.Should().BeEquivalentTo(stateSpace.Arcs);
		savedStateSpace.FinalDpnMarking.Should().BeEquivalentTo(stateSpace.FinalDpnMarking);
		savedStateSpace.IsFullGraph.Should().Be(savedStateSpace.IsFullGraph);
		savedStateSpace.TypedVariables.Should().BeEquivalentTo(stateSpace.TypedVariables);
		savedStateSpace.StateSpaceType.Should().Be(TransitionSystemType.AbstractReachabilityGraph);
		
		savedStateSpace.Nodes.Select(n=>n.Marking).Should().BeEquivalentTo(stateSpace.Nodes.Select(n=>n.Marking));
		savedStateSpace.Nodes.Join(stateSpace.Nodes, s=>s.Id, s=>s.Id, (s1,s2)=>(s1.StateConstraint,s2.StateConstraint))
			.All(formulas=> livelockDpn.Context.AreEqual(formulas.Item1, formulas.Item2)).Should().BeTrue();
	}
}