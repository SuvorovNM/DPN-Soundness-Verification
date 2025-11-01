using System.Diagnostics;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using DPN.Models;
using DPN.Models.Enums;
using DPN.Parsers;
using DPN.Soundness;
using DPN.Soundness.Repair;
using DPN.Soundness.TransitionSystems;
using DPN.Soundness.TransitionSystems.StateSpace;
using DPN.Soundness.Verification;

namespace DPN.VerificationConsoleApp
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
			    !parameters.TryGetValue(DpnFileParameter, out var dpnFilePath))
			{
				throw new ArgumentException("Missing required parameters: Operation, DpnFile, OutputDirectory");
			}

			parameters.TryGetValue(OutputDirectoryParameter, out var outputDirectory);

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
					if (outputDirectory == null && saveStateSpace)
					{
						throw new ArgumentException("Output directory cannot be null.");
					}
					
					return VerifyDpn(dpnToProcess, soundnessType, verificationParameters, outputDirectory, saveStateSpace, isVerbose);
				case "repair":
					if (outputDirectory == null)
					{
						throw new ArgumentException("Output directory cannot be null.");
					}
					if (soundnessType != SoundnessType.Classical)
					{
						throw new ArgumentException("Unsupported soundness type for repair.");
					}
					
					var result = RepairDpn(dpnToProcess, repairParameters, outputDirectory);
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
			string? outputDirectory,
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

			Console.WriteLine($"Verification time: {timer.Elapsed.TotalSeconds.ToString("0.00", CultureInfo.InvariantCulture)} seconds");
			Console.WriteLine($"Soundness: {verificationResult.SoundnessProperties.Soundness}");
			Console.WriteLine($"Boundedness: {verificationResult.SoundnessProperties.Boundedness}");
			Console.WriteLine($"Dead transitions: {(verificationResult.SoundnessProperties.DeadTransitions.Length == 0 ? "Absent" :string.Join(", ", verificationResult.SoundnessProperties.DeadTransitions))}");
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
						case StateType.Default or StateType.Initial:
							continue;
						case StateType.Final:
							Console.WriteLine($"Finals:");
							break;
						case StateType.Deadlock:
							Console.WriteLine($"Deadlocks:");
							break;
						case StateType.NoWayToFinalMarking:
							Console.WriteLine($"No way to finals:");
							break;
						case StateType.UncleanFinal:
							Console.WriteLine($"Unclean finals:");
							break;
						case StateType.StrictlyCovered when soundnessType == SoundnessType.Classical:
							Console.WriteLine($"Strictly covered:");
							break;
					}

					foreach (var state in stateType)
					{
						var node = nodes[state.Key];
						Console.WriteLine($"{state.Key}: [{string.Join(" ",node.Marking.Where(kvp=>kvp.Value > 0).Select(kvp=>$"{(kvp.Value > 1 ? kvp.Value : "")}{kvp.Key}"))}] {node.StateConstraint}");
					}
				}
			}

			if (saveStateSpace)
			{
				SaveStateSpace(verificationResult.StateSpaceGraph, outputDirectory!);
			}

			return verificationResult.SoundnessProperties.Soundness ? 1 : -1;
		}

		private static int RepairDpn(
			DataPetriNet dpn,
			Dictionary<string, string> repairParameters,
			string outputDirectory)
		{
			Console.WriteLine("Starting repair...");

			var dpnRepairer = new ClassicalSoundnessRepairer();
			var repairResult = dpnRepairer.Repair(dpn, repairParameters);

			Console.WriteLine($"Repair result: {(repairResult.IsSuccess ? "Success" : "Failure")} \nRepair steps: {repairResult.RepairSteps} \nRepair time: {repairResult.RepairTime.TotalSeconds.ToString("0.00", CultureInfo.InvariantCulture)} seconds");

			if (repairResult.IsSuccess)
			{
				SaveRepairedDpn(repairResult.Dpn, outputDirectory);
			}

			return repairResult.IsSuccess ? 1 : -1;
		}

		private static DataPetriNet GetDpnToVerify(string dpnFilePath)
		{
			var xDocument = XDocument.Load(dpnFilePath);

			var parser = new PnmlxParser();
			return parser.Deserialize(xDocument);
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

			var asmlParser = new AsmlParser();
			asmlParser.Serialize(stateSpaceGraph).Save(stateSpacePath);
			Console.WriteLine($"State space saved to {stateSpacePath}");
		}

		private static void SaveRepairedDpn(DataPetriNet dataPetriNet, string outputDirectory)
		{
			Directory.CreateDirectory(outputDirectory);
			
			var stateSpacePath = Path.Combine(outputDirectory, $"{dataPetriNet.Name}-repaired.pnmlx");

			var pnmlParser = new PnmlxParser();
			pnmlParser.Serialize(dataPetriNet).Save(stateSpacePath);
			Console.WriteLine($"Repaired DPN saved to {stateSpacePath}");
		}
	}
}