using CsvHelper;
using DPN.Models;
using DPN.Models.Enums;
using DPN.Parsers;
using System.Diagnostics;
using System.Globalization;
using System.IO.Pipes;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using DPN.Experiments.Common;
using DPN.Experiments.Common.CsvClassMaps;
using DPN.Soundness;
using DPN.Soundness.Repair;
using DPN.Soundness.Transformations;
using DPN.Soundness.TransitionSystems;
using DPN.Soundness.Verification;
using Microsoft.Z3;

namespace DataPetriNetVerificationApplication
{
	internal class Program
	{
		const string BoundednessParameterName = nameof(ConditionsInfo.Boundedness);
		const string SoundnessParameterName = nameof(ConditionsInfo.Soundness);
		const string DeadTransitionsParameterName = nameof(ConditionsInfo.DeadTransitions);
		const string VerificationAlgorithmTypeParameterName = nameof(VerificationAlgorithmTypeEnum);
		const string SoundnessTypeParameterName = nameof(SoundnessType);
		const string WithRepairParameterName = "WithRepair";
		const string PipeClientHandleParameterName = "PipeClientHandle";
		const string DpnFileParameterName = "DpnFile";
		const string OutputDirectoryParameterName = "OutputDirectory";
		const string SaveConstraintGraph = "SaveCG";
		const string VerificationParameters = "VerificationParameters";
		const string RepairParameters = "RepairParameters";

