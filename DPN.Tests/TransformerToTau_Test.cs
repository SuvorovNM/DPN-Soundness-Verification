using DPN.Parsers;
using DPN.Soundness.Transformations;
using FluentAssertions;
using Microsoft.Z3;

namespace DPN.Tests;

public class TransformerToTau_Test
{
	[Test]
	public void TransformWhenNoTauCanBeAdded_Should_ReturnTheSameModel()
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\Unbounded.pnmlx", FileMode.Open);
		using var context = new Context();
		var unboundedDpn = pnmlxParser.Deserialize(fs, context);

		var transformerToTau = new TransformerToTau();
		var transformedDpn = transformerToTau.Transform(unboundedDpn);
		
		transformedDpn.Should().BeEquivalentTo(unboundedDpn);
	}
	
	[Test]
	public void TransformWhenSingleTauCanBeAdded_Should_ReturnTransformedModel()
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\Livelock.pnmlx", FileMode.Open);
		using var context = new Context();
		var livelockDpn = pnmlxParser.Deserialize(fs, context);

		var transformerToTau = new TransformerToTau();
		var transformedDpn = transformerToTau.Transform(livelockDpn);

		transformedDpn.Should().BeEquivalentTo(livelockDpn, 
			o=>o.Excluding(dpn=>dpn.Transitions).Excluding(dpn=>dpn.Arcs));
		var sourceTransitions = transformedDpn.Transitions.Where(t => !t.IsTau);
		sourceTransitions.Should().BeEquivalentTo(livelockDpn.Transitions);
		
		var tauTransition = transformedDpn.Transitions.Single(t => t.IsTau);
		tauTransition.Id.Should().Be("τ(t2)");
		tauTransition.NonTauTransitionId.Should().Be("t2");
		transformedDpn.Arcs.Where(a=>a.Destination == tauTransition).Should()
			.BeEquivalentTo(livelockDpn.Arcs.Where(a=>a.Destination.Id == "t2"), o=>o.Excluding(a=>a.Destination));
		transformedDpn.Arcs.Where(a=>a.Source == tauTransition).Select(a=>a.Destination).Should()
			.BeEquivalentTo(livelockDpn.Arcs.Where(a=>a.Destination.Id == "t2").Select(a=>a.Source));
	}
	
	[Test]
	public void TransformWhenMultipleTauCanBeAdded_Should_ReturnTransformedModel()
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\Gambling.pnmlx", FileMode.Open);
		using var context = new Context();
		var gamblingDpn = pnmlxParser.Deserialize(fs, context);

		var transformerToTau = new TransformerToTau();
		var transformedDpn = transformerToTau.Transform(gamblingDpn);

		transformedDpn.Should().BeEquivalentTo(gamblingDpn, o=>o.Excluding(dpn=>dpn.Transitions).Excluding(dpn=>dpn.Arcs));
		var sourceTransitions = transformedDpn.Transitions.Where(t => !t.IsTau);
		sourceTransitions.Should().BeEquivalentTo(gamblingDpn.Transitions);

		var tauTransitions = transformedDpn.Transitions.Where(t => t.IsTau).ToArray();
		tauTransitions.Should().HaveCount(3);
		foreach (var tauTransition in tauTransitions)
		{
			tauTransition.Id.Should().Be($"τ({tauTransition.NonTauTransitionId})");
			transformedDpn.Arcs.Where(a=>a.Destination == tauTransition).Should()
				.BeEquivalentTo(gamblingDpn.Arcs.Where(a=>a.Destination.Id == tauTransition.NonTauTransitionId), o=>o.Excluding(a=>a.Destination));
			transformedDpn.Arcs.Where(a=>a.Source == tauTransition).Select(a=>a.Destination).Should()
				.BeEquivalentTo(gamblingDpn.Arcs.Where(a=>a.Destination.Id == tauTransition.NonTauTransitionId).Select(a=>a.Source));
		}
	}
}