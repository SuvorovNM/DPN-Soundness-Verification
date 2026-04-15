using DPN.Parsers;
using DPN.Soundness;
using DPN.Soundness.TransitionSystems;
using DPN.Soundness.Verification;
using FluentAssertions;
using Microsoft.Z3;
using static DPN.Soundness.Verification.ClassicalVerificationSettingsConstants;

namespace DPN.Tests;

[TestFixture(DirectVersion)]
[TestFixture(DeferringRefinementVersion)]
public class ClassicalSoundnessVerification_Test
{
	private readonly string algorithmVersion;

	public ClassicalSoundnessVerification_Test(string algorithmVersion)
	{
		this.algorithmVersion = algorithmVersion;
	}

	[Test]
	public void VerifyCreditBankRequest_Should_ReturnIsUnsound()
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\CreditBankRequest.pnmlx", FileMode.Open);
		using var context = new Context();
		var dpn = pnmlxParser.Deserialize(fs, context);

		var soundnessVerifier = new ClassicalSoundnessVerifier();
		var verificationResult = soundnessVerifier.Verify(dpn, new Dictionary<string, string> { { AlgorithmVersion, algorithmVersion } });

		verificationResult.SoundnessProperties.Should()
			.BeEquivalentTo(
				new SoundnessProperties(
					SoundnessType.Classical,
					null!,
					true,
					Array.Empty<string>(),
					true,
					false),
				o => o.Excluding(sp => sp.StateTypes));
		verificationResult.SoundnessProperties.StateTypes.Should().ContainSingle(st => st.Value.HasFlag(StateType.Deadlock));
		verificationResult.SoundnessProperties.StateTypes.Should().Contain(st => st.Value.HasFlag(StateType.Final));
		verificationResult.StateSpaceGraph.Nodes.Should().NotBeEmpty();
		verificationResult.StateSpaceGraph.Arcs.Should().NotBeEmpty();
	}

	[Test]
	public void VerifyLivelock_Should_ReturnIsUnsound()
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\Livelock.pnmlx", FileMode.Open);
		using var context = new Context();
		var dpn = pnmlxParser.Deserialize(fs, context);

		var soundnessVerifier = new ClassicalSoundnessVerifier();
		var verificationResult = soundnessVerifier.Verify(dpn, new Dictionary<string, string> { { AlgorithmVersion, algorithmVersion } });

		verificationResult.SoundnessProperties.Should()
			.BeEquivalentTo(
				new SoundnessProperties(
					SoundnessType.Classical,
					null!,
					true,
					Array.Empty<string>(),
					false,
					false),
				o => o.Excluding(sp => sp.StateTypes));
		verificationResult.SoundnessProperties.StateTypes.Should().ContainSingle(st => st.Value.HasFlag(StateType.NoWayToFinalMarking));
		verificationResult.SoundnessProperties.StateTypes.Should().Contain(st => st.Value.HasFlag(StateType.Final));
		verificationResult.StateSpaceGraph.Nodes.Should().NotBeEmpty();
		verificationResult.StateSpaceGraph.Arcs.Should().NotBeEmpty();
	}

	[Test]
	public void VerifyCasino_Should_ReturnIsUnsound()
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\Casino.pnmlx", FileMode.Open);
		using var context = new Context();
		var dpn = pnmlxParser.Deserialize(fs, context);

		var soundnessVerifier = new ClassicalSoundnessVerifier();
		var verificationResult = soundnessVerifier.Verify(dpn, new Dictionary<string, string> { { AlgorithmVersion, algorithmVersion } });

		verificationResult.SoundnessProperties.Should()
			.BeEquivalentTo(
				new SoundnessProperties(
					SoundnessType.Classical,
					null!,
					true,
					Array.Empty<string>(),
					true,
					false),
				o => o.Excluding(sp => sp.StateTypes));
		verificationResult.SoundnessProperties.StateTypes.Should().ContainSingle(st => st.Value.HasFlag(StateType.Deadlock));
		verificationResult.SoundnessProperties.StateTypes.Should().Contain(st => st.Value.HasFlag(StateType.Final));
		verificationResult.StateSpaceGraph.Nodes.Should().NotBeEmpty();
		verificationResult.StateSpaceGraph.Arcs.Should().NotBeEmpty();
	}

	[Theory]
	public void VerifyUnbounded_Should_ReturnIsUnsound(bool constructFullGraph)
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\Unbounded.pnmlx", FileMode.Open);
		using var context = new Context();
		var dpn = pnmlxParser.Deserialize(fs, context);

		var soundnessVerifier = new ClassicalSoundnessVerifier();
		var verificationResult = soundnessVerifier.Verify(dpn, new Dictionary<string, string>
		{
			{ AlgorithmVersion, algorithmVersion },
			{ ConstructFullGraph, constructFullGraph.ToString() }
		});

		verificationResult.SoundnessProperties.Should()
			.BeEquivalentTo(
				new SoundnessProperties(
					SoundnessType.Classical,
					null!,
					false,
					Array.Empty<string>(),
					false,
					false),
				o => o.Excluding(sp => sp.StateTypes));
		verificationResult.StateSpaceGraph.Nodes.Should().NotBeEmpty();
		verificationResult.StateSpaceGraph.Arcs.Should().NotBeEmpty();

		if (constructFullGraph)
		{
			verificationResult.SoundnessProperties.StateTypes.Should().Contain(st => st.Value.HasFlag(StateType.StrictlyCovered));
			verificationResult.SoundnessProperties.StateTypes.Should().Contain(st => st.Value.HasFlag(StateType.Final));
		}
		else
		{
			verificationResult.SoundnessProperties.StateTypes.Should().AllSatisfy(s => s.Value.Should().Be(StateType.Default));
		}
	}

	[Theory]
	public void VerifyGambling_Should_ReturnIsUnsound(bool constructFullGraph)
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\Gambling.pnmlx", FileMode.Open);
		using var context = new Context();
		var dpn = pnmlxParser.Deserialize(fs, context);

		var soundnessVerifier = new ClassicalSoundnessVerifier();
		var verificationResult = soundnessVerifier.Verify(dpn, new Dictionary<string, string>
		{
			{ AlgorithmVersion, algorithmVersion },
			{ ConstructFullGraph, constructFullGraph.ToString() }
		});

		verificationResult.SoundnessProperties.Should()
			.BeEquivalentTo(
				new SoundnessProperties(
					SoundnessType.Classical,
					null!,
					false,
					Array.Empty<string>(),
					false,
					false),
				o => o.Excluding(sp => sp.StateTypes));
		verificationResult.StateSpaceGraph.Nodes.Should().NotBeEmpty();
		verificationResult.StateSpaceGraph.Arcs.Should().NotBeEmpty();

		if (constructFullGraph)
		{
			verificationResult.SoundnessProperties.StateTypes.Should().Contain(st => st.Value.HasFlag(StateType.StrictlyCovered));
			verificationResult.SoundnessProperties.StateTypes.Should().Contain(st => st.Value.HasFlag(StateType.Final));
		}
		else
		{
			verificationResult.SoundnessProperties.StateTypes.Should().AllSatisfy(s => s.Value.Should().Be(StateType.Default));
		}
	}

	[Test]
	public void VerifyRoadFines_Should_ReturnIsUnsound()
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\RoadFines.pnmlx", FileMode.Open);
		using var context = new Context();
		var dpn = pnmlxParser.Deserialize(fs, context);

		var soundnessVerifier = new ClassicalSoundnessVerifier();
		var verificationResult = soundnessVerifier.Verify(dpn, new Dictionary<string, string> { { AlgorithmVersion, algorithmVersion } });

		verificationResult.SoundnessProperties.Should()
			.BeEquivalentTo(
				new SoundnessProperties(
					SoundnessType.Classical,
					null!,
					true,
					["n15"],
					algorithmVersion == DirectVersion,
					false),
				o => o.Excluding(sp => sp.StateTypes));

		if (algorithmVersion == DirectVersion)
		{
			verificationResult.SoundnessProperties.StateTypes.Should().Contain(st => st.Value.HasFlag(StateType.Deadlock));
		}

		verificationResult.SoundnessProperties.StateTypes.Should().Contain(st => st.Value.HasFlag(StateType.Final));
		verificationResult.StateSpaceGraph.Nodes.Should().NotBeEmpty();
		verificationResult.StateSpaceGraph.Arcs.Should().NotBeEmpty();
	}

	[Test]
	public void VerifySepsis_Should_ReturnSound()
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\SepsisMined.pnmlx", FileMode.Open);
		using var context = new Context();
		var dpn = pnmlxParser.Deserialize(fs, context);

		var soundnessVerifier = new ClassicalSoundnessVerifier();
		var verificationResult = soundnessVerifier.Verify(dpn, new Dictionary<string, string> { { AlgorithmVersion, algorithmVersion } });

		verificationResult.SoundnessProperties.Should()
			.BeEquivalentTo(
				new SoundnessProperties(
					SoundnessType.Classical,
					null!,
					true,
					Array.Empty<string>(),
					false,
					true),
				o => o.Excluding(sp => sp.StateTypes));
		verificationResult.SoundnessProperties.StateTypes.Should().Contain(st => st.Value.HasFlag(StateType.Final));
		verificationResult.SoundnessProperties.StateTypes.Should().NotContain(st => st.Value > StateType.Final);
		verificationResult.StateSpaceGraph.Nodes.Should().NotBeEmpty();
		verificationResult.StateSpaceGraph.Arcs.Should().NotBeEmpty();
	}

	[Test]
	public void VerifyDigitalWhiteboardTransfer_Should_ReturnIsUnsound()
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\DigitalWhiteboard_Transfer.pnmlx", FileMode.Open);
		using var context = new Context();
		var dpn = pnmlxParser.Deserialize(fs, context);

		var soundnessVerifier = new ClassicalSoundnessVerifier();
		var verificationResult = soundnessVerifier.Verify(dpn, new Dictionary<string, string> { { AlgorithmVersion, algorithmVersion } });

		verificationResult.SoundnessProperties.Should()
			.BeEquivalentTo(
				new SoundnessProperties(
					SoundnessType.Classical,
					null!,
					true,
					Array.Empty<string>(),
					true,
					false),
				o => o.Excluding(sp => sp.StateTypes));
		verificationResult.SoundnessProperties.StateTypes.Should().ContainSingle(st => st.Value.HasFlag(StateType.Deadlock));
		verificationResult.SoundnessProperties.StateTypes.Should().Contain(st => st.Value.HasFlag(StateType.Final));
		verificationResult.StateSpaceGraph.Nodes.Should().NotBeEmpty();
		verificationResult.StateSpaceGraph.Arcs.Should().NotBeEmpty();
	}

	[Test]
	public void VerifyHospitalBilling_Should_ReturnSound()
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\HospitalBilling.pnmlx", FileMode.Open);
		using var context = new Context();
		var dpn = pnmlxParser.Deserialize(fs, context);

		var soundnessVerifier = new ClassicalSoundnessVerifier();
		var verificationResult = soundnessVerifier.Verify(dpn, new Dictionary<string, string> { { AlgorithmVersion, algorithmVersion } });

		verificationResult.SoundnessProperties.Should()
			.BeEquivalentTo(
				new SoundnessProperties(
					SoundnessType.Classical,
					null!,
					true,
					Array.Empty<string>(),
					false,
					true),
				o => o.Excluding(sp => sp.StateTypes));
		verificationResult.SoundnessProperties.StateTypes.Should().Contain(st => st.Value.HasFlag(StateType.Final));
		verificationResult.SoundnessProperties.StateTypes.Should().NotContain(st => st.Value > StateType.Final);
		verificationResult.StateSpaceGraph.Nodes.Should().NotBeEmpty();
		verificationResult.StateSpaceGraph.Arcs.Should().NotBeEmpty();
	}

	[Test]
	public void VerifyPackageHandling_Should_ReturnIsUnsound()
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\PackageHandling.pnmlx", FileMode.Open);
		using var context = new Context();
		var dpn = pnmlxParser.Deserialize(fs, context);

		var soundnessVerifier = new ClassicalSoundnessVerifier();
		var verificationResult = soundnessVerifier.Verify(dpn, new Dictionary<string, string> { { AlgorithmVersion, algorithmVersion } });

		verificationResult.SoundnessProperties.Should()
			.BeEquivalentTo(
				new SoundnessProperties(
					SoundnessType.Classical,
					null!,
					true,
					["t4", "tau2", "t9", "tau6", "t10", "tau10", "t14", "tau12"],
					false,
					false),
				o => o.Excluding(sp => sp.StateTypes));
		verificationResult.SoundnessProperties.StateTypes.Should().Contain(st => st.Value.HasFlag(StateType.Final));
		verificationResult.SoundnessProperties.StateTypes.Should().NotContain(st => st.Value > StateType.Final);
		verificationResult.StateSpaceGraph.Nodes.Should().NotBeEmpty();
		verificationResult.StateSpaceGraph.Arcs.Should().NotBeEmpty();
	}

	[Test]
	public void VerifySimpleAuction_Should_ReturnIsUnsound()
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\SimpleAuction.pnmlx", FileMode.Open);
		using var context = new Context();
		var dpn = pnmlxParser.Deserialize(fs, context);

		var soundnessVerifier = new ClassicalSoundnessVerifier();
		var verificationResult = soundnessVerifier.Verify(dpn, new Dictionary<string, string> { { AlgorithmVersion, algorithmVersion } });

		verificationResult.SoundnessProperties.Should()
			.BeEquivalentTo(
				new SoundnessProperties(
					SoundnessType.Classical,
					null!,
					true,
					["reset"],
					algorithmVersion == DirectVersion,
					false),
				o => o.Excluding(sp => sp.StateTypes));

		if (algorithmVersion == DirectVersion)
		{
			verificationResult.SoundnessProperties.StateTypes.Should().ContainSingle(st => st.Value.HasFlag(StateType.Deadlock));
		}

		verificationResult.SoundnessProperties.StateTypes.Should().Contain(st => st.Value.HasFlag(StateType.Final));
		verificationResult.StateSpaceGraph.Nodes.Should().NotBeEmpty();
		verificationResult.StateSpaceGraph.Arcs.Should().NotBeEmpty();
	}
}