		// TODO: логировать данные по операциям > n минут. Подумать, можем ли мы как-то описать этап, на котором встали. В целом, можно их просто сохранять, а потом руками посмотреть, но может быть долго
		// Можем писать в лог статусы по каждой доверенности, но как простым образом обеспечить логирование? Прокидывать везде ILogger?
		// For without IterativeVerificationApp pass the args in the format (splitting by " "):
		// TODO: проверять relaxed lazy нужно с завершением при покрытии o
		//@"DpnFile \workingDirectory\Output\8fcd9437-a5ee-4277-87bc-6769d5aab87d.pnmlx OutputDirectory \Output VerificationAlgorithmTypeEnum ImprovedVersion SoundnessType Classical WithRepair False"
		static int Main(string[] args)
		{
			bool? soundness = null;
			bool? boundedness = null;
			byte? deadTransitions = null;
			VerificationAlgorithmTypeEnum? verificationAlgorithmType = null;
			string? pipeClientHandle = null;
			string? dpnFilePath = null;
			string? outputDirectory = null;
			var soundnessType = SoundnessType.Classical;
			var withRepair = false;
			var saveCg = false;
			var verificationParameters = new Dictionary<string, string>();
			var repairParameters = new Dictionary<string, string>();

			//args = @"DpnFile C:\Users\Suvor\RiderProjects\DPN-Soundness-Verification\DPN.Experiments.IterativeVerificationApp\bin\Debug\net8.0-windows\Output\7e066b47-6b41-4c4d-b2b9-cce2422655a2.pnmlx PipeClientHandle 2656 OutputDirectory C:\Users\Suvor\RiderProjects\DPN-Soundness-Verification\DPN.Experiments.IterativeVerificationApp\bin\Debug\net8.0-windows\Output VerificationAlgorithmTypeEnum ImprovedVersion SoundnessType Classical WithRepair False".Split();
			//args = @"DpnFile C:\Users\Suvor\RiderProjects\DPN-Soundness-Verification\DPN.Experiments.IterativeVerificationApp\bin\Debug\net8.0-windows\Output\test\a2ddc07c-fc36-453c-adfd-9ca2fe4dc5d0.pnmlx PipeClientHandle 772 OutputDirectory C:\Users\Suvor\RiderProjects\DPN-Soundness-Verification\DPN.Experiments.IterativeVerificationApp\bin\Debug\net8.0-windows\Output\console-test\ VerificationAlgorithmTypeEnum DirectVersion SoundnessType Classical WithRepair True".Split();

			var index = 0;
			do
			{
				switch (args[index])
				{
					case BoundednessParameterName:
						boundedness = bool.Parse(args[++index]);
						break;
					case SoundnessParameterName:
						soundness = bool.Parse(args[++index]);
						break;
					case DeadTransitionsParameterName:
						deadTransitions = byte.Parse(args[++index]);
						break;
					case SoundnessTypeParameterName:
						soundnessType = Enum.Parse<SoundnessType>(args[++index], true);
						break;
					case WithRepairParameterName:
						withRepair = bool.Parse(args[++index]);
						break;
					case VerificationAlgorithmTypeParameterName:
						verificationAlgorithmType = Enum.Parse<VerificationAlgorithmTypeEnum>(args[++index], true);
						break;
					case PipeClientHandleParameterName:
						pipeClientHandle = args[++index];
						break;
					case DpnFileParameterName:
						dpnFilePath = args[++index];
						break;
					case OutputDirectoryParameterName:
						outputDirectory = args[++index];
						break;
					case SaveConstraintGraph:
						saveCg = bool.Parse(args[++index]);
						break;
					case VerificationParameters:
						var keyValuesForVerification = args[++index].Trim().Replace("\"", "").Split(' ');
						for (int i = 0; i < keyValuesForVerification.Length - 1; i += 2)
							verificationParameters.Add(keyValuesForVerification[i], keyValuesForVerification[i + 1]);
						break;
					case RepairParameters:
						var keyValuesForRepair = args[++index].Trim().Replace("\"", "").Split(' ');
						for (int i = 0; i < keyValuesForRepair.Length - 1; i += 2)
							repairParameters.Add(keyValuesForRepair[i], keyValuesForRepair[i + 1]);
						break;
					default:
						throw new ArgumentException("Parameter " + args[index] + " is not supported!");
				}

				index++;
			} while (index < args.Length);

			ArgumentNullException.ThrowIfNull(dpnFilePath);
			ArgumentNullException.ThrowIfNull(outputDirectory);
			
			Global.SetParameter("parallel.enable", "true");

			var conditionsInfo = new ConditionsInfo
			{
				Boundedness = boundedness,
				Soundness = soundness,
				DeadTransitions = deadTransitions
			};

			var dpnToVerify = GetDpnToVerify(dpnFilePath);

			using var source = new CancellationTokenSource(TimeSpan.FromMinutes(30));
			MainVerificationInfo outputRow = null;
			var timer = new Stopwatch();

			timer.Start();
			var satisfiesConditions = false;

			VerificationResult? verificationResult = null;

			var verificationTask = Task.Run(() =>
			{
				timer.Start();
				switch (soundnessType)
				{
					case SoundnessType.RelaxedLazy:
					{
						var soundnessVerifier = new RelaxedLazySoundnessVerifier();
						//verificationParameters[RelaxedLazyVerificationSettingsConstants.StopOnCoveringFinalPosition] = "false";
						verificationResult = soundnessVerifier.Verify(dpnToVerify, verificationParameters);
						break;
					}
					case SoundnessType.Classical:
					{
						var soundnessVerifier = new ClassicalSoundnessVerifier();
						verificationResult = soundnessVerifier.Verify(dpnToVerify, verificationParameters);
						break;
					}
					case SoundnessType.None:
					default:
						throw new ArgumentException("Soundness type is either not defined or not supported!");
				}

				satisfiesConditions = VerifyConditions(
					conditionsInfo,
					dpnToVerify.Transitions.Count,
					verificationResult);
				
				RepairResult? repairResult = null;
				if (withRepair && satisfiesConditions)
				{
					repairResult = ConductSoundnessRepairIfAnyPathToFinal(dpnToVerify, verificationResult.SoundnessProperties, repairParameters);
					satisfiesConditions &= repairResult != null;
				}

				outputRow = new MainVerificationInfo(
					dpnToVerify,
					satisfiesConditions,
					verificationResult,
					repairResult);
			}, source.Token);

			if (!verificationTask.Wait(TimeSpan.FromMinutes(25)))
			{
				var conditionsCount = dpnToVerify
					.Transitions
					.Sum(x => AtomicFormulaCounter.CountAtomicFormulas(x.Guard.BaseConstraintExpressions));
				var badCasesPath = Path.Combine(outputDirectory, "bad_cases.txt");
				File.AppendAllText(badCasesPath,
					$"{dpnToVerify.Name}, {dpnToVerify.Places.Count}, {dpnToVerify.Transitions.Count}, {dpnToVerify.Arcs.Count}, {dpnToVerify.Variables.GetAllVariables().Length}, {conditionsCount}, {verificationResult != null}\n");

				throw new TimeoutException("Process requires more than 25 minutes to verify soundness");
			}

			if (pipeClientHandle != null)
			{
				SendResultToPipe(pipeClientHandle, outputRow!);
			}

			if (satisfiesConditions)
			{
				SaveResultInFile(verificationAlgorithmType, outputDirectory, outputRow);

				if (saveCg)
				{
					throw new NotImplementedException("Currently, it is prohibited to save CG!");
				}

				return 1;
			}

			return -1;
		}

		private static RepairResult? ConductSoundnessRepairIfAnyPathToFinal(
			DataPetriNet dpnToVerify, 
			SoundnessProperties soundnessProps,
			Dictionary<string,string> repairParameters)
		{
			//if (soundnessProps.StateTypes.Any(state => state.Value == StateType.Final))
			{
				var dpnRepairer = new ClassicalSoundnessRepairer();
				return dpnRepairer.Repair(dpnToVerify, repairParameters);
			}

			return null;
		}

		private static DataPetriNet GetDpnToVerify(string dpnFilePath)
		{
			using var fs = new FileStream(dpnFilePath, FileMode.Open);

			var parser = new PnmlxParser();
			var dpn = parser.Deserialize(fs);
			return dpn;
		}


		private static void SaveResultInFile(VerificationAlgorithmTypeEnum? verificationType, string? outputDirectory,
			MainVerificationInfo outputRow)
		{
			using var writer = new StreamWriter(outputDirectory + "/" + verificationType.ToString() + ".csv", true);
			using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
			csv.Context.RegisterClassMap<VerificationOutputClassMap>();
			csv.WriteRecord(outputRow);
			csv.NextRecord();
		}

		private static void SendResultToPipe(string pipeClientHandle, MainVerificationInfo outputRow)
		{
			using PipeStream pipeClient = new AnonymousPipeClientStream(PipeDirection.Out, pipeClientHandle);
			using var sw = new StreamWriter(pipeClient);
			//using var sw = new StreamWriter("C:\\workspace\\text.txt");
			sw.AutoFlush = true;
			var serializer = new XmlSerializer(typeof(MainVerificationInfo)); //null
			serializer.Serialize(sw, outputRow);
		}

		private static bool VerifyConditions(
			ConditionsInfo conditionsInfo, 
			int transitionsCount,
			VerificationResult verificationResult)
		{
			/*var notCoverOutput = verificationResult.StateSpaceGraph.Nodes
				.All(n => n.Marking["o"] <= 1);
			var existsPathToFinal = verificationResult.StateSpaceGraph.Nodes
				.Any(n => n.Marking["o"] == 1 && n.Marking.All(p=> p.Key == "o" || p.Value == 0));// && n.Marking.All(p=> p.Key == "o" || p.Value == 0)
			if (!existsPathToFinal || !notCoverOutput)
			{
				return false;
			}*/

			/*if (!verificationResult.StateSpaceGraph.Nodes
				     .Any(n => n.Marking["o"] == 1 && n.Marking.All(p => p.Key == "o" || p.Value == 0)))
			{
				return false;
			}*/
			
			var soundnessProperties = verificationResult.SoundnessProperties;
			var satisfiesConditions = true;
			if (conditionsInfo.Boundedness.HasValue)
			{
				satisfiesConditions &= soundnessProperties.Boundedness == conditionsInfo.Boundedness.Value;
			}

			if (conditionsInfo.Soundness.HasValue)
			{
				satisfiesConditions &= soundnessProperties.Soundness == conditionsInfo.Soundness.Value;
			}

			if (conditionsInfo.DeadTransitions.HasValue)
			{
				satisfiesConditions &= soundnessProperties.DeadTransitions.Length <
				                       (conditionsInfo.DeadTransitions.Value * transitionsCount / 100);
			}

			return satisfiesConditions;
		}

		private static DataPetriNet DeserializeDpn(string dpnFilePath)
		{
			DataPetriNet? deserializedDpn;

			var fileInfo = new FileInfo(dpnFilePath);
			if (fileInfo.Exists)
			{
				var fs = new FileStream(dpnFilePath, FileMode.Open);
				try
				{
					var serializer = new XmlSerializer(typeof(DataPetriNet));
					deserializedDpn = (DataPetriNet?)serializer.Deserialize(fs);
				}
				catch (SerializationException e)
				{
					Console.WriteLine("Failed to deserialize. Reason: " + e.Message);
					throw;
				}
				finally
				{
					fs.Close();
				}
			}
			else
			{
				throw new FileNotFoundException(dpnFilePath);
			}

			return deserializedDpn != null
				? deserializedDpn
				: throw new ArgumentNullException(nameof(deserializedDpn));
		}
	}
}