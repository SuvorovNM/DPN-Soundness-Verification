using DataPetriNetGeneration;
using DPN.Models.Extensions;
using DPN.Soundness.Verification;
using FluentAssertions;
using Microsoft.Z3;

namespace DPN.Tests;

public class DPNGenerator_Test
{
	[TestCase(4, 6, 0, 0, 0, 0)]
	[TestCase(4, 6, 2, 0, 1, 3)]
	[TestCase(6, 4, 0, 2, 3, 1)]
	[TestCase(6, 4, 2, 2, 3, 3)]
	[TestCase(5, 5, 0, 2, 1, 3)]
	public void GenerateRandomDPN_Should_GenerateAccordingToParameters(int placesCount, int transitionsCount, int resourcePlacesCount, int extraArcsCount, int varsCount, int conditionsCount)
	{
		using var context = new Context();
		var dpnGenerator = new DPNGenerator(context);

		var generatedDpn = dpnGenerator.Generate(placesCount, transitionsCount, resourcePlacesCount, extraArcsCount, varsCount, conditionsCount);

		generatedDpn.Places.Should().HaveCount(placesCount + resourcePlacesCount);
		generatedDpn.Places.Should().ContainSingle(p=>p.Label == "i");
		generatedDpn.Places.Should().ContainSingle(p=>p.Label == "o");	
		generatedDpn.Places.Where(p => p.Label.StartsWith('p')).Should().HaveCount(placesCount - 2);
		generatedDpn.Places.Where(p => p.Label.StartsWith('r')).Should().HaveCount(resourcePlacesCount);
		generatedDpn.Transitions.Should().HaveCount(transitionsCount);
		generatedDpn.Arcs.Sum(a=>a.Weight).Should().BeGreaterThanOrEqualTo(placesCount + transitionsCount + extraArcsCount - 1);
		generatedDpn.Variables.GetAllVariables().Should().HaveCount(varsCount);
		generatedDpn.Transitions.Count(t => !t.Guard.ActualConstraintExpression.IsTrue).Should().BeLessThanOrEqualTo(conditionsCount);
		generatedDpn.Transitions.ForEach(t=>context.CanBeSatisfied(t.Guard.ActualConstraintExpression).Should().BeTrue());
	}
	
	[TestCase(3, 7)]
	[TestCase(7, 3)]
	[TestCase(7, 7)]
	[TestCase(3, 3)]
	public void GenerateDPNWithoutDataAndExtraElements_Should_GenerateSoundNet(int placesCount, int transitionsCount)
	{
		using var context = new Context();
		var dpnGenerator = new DPNGenerator(context);

		var generatedDpn = dpnGenerator.Generate(placesCount, transitionsCount, 0, 0, 0, 0);

		var soundnessVerifier = new ClassicalSoundnessVerifier();
		var res= soundnessVerifier.Verify(generatedDpn, new Dictionary<string, string>());
		res.SoundnessProperties.Soundness.Should().BeTrue();
	}
}