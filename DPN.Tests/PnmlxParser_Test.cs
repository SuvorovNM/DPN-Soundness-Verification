using DPN.Models.Extensions;
using DPN.Parsers;
using DPN.Soundness.Transformations;
using FluentAssertions;
using Microsoft.Z3;

namespace DPN.Tests;

public class PnmlxParser_Test
{
	[TestCase(false, false)]
	[TestCase(true, false)]
	[TestCase(false, true)]
	[TestCase(true, true)]
	public async Task SerializeToPnmlxAndDeserializeModelWithTransformations_Should_ReturnTheSameModel(bool isTau, bool isRefined)
	{
		var pnmlxParser = new PnmlxParser();
		await using var fs = new FileStream("TestData\\Livelock.pnmlx", FileMode.Open);
		using var context = new Context();
		var livelockDpn = pnmlxParser.Deserialize(fs, context);

		if (isRefined)
		{
			(livelockDpn, _) = new TransformerToRefined().Transform(livelockDpn, new Dictionary<string, string>());
		}

		if (isTau)
		{
			livelockDpn = new TransformerToTau().Transform(livelockDpn);
		}

		using var memoryStream = new MemoryStream();
		await pnmlxParser.Serialize(livelockDpn, memoryStream);
		memoryStream.Seek(0, SeekOrigin.Begin);
		var savedDpn = pnmlxParser.Deserialize(memoryStream, context);

		savedDpn.Places.Should().BeEquivalentTo(livelockDpn.Places);
		savedDpn.Transitions.Should().BeEquivalentTo(
			livelockDpn.Transitions,
			o => o.Including(t => t.Label).Including(t => t.IsTau));
		savedDpn.Transitions.Join(livelockDpn.Transitions, s => s.Label, s => s.Label, (s1, s2) => (s1.Guard.ActualConstraintExpression, s2.Guard.ActualConstraintExpression))
			.All(formulas => livelockDpn.Context.AreEqual(formulas.Item1, formulas.Item2)).Should().BeTrue();
		savedDpn.Variables.GetAllVariables().Should().BeEquivalentTo(livelockDpn.Variables.GetAllVariables());
		savedDpn.Name.Should().Be(livelockDpn.Name);
		savedDpn.Id.Should().Be(livelockDpn.Id);
		savedDpn.Arcs.Should().BeEquivalentTo(livelockDpn.Arcs, o=>o.Excluding(a=>a.Destination.Id).Excluding(a=>a.Source.Id));
	}
	
	[Test]
	public async Task SerializeToPnmlxAndDeserializeModelWithArcWeights_Should_ReturnTheSameModel()
	{
		var pnmlxParser = new PnmlxParser();
		await using var fs = new FileStream("TestData\\Gambling.pnmlx", FileMode.Open);
		using var context = new Context();
		var gamblingDpn = pnmlxParser.Deserialize(fs, context);

		using var memoryStream = new MemoryStream();
		await pnmlxParser.Serialize(gamblingDpn, memoryStream);
		memoryStream.Seek(0, SeekOrigin.Begin);
		var savedDpn = pnmlxParser.Deserialize(memoryStream, context);

		savedDpn.Places.Should().BeEquivalentTo(gamblingDpn.Places);
		savedDpn.Transitions.Should().BeEquivalentTo(
			gamblingDpn.Transitions,
			o => o.Including(t => t.Label).Including(t => t.IsTau));
		savedDpn.Transitions.Join(gamblingDpn.Transitions, s => s.Label, s => s.Label, (s1, s2) => (s1.Guard.ActualConstraintExpression, s2.Guard.ActualConstraintExpression))
			.All(formulas => gamblingDpn.Context.AreEqual(formulas.Item1, formulas.Item2)).Should().BeTrue();
		savedDpn.Variables.GetAllVariables().Should().BeEquivalentTo(gamblingDpn.Variables.GetAllVariables());
		savedDpn.Name.Should().Be(gamblingDpn.Name);
		savedDpn.Id.Should().Be(gamblingDpn.Id);
		savedDpn.Arcs.Should().BeEquivalentTo(gamblingDpn.Arcs, o=>o.Excluding(a=>a.Destination.Id).Excluding(a=>a.Source.Id));
	}
}