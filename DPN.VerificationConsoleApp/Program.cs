using DPN.Models;
using DPN.Parsers;
using System.Diagnostics;
using System.Xml;
using DPN.Models.Enums;
using DPN.Soundness;
using DPN.Soundness.Repair;
using DPN.Soundness.TransitionSystems.StateSpace;
using DPN.Soundness.Verification;

namespace DPNConsoleApp
{
	internal class Program
	{
		private const string OperationParameter = "Operation";
		private const string DpnFileParameter = "DpnFile";
		private const string OutputDirectoryParameter = "OutputDirectory";
		private const string SoundnessTypeParameter = "SoundnessType";
		private const string VerificationParameters = "VerificationParameters";
		private const string RepairParameters = "RepairParameters";
		private const string SaveStateSpaceParameter = "SaveStateSpace";
		private const string Verbose = "Verbose";

		static int Main(string[] args)
		{
			if (args.Length == 0)
			{
				PrintUsage();
				return 0;
			}

			try
			{
				var parameters = ParseArguments(args);
				return ExecuteOperation(parameters);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error: {ex.Message}");
				return -1;
			}
		}

		private static void PrintUsage()
		{
			Console.WriteLine("DPN Verification and Repair Console Application");
			Console.WriteLine();
			Console.WriteLine("Usage:");
			Console.WriteLine("  --Operation <Verify|Repair>");
			Console.WriteLine("  --DpnFile <path_to_dpn_file>");
			Console.WriteLine("  --OutputDirectory <output_directory>");
			Console.WriteLine("  --SoundnessType <Classical|RelaxedLazy>");
			Console.WriteLine("  --SaveStateSpace <true|false>");
			Console.WriteLine("  --VerificationParameters \"<key1> <value1> <key2> <value2>...\"");
			Console.WriteLine("  --RepairParameters \"<key1> <value1> <key2> <value2>...\"");
			Console.WriteLine("  --Verbose");
			Console.WriteLine();
			Console.WriteLine("Examples:");
			Console.WriteLine("  --Operation Verify --DpnFile model.pnmlx --OutputDirectory ./results --SoundnessType RelaxedLazy --Verbose");
			Console.WriteLine("  --Operation Repair --DpnFile model.pnmlx --OutputDirectory ./results --SoundnessType Classical");
		}

		private static Dictionary<string, string> ParseArguments(string[] args)
		{
			var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			var index = 0;

			while (index < args.Length)
			{
				if (args[index].StartsWith("--"))
				{
					var key = args[index][2..];
					if (index + 1 < args.Length && !args[index + 1].StartsWith("--"))
					{
						parameters[key] = args[++index];
					}
					else
					{
						parameters[key] = "true";
					}
				}

				index++;
			}

			return parameters;
		}

		private static int ExecuteOperation(Dictionary<string, string> parameters)
		{
			if (!parameters.TryGetValue(OperationParameter, out var operation) ||
			    !parameters.TryGetValue(DpnFileParameter, out var dpnFilePath) ||
			    !parameters.TryGetValue(OutputDirectoryParameter, out var outputDirectory))
			{
				throw new ArgumentException("Missing required parameters: Operation, DpnFile, OutputDirectory");
			}

			Directory.CreateDirectory(outputDirectory);

			var soundnessType = parameters.TryGetValue(SoundnessTypeParameter, out var soundnessTypeStr)
				? Enum.Parse<SoundnessType>(soundnessTypeStr, true)
				: SoundnessType.Classical;

			var saveStateSpace = parameters.TryGetValue(SaveStateSpaceParameter, out var saveStateSpaceStr)
			                     && bool.Parse(saveStateSpaceStr);

			var isVerbose = parameters.TryGetValue(Verbose, out var verbose) && bool.Parse(verbose);

			var verificationParameters = ParseKeyValueParameters(parameters, VerificationParameters);
			var repairParameters = ParseKeyValueParameters(parameters, RepairParameters);

			var dpnToProcess = GetDpnToVerify(dpnFilePath);

			Console.WriteLine($"Processing DPN: {dpnFilePath}");
			Console.WriteLine($"Places: {dpnToProcess.Places.Count}, Transitions: {dpnToProcess.Transitions.Count}, Variables: {dpnToProcess.Variables.GetAllVariables().Length}");

			switch (operation.ToLowerInvariant())
			{
				case "verify":
					return VerifyDpn(dpnToProcess, soundnessType, verificationParameters, outputDirectory, saveStateSpace, isVerbose);
				case "repair":
					var result = RepairDpn(dpnToProcess, soundnessType, repairParameters, outputDirectory);
					if (saveStateSpace && result == 1)
					{
						Console.WriteLine("To examine the state space of the repaired DPN, call verify on it");
					}

					return result;
				default:
					throw new ArgumentException($"Unknown operation: {operation}. Supported operations: Verify, Repair");
			}
		}

		private static int VerifyDpn(
			DataPetriNet dpn,
			SoundnessType soundnessType,
			Dictionary<string, string> verificationParameters,
			string outputDirectory,
			bool saveStateSpace,
			bool isVerbose)
		{
			Console.WriteLine("Starting verification...");

			var timer = Stopwatch.StartNew();
			VerificationResult verificationResult;

			switch (soundnessType)
			{
				case SoundnessType.RelaxedLazy:
					var relaxedSoundnessVerifier = new RelaxedLazySoundnessVerifier();
					verificationResult = relaxedSoundnessVerifier.Verify(dpn, verificationParameters);
					break;
				case SoundnessType.Classical:
					var classicalSoundnessVerifier = new ClassicalSoundnessVerifier();
					verificationResult = classicalSoundnessVerifier.Verify(dpn, verificationParameters);
					break;
				default:
					throw new ArgumentException($"Unsupported soundness type: {soundnessType}");
			}

			timer.Stop();

			Console.WriteLine($"Verification completed in {timer.Elapsed.TotalSeconds:F2} seconds");
			Console.WriteLine($"Soundness: {verificationResult.SoundnessProperties.Soundness}");
			Console.WriteLine($"Boundedness: {verificationResult.SoundnessProperties.Boundedness}");
			Console.WriteLine($"Dead transitions: {verificationResult.SoundnessProperties.DeadTransitions?.Length ?? 0}");
			if (soundnessType == SoundnessType.Classical)
			{
				Console.WriteLine($"Deadlocks: {verificationResult.SoundnessProperties.Deadlocks}");
			}

			if (isVerbose)
			{
				var nodes = verificationResult.StateSpaceGraph.Nodes.ToDictionary(n => n.Id, n => n);
				var groupedStates = verificationResult.SoundnessProperties.StateTypes
					.GroupBy(s => s.Value)
					.OrderBy(g => g.Key);

				foreach (var stateType in groupedStates)
				{
					switch (stateType.Key)
					{
						case ConstraintStateType.Default or ConstraintStateType.Initial:
							continue;
						case ConstraintStateType.Final:
							Console.WriteLine($"Finals:");
							break;
						case ConstraintStateType.Deadlock:
							Console.WriteLine($"Deadlocks:");
							break;
						case ConstraintStateType.NoWayToFinalMarking:
							Console.WriteLine($"No way to finals:");
							break;
						case ConstraintStateType.UncleanFinal:
							Console.WriteLine($"Unclean finals:");
							break;
						case ConstraintStateType.StrictlyCovered when soundnessType == SoundnessType.Classical:
							Console.WriteLine($"Strictly covered:");
							break;
					}

					foreach (var final in stateType)
					{
						var node = nodes[final.Key];
						Console.WriteLine($"[{final.Key}] {node.Marking} {node.StateConstraint}");
					}
				}
			}

			if (saveStateSpace)
			{
				SaveStateSpace(verificationResult.StateSpaceGraph, outputDirectory);
			}

			return verificationResult.SoundnessProperties?.Soundness == true ? 1 : -1;
		}

		private static int RepairDpn(
			DataPetriNet dpn,
			SoundnessType soundnessType,
			Dictionary<string, string> repairParameters,
			string outputDirectory)
		{
			if (soundnessType != SoundnessType.Classical)
			{
				throw new ArgumentException("Unsupported soundness type for repair.");
			}

			Console.WriteLine("Starting repair...");

			var dpnRepairer = new ClassicalSoundnessRepairer();
			var repairResult = dpnRepairer.Repair(dpn, repairParameters);

			Console.WriteLine($"Repair result: {(repairResult.IsSuccess ? "Success" : "Failure")}. Repair steps: {repairResult.RepairSteps}. Repair time: {repairResult.RepairTime}");

			if (repairResult.IsSuccess)
			{
				SaveDpn(repairResult.Dpn, outputDirectory);
			}

			return repairResult.IsSuccess ? 1 : -1;
		}

		private static DataPetriNet GetDpnToVerify(string dpnFilePath)
		{
			var xDoc = new XmlDocument();
			xDoc.Load(dpnFilePath);

			var parser = new PnmlParser();
			return parser.Deserialize(xDoc);
		}

		private static Dictionary<string, string> ParseKeyValueParameters(Dictionary<string, string> parameters, string parameterName)
		{
			var result = new Dictionary<string, string>();
			if (parameters.TryGetValue(parameterName, out var keyValueString))
			{
				var keyValues = keyValueString.Trim().Replace("\"", "").Split(' ');
				for (int i = 0; i < keyValues.Length - 1; i += 2)
				{
					result.Add(keyValues[i], keyValues[i + 1]);
				}
			}

			return result;
		}

		private static void SaveStateSpace(StateSpaceGraph stateSpaceGraph, string outputDirectory)
		{
			var stateSpacePath = Path.Combine(outputDirectory, "state_space.xml");
			Console.WriteLine($"State space saving to {stateSpacePath}");

			var asmlParser = new AsmlParser();
			asmlParser.Serialize(stateSpaceGraph).Save(stateSpacePath);
		}

		private static void SaveDpn(DataPetriNet dataPetriNet, string outputDirectory)
		{
			var stateSpacePath = Path.Combine(outputDirectory, $"{dataPetriNet.Name}-repaired.xml");
			Console.WriteLine($"State space saving to {stateSpacePath}");

			var pnmlParser = new PnmlParser();
			pnmlParser.Serialize(dataPetriNet).Save(stateSpacePath);
		}
	}
}