using DPN.Parsers;
using DPN.Soundness;
using DPN.Soundness.Repair;
using DPN.Soundness.TransitionSystems;
using DPN.Soundness.Verification;
using FluentAssertions;
using Microsoft.Z3;

namespace DPN.Tests;

[TestFixture(false, false)]
[TestFixture(false, true)]
[TestFixture(true, false)]
[TestFixture(true, true)]
public class ClassicalSoundnessRepair_Test
{
	private bool mergeTransitionsBack;
	private bool tryRollbackTransitions;

	public ClassicalSoundnessRepair_Test(bool mergeTransitionsBack, bool tryRollbackTransitions)
	{
		this.mergeTransitionsBack = mergeTransitionsBack;
		this.tryRollbackTransitions = tryRollbackTransitions;
	}

	[Test]
	public void RepairCreditBankRequest_Should_ReturnSoundModel()
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\CreditBankRequest.pnmlx", FileMode.Open);
		using var context = new Context();
		var dpn = pnmlxParser.Deserialize(fs, context);

		var soundnessRepairer = new ClassicalSoundnessRepairer();
		var repairResult = soundnessRepairer.Repair(dpn, new Dictionary<string, string>
		{
			{ ClassicalRepairSettingsConstants.MergeTransitionsBack, mergeTransitionsBack.ToString() },
			{ ClassicalRepairSettingsConstants.TryRollbackRestrictions, tryRollbackTransitions.ToString() }
		});

		repairResult.IsSuccess.Should().BeTrue();
		repairResult.RepairSteps.Should().Be(2);
		repairResult.RepairModifications.EnhancedTransitions.Should().BeEquivalentTo(tryRollbackTransitions ? ["t6"] : ["t6", "t7"]);
		repairResult.RepairModifications.TransitionEnhancedButRolledBack.Should().BeEquivalentTo(tryRollbackTransitions ? ["t7"] : []);

		var soundnessVerifier = new ClassicalSoundnessVerifier();
		var verificationResult = soundnessVerifier.Verify(repairResult.Dpn, new Dictionary<string, string>());
		verificationResult.SoundnessProperties.Soundness.Should().BeTrue();
	}
	
	[Test]
	public void RepairLivelock_Should_ReturnSoundModel()
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\Livelock.pnmlx", FileMode.Open);
		using var context = new Context();
		var dpn = pnmlxParser.Deserialize(fs, context);

		var soundnessRepairer = new ClassicalSoundnessRepairer();
		var repairResult = soundnessRepairer.Repair(dpn, new Dictionary<string, string>
		{
			{ ClassicalRepairSettingsConstants.MergeTransitionsBack, mergeTransitionsBack.ToString() },
			{ ClassicalRepairSettingsConstants.TryRollbackRestrictions, tryRollbackTransitions.ToString() }
		});

		repairResult.IsSuccess.Should().BeTrue();
		repairResult.RepairSteps.Should().Be(1);
		repairResult.RepairModifications.EnhancedTransitions.Should().BeEquivalentTo(["t1"]);
		repairResult.RepairModifications.TransitionEnhancedButRolledBack.Should().BeEmpty();
		repairResult.Dpn.Transitions.Any(t => t.IsSplit).Should().Be(!mergeTransitionsBack);

		var soundnessVerifier = new ClassicalSoundnessVerifier();
		var verificationResult = soundnessVerifier.Verify(repairResult.Dpn, new Dictionary<string, string>());
		verificationResult.SoundnessProperties.Soundness.Should().BeTrue();
	}
	
	[Test]
	public void RepairCasino_Should_ReturnSoundModel()
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\Casino.pnmlx", FileMode.Open);
		using var context = new Context();
		var dpn = pnmlxParser.Deserialize(fs, context);

		var soundnessRepairer = new ClassicalSoundnessRepairer();
		var repairResult = soundnessRepairer.Repair(dpn, new Dictionary<string, string>
		{
			{ ClassicalRepairSettingsConstants.MergeTransitionsBack, mergeTransitionsBack.ToString() },
			{ ClassicalRepairSettingsConstants.TryRollbackRestrictions, tryRollbackTransitions.ToString() }
		});

		repairResult.IsSuccess.Should().BeTrue();
		repairResult.RepairSteps.Should().Be(1);
		repairResult.RepairModifications.EnhancedTransitions.Should().BeEquivalentTo(["t1"]);
		repairResult.RepairModifications.TransitionEnhancedButRolledBack.Should().BeEmpty();

		var soundnessVerifier = new ClassicalSoundnessVerifier();
		var verificationResult = soundnessVerifier.Verify(repairResult.Dpn, new Dictionary<string, string>());
		verificationResult.SoundnessProperties.Soundness.Should().BeTrue();
	}
	
	[Test]
	public void RepairUnbounded_Should_ReturnSoundModel()
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\Unbounded.pnmlx", FileMode.Open);
		using var context = new Context();
		var dpn = pnmlxParser.Deserialize(fs, context);

		var soundnessRepairer = new ClassicalSoundnessRepairer();
		var repairResult = soundnessRepairer.Repair(dpn, new Dictionary<string, string>
		{
			{ ClassicalRepairSettingsConstants.MergeTransitionsBack, mergeTransitionsBack.ToString() },
			{ ClassicalRepairSettingsConstants.TryRollbackRestrictions, tryRollbackTransitions.ToString() }
		});

		repairResult.IsSuccess.Should().BeTrue();
		repairResult.RepairSteps.Should().Be(1);
		repairResult.RepairModifications.EnhancedTransitions.Should().BeEquivalentTo(["t2"]);
		repairResult.RepairModifications.TransitionEnhancedButRolledBack.Should().BeEmpty();

		var soundnessVerifier = new ClassicalSoundnessVerifier();
		var verificationResult = soundnessVerifier.Verify(repairResult.Dpn, new Dictionary<string, string>());
		verificationResult.SoundnessProperties.Soundness.Should().BeTrue();
	}
	
	[Test]
	public void RepairGambling_Should_ReturnFailure()
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\Gambling.pnmlx", FileMode.Open);
		using var context = new Context();
		var dpn = pnmlxParser.Deserialize(fs, context);

		var soundnessRepairer = new ClassicalSoundnessRepairer();
		var repairResult = soundnessRepairer.Repair(dpn, new Dictionary<string, string>
		{
			{ ClassicalRepairSettingsConstants.MergeTransitionsBack, mergeTransitionsBack.ToString() },
			{ ClassicalRepairSettingsConstants.TryRollbackRestrictions, tryRollbackTransitions.ToString() }
		});

		repairResult.IsSuccess.Should().BeFalse();
		repairResult.RepairSteps.Should().Be(1);
		repairResult.RepairModifications.EnhancedTransitions.Should().BeEmpty();
		repairResult.RepairModifications.TransitionEnhancedButRolledBack.Should().BeEmpty();
		repairResult.Dpn.Should().BeEquivalentTo(dpn);
	}
	
	[Test]
	public void RepairRoadFines_Should_ReturnSoundModel()
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\RoadFines.pnmlx", FileMode.Open);
		using var context = new Context();
		var dpn = pnmlxParser.Deserialize(fs, context);

		var soundnessRepairer = new ClassicalSoundnessRepairer();
		var repairResult = soundnessRepairer.Repair(dpn, new Dictionary<string, string>
		{
			{ ClassicalRepairSettingsConstants.MergeTransitionsBack, mergeTransitionsBack.ToString() },
			{ ClassicalRepairSettingsConstants.TryRollbackRestrictions, tryRollbackTransitions.ToString() }
		});

		repairResult.IsSuccess.Should().BeTrue();
		repairResult.RepairSteps.Should().Be(1);
		repairResult.RepairModifications.EnhancedTransitions.Should().BeEquivalentTo(["n17", "n15"]);
		repairResult.RepairModifications.TransitionEnhancedButRolledBack.Should().BeEmpty();

		var soundnessVerifier = new ClassicalSoundnessVerifier();
		var verificationResult = soundnessVerifier.Verify(repairResult.Dpn, new Dictionary<string, string>());
		verificationResult.SoundnessProperties.Soundness.Should().BeTrue();
	}
	
	[Test]
	public void RepairDigitalWhiteboardTransfer_Should_ReturnSoundModel()
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\DigitalWhiteboard_Transfer.pnmlx", FileMode.Open);
		using var context = new Context();
		var dpn = pnmlxParser.Deserialize(fs, context);

		var soundnessRepairer = new ClassicalSoundnessRepairer();
		var repairResult = soundnessRepairer.Repair(dpn, new Dictionary<string, string>
		{
			{ ClassicalRepairSettingsConstants.MergeTransitionsBack, mergeTransitionsBack.ToString() },
			{ ClassicalRepairSettingsConstants.TryRollbackRestrictions, tryRollbackTransitions.ToString() }
		});

		repairResult.IsSuccess.Should().BeTrue();
		repairResult.RepairSteps.Should().Be(4);
		repairResult.RepairModifications.EnhancedTransitions.Should().BeEquivalentTo(tryRollbackTransitions ? ["bed1"] : ["bed1", "bed2", "eom1", "eom2"]);
		repairResult.RepairModifications.TransitionEnhancedButRolledBack.Should().BeEquivalentTo(tryRollbackTransitions ? ["bed2", "eom1", "eom2"] : []);

		var soundnessVerifier = new ClassicalSoundnessVerifier();
		var verificationResult = soundnessVerifier.Verify(repairResult.Dpn, new Dictionary<string, string>());
		verificationResult.SoundnessProperties.Soundness.Should().BeTrue();
	}
	
	[Test]
	public void RepairPackageHandling_Should_ReturnSoundModel()
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\PackageHandling.pnmlx", FileMode.Open);
		using var context = new Context();
		var dpn = pnmlxParser.Deserialize(fs, context);

		var soundnessRepairer = new ClassicalSoundnessRepairer();
		var repairResult = soundnessRepairer.Repair(dpn, new Dictionary<string, string>
		{
			{ ClassicalRepairSettingsConstants.MergeTransitionsBack, mergeTransitionsBack.ToString() },
			{ ClassicalRepairSettingsConstants.TryRollbackRestrictions, tryRollbackTransitions.ToString() }
		});

		repairResult.IsSuccess.Should().BeTrue();
		repairResult.RepairSteps.Should().Be(0);
		repairResult.RepairModifications.EnhancedTransitions.Should().BeEquivalentTo(["t4", "tau2", "t9", "tau6", "t10", "tau10", "t14", "tau12"]);
		repairResult.RepairModifications.TransitionEnhancedButRolledBack.Should().BeEmpty();

		var soundnessVerifier = new ClassicalSoundnessVerifier();
		var verificationResult = soundnessVerifier.Verify(repairResult.Dpn, new Dictionary<string, string>());
		verificationResult.SoundnessProperties.Soundness.Should().BeTrue();
	}
	
	[Test]
	public void RepairSimpleAuction_Should_ReturnSoundModel()
	{
		var pnmlxParser = new PnmlxParser();
		using var fs = new FileStream("TestData\\SimpleAuction.pnmlx", FileMode.Open);
		using var context = new Context();
		var dpn = pnmlxParser.Deserialize(fs, context);

		var soundnessRepairer = new ClassicalSoundnessRepairer();
		var repairResult = soundnessRepairer.Repair(dpn, new Dictionary<string, string>
		{
			{ ClassicalRepairSettingsConstants.MergeTransitionsBack, mergeTransitionsBack.ToString() },
			{ ClassicalRepairSettingsConstants.TryRollbackRestrictions, tryRollbackTransitions.ToString() }
		});

		repairResult.IsSuccess.Should().BeTrue();
		repairResult.RepairSteps.Should().Be(1);
		repairResult.RepairModifications.EnhancedTransitions.Should().BeEquivalentTo(["dec", "reset"]);
		repairResult.RepairModifications.TransitionEnhancedButRolledBack.Should().BeEmpty();

		var soundnessVerifier = new ClassicalSoundnessVerifier();
		var verificationResult = soundnessVerifier.Verify(repairResult.Dpn, new Dictionary<string, string>());
		verificationResult.SoundnessProperties.Soundness.Should().BeTrue();
	}
}