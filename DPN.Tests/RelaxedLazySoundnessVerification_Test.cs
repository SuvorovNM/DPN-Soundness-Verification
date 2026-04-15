using DPN.Parsers;
using DPN.Soundness;
using DPN.Soundness.TransitionSystems;
using DPN.Soundness.Verification;
using FluentAssertions;
using Microsoft.Z3;

namespace DPN.Tests;

[TestFixture(RelaxedLazyVerificationSettingsConstants.CoverabilityGraph)]
[TestFixture(RelaxedLazyVerificationSettingsConstants.CoverabilityTree)]
public class RelaxedLazySoundnessVerification_Test
{
	private string baseStructure;

	public RelaxedLazySoundnessVerification_Test(string baseStructure)
	{
		this.baseStructure = baseStructure;
	}

	[Test]
	public void VerifyCreditBankRequest_Should_ReturnIsSound()
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\CreditBankRequest.pnmlx", FileMode.Open);
		using var context = new Context();
		var dpn = pnmlxParser.Deserialize(fs, context);

		var soundnessVerifier = new RelaxedLazySoundnessVerifier();
		var verificationResult = soundnessVerifier.Verify(dpn, new Dictionary<string, string>
		{
			{
				RelaxedLazyVerificationSettingsConstants.BaseStructure, baseStructure
			}
		});

		verificationResult.SoundnessProperties.Should()
			.BeEquivalentTo(
				new SoundnessProperties(
					SoundnessType.RelaxedLazy,
					null!,
					true,
					Array.Empty<string>(),
					false,
					true),
				o => o.Excluding(sp => sp.StateTypes));
		verificationResult.SoundnessProperties.StateTypes.Should().Contain(st => st.Value.HasFlag(StateType.Final));
		verificationResult.StateSpaceGraph.Nodes.Should().NotBeEmpty();
		verificationResult.StateSpaceGraph.Arcs.Should().NotBeEmpty();
	}

	[Test]
	public void VerifyLivelock_Should_ReturnIsSound()
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\Livelock.pnmlx", FileMode.Open);
		using var context = new Context();
		var dpn = pnmlxParser.Deserialize(fs, context);

		var soundnessVerifier = new RelaxedLazySoundnessVerifier();
		var verificationResult = soundnessVerifier.Verify(dpn, new Dictionary<string, string>
		{
			{
				RelaxedLazyVerificationSettingsConstants.BaseStructure, baseStructure
			}
		});

		verificationResult.SoundnessProperties.Should()
			.BeEquivalentTo(
				new SoundnessProperties(
					SoundnessType.RelaxedLazy,
					null!,
					true,
					Array.Empty<string>(),
					false,
					true),
				o => o.Excluding(sp => sp.StateTypes));
		verificationResult.SoundnessProperties.StateTypes.Should().Contain(st => st.Value.HasFlag(StateType.Final));
		verificationResult.StateSpaceGraph.Nodes.Should().NotBeEmpty();
		verificationResult.StateSpaceGraph.Arcs.Should().NotBeEmpty();
	}

	[Test]
	public void VerifyCasino_Should_ReturnIsSound()
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\Casino.pnmlx", FileMode.Open);
		using var context = new Context();
		var dpn = pnmlxParser.Deserialize(fs, context);

		var soundnessVerifier = new RelaxedLazySoundnessVerifier();
		var verificationResult = soundnessVerifier.Verify(dpn, new Dictionary<string, string>
		{
			{
				RelaxedLazyVerificationSettingsConstants.BaseStructure, baseStructure
			}
		});

		verificationResult.SoundnessProperties.Should()
			.BeEquivalentTo(
				new SoundnessProperties(
					SoundnessType.RelaxedLazy,
					null!,
					true,
					Array.Empty<string>(),
					false,
					true),
				o => o.Excluding(sp => sp.StateTypes));
		verificationResult.SoundnessProperties.StateTypes.Should().Contain(st => st.Value.HasFlag(StateType.Final));
		verificationResult.StateSpaceGraph.Nodes.Should().NotBeEmpty();
		verificationResult.StateSpaceGraph.Arcs.Should().NotBeEmpty();
	}

	[Theory]
	public void VerifyUnbounded_Should_ReturnIsSound(bool stopOnCoveringFinal)
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\Unbounded.pnmlx", FileMode.Open);
		using var context = new Context();
		var dpn = pnmlxParser.Deserialize(fs, context);

		var soundnessVerifier = new RelaxedLazySoundnessVerifier();
		var verificationResult = soundnessVerifier.Verify(dpn, new Dictionary<string, string>
		{
			{ RelaxedLazyVerificationSettingsConstants.BaseStructure, baseStructure },
			{ RelaxedLazyVerificationSettingsConstants.StopOnCoveringFinalPosition, stopOnCoveringFinal.ToString() }
		});

		verificationResult.SoundnessProperties.Should()
			.BeEquivalentTo(
				new SoundnessProperties(
					SoundnessType.RelaxedLazy,
					null!,
					false,
					Array.Empty<string>(),
					false,
					true),
				o => o.Excluding(sp => sp.StateTypes));
		verificationResult.SoundnessProperties.StateTypes.Should().Contain(st => st.Value.HasFlag(StateType.Final));
		verificationResult.StateSpaceGraph.Nodes.Should().NotBeEmpty();
		verificationResult.StateSpaceGraph.Arcs.Should().NotBeEmpty();
	}
	
	[Theory]
	public void VerifyGambling_Should_ReturnIsSound(bool stopOnCoveringFinal)
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\Gambling.pnmlx", FileMode.Open);
		using var context = new Context();
		var dpn = pnmlxParser.Deserialize(fs, context);

		var soundnessVerifier = new RelaxedLazySoundnessVerifier();
		var verificationResult = soundnessVerifier.Verify(dpn, new Dictionary<string, string>
		{
			{ RelaxedLazyVerificationSettingsConstants.BaseStructure, baseStructure },
			{ RelaxedLazyVerificationSettingsConstants.StopOnCoveringFinalPosition, stopOnCoveringFinal.ToString() }
		});

		verificationResult.SoundnessProperties.Should()
			.BeEquivalentTo(
				new SoundnessProperties(
					SoundnessType.RelaxedLazy,
					null!,
					false,
					Array.Empty<string>(),
					false,
					true),
				o => o.Excluding(sp => sp.StateTypes));
		verificationResult.SoundnessProperties.StateTypes.Should().Contain(st => st.Value.HasFlag(StateType.Final));
		verificationResult.StateSpaceGraph.Nodes.Should().NotBeEmpty();
		verificationResult.StateSpaceGraph.Arcs.Should().NotBeEmpty();
	}
	
	[Test]
	public void VerifyRoadFines_Should_ReturnIsUnsound()
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\RoadFines.pnmlx", FileMode.Open);
		using var context = new Context();
		var dpn = pnmlxParser.Deserialize(fs, context);

		var soundnessVerifier = new RelaxedLazySoundnessVerifier();
		var verificationResult = soundnessVerifier.Verify(dpn, new Dictionary<string, string>
		{
			{ RelaxedLazyVerificationSettingsConstants.BaseStructure, baseStructure }
		});

		verificationResult.SoundnessProperties.Should()
			.BeEquivalentTo(
				new SoundnessProperties(
					SoundnessType.RelaxedLazy,
					null!,
					true,
					["n15"],
					false,
					false),
				o => o.Excluding(sp => sp.StateTypes));

		verificationResult.SoundnessProperties.StateTypes.Should().Contain(st => st.Value.HasFlag(StateType.Final));
		verificationResult.StateSpaceGraph.Nodes.Should().NotBeEmpty();
		verificationResult.StateSpaceGraph.Arcs.Should().NotBeEmpty();
	}
	
	[Test]
	[Explicit("Full ACT construction requires much time")]
	public void VerifySepsis_Should_ReturnSound()
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\SepsisMined.pnmlx", FileMode.Open);
		using var context = new Context();
		var dpn = pnmlxParser.Deserialize(fs, context);

		var soundnessVerifier = new RelaxedLazySoundnessVerifier();
		var verificationResult = soundnessVerifier.Verify(dpn, new Dictionary<string, string>
		{
			{ RelaxedLazyVerificationSettingsConstants.BaseStructure, baseStructure }
		});

		verificationResult.SoundnessProperties.Should()
			.BeEquivalentTo(
				new SoundnessProperties(
					SoundnessType.RelaxedLazy,
					null!,
					true,
					Array.Empty<string>(),
					false,
					true),
				o => o.Excluding(sp => sp.StateTypes));
		verificationResult.SoundnessProperties.StateTypes.Should().Contain(st => st.Value.HasFlag(StateType.Final));
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

		var soundnessVerifier = new RelaxedLazySoundnessVerifier();
		var verificationResult = soundnessVerifier.Verify(dpn, new Dictionary<string, string>
		{
			{ RelaxedLazyVerificationSettingsConstants.BaseStructure, baseStructure }
		});

		verificationResult.SoundnessProperties.Should()
			.BeEquivalentTo(
				new SoundnessProperties(
					SoundnessType.RelaxedLazy,
					null!,
					true,
					Array.Empty<string>(),
					false,
					true),
				o => o.Excluding(sp => sp.StateTypes));
		verificationResult.SoundnessProperties.StateTypes.Should().Contain(st => st.Value.HasFlag(StateType.Final));
		verificationResult.StateSpaceGraph.Nodes.Should().NotBeEmpty();
		verificationResult.StateSpaceGraph.Arcs.Should().NotBeEmpty();
	}
	
	[Test]
	[Explicit("Full ACT construction requires much time")]
	public void VerifyHospitalBilling_Should_ReturnSound()
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\HospitalBilling.pnmlx", FileMode.Open);
		using var context = new Context();
		var dpn = pnmlxParser.Deserialize(fs, context);

		var soundnessVerifier = new RelaxedLazySoundnessVerifier();
		var verificationResult = soundnessVerifier.Verify(dpn, new Dictionary<string, string>
		{
			{ RelaxedLazyVerificationSettingsConstants.BaseStructure, baseStructure }
		});

		verificationResult.SoundnessProperties.Should()
			.BeEquivalentTo(
				new SoundnessProperties(
					SoundnessType.RelaxedLazy,
					null!,
					true,
					Array.Empty<string>(),
					false,
					true),
				o => o.Excluding(sp => sp.StateTypes));
		verificationResult.SoundnessProperties.StateTypes.Should().Contain(st => st.Value.HasFlag(StateType.Final));
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

		var soundnessVerifier = new RelaxedLazySoundnessVerifier();
		var verificationResult = soundnessVerifier.Verify(dpn, new Dictionary<string, string>
		{
			{ RelaxedLazyVerificationSettingsConstants.BaseStructure, baseStructure }
		});

		verificationResult.SoundnessProperties.Should()
			.BeEquivalentTo(
				new SoundnessProperties(
					SoundnessType.RelaxedLazy,
					null!,
					true,
					["t4", "tau2", "t9", "tau6", "t10", "tau10", "t14", "tau12"],
					false,
					false),
				o => o.Excluding(sp => sp.StateTypes));
		verificationResult.SoundnessProperties.StateTypes.Should().Contain(st => st.Value.HasFlag(StateType.Final));
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

		var soundnessVerifier = new RelaxedLazySoundnessVerifier();
		var verificationResult = soundnessVerifier.Verify(dpn, new Dictionary<string, string>
		{
			{ RelaxedLazyVerificationSettingsConstants.BaseStructure, baseStructure }
		});

		verificationResult.SoundnessProperties.Should()
			.BeEquivalentTo(
				new SoundnessProperties(
					SoundnessType.RelaxedLazy,
					null!,
					true,
					["reset"],
					false,
					false),
				o => o.Excluding(sp => sp.StateTypes));
		verificationResult.SoundnessProperties.StateTypes.Should().Contain(st => st.Value.HasFlag(StateType.Final));
		verificationResult.StateSpaceGraph.Nodes.Should().NotBeEmpty();
		verificationResult.StateSpaceGraph.Arcs.Should().NotBeEmpty();
	}
	
	[Theory]
	public void VerifyUnboundedCoveringOutput_Should_ReturnIsUnsound(bool stopOnCoveringFinal)
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\Unbounded_covering_output.pnmlx", FileMode.Open);
		using var context = new Context();
		var dpn = pnmlxParser.Deserialize(fs, context);

		var soundnessVerifier = new RelaxedLazySoundnessVerifier();
		var verificationResult = soundnessVerifier.Verify(dpn, new Dictionary<string, string>
		{
			{ RelaxedLazyVerificationSettingsConstants.BaseStructure, baseStructure },
			{ RelaxedLazyVerificationSettingsConstants.StopOnCoveringFinalPosition, stopOnCoveringFinal.ToString() }
		});

		verificationResult.SoundnessProperties.Should()
			.BeEquivalentTo(
				new SoundnessProperties(
					SoundnessType.RelaxedLazy,
					null!,
					stopOnCoveringFinal, // Unboundedness can remain undetected if stopping on covering final, as is true for this case 
					Array.Empty<string>(),
					false,
					false),
				o => o.Excluding(sp => sp.StateTypes));
		verificationResult.SoundnessProperties.StateTypes.Should().Contain(st => st.Value.HasFlag(StateType.Final));
		verificationResult.StateSpaceGraph.Nodes.Should().NotBeEmpty();
		verificationResult.StateSpaceGraph.Arcs.Should().NotBeEmpty();
	}
}