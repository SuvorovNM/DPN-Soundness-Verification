using CsvHelper;
using DPN.Models;
using DPN.Parsers;
using System.Diagnostics;
using System.Globalization;
using System.IO.Pipes;
using System.Xml.Serialization;
using DPN.Experiments.Common;
using DPN.Experiments.Common.CsvClassMaps;
using DPN.Soundness;
using DPN.Soundness.Repair;
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
		const string VerificationParameters = "VerificationParameters";
		const string RepairParameters = "RepairParameters";
		private const int TimeoutInMinutes = 60;
		
		// Arguments sample: @"DpnFile \workingDirectory\Output\8fcd9437-a5ee-4277-87bc-6769d5aab87d.pnmlx OutputDirectory \Output VerificationAlgorithmTypeEnum ImprovedVersion SoundnessType Classical WithRepair False"
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
			var verificationParameters = new Dictionary<string, string>();
			var repairParameters = new Dictionary<string, string>();

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
					case VerificationParameters:
						var keyValuesForVerification = args[++index].Trim().Replace("\"", "").Split(' ');
						for (var i = 0; i < keyValuesForVerification.Length - 1; i += 2)
							verificationParameters.Add(keyValuesForVerification[i], keyValuesForVerification[i + 1]);
						break;
					case RepairParameters:
						var keyValuesForRepair = args[++index].Trim().Replace("\"", "").Split(' ');
						for (var i = 0; i < keyValuesForRepair.Length - 1; i += 2)
							repairParameters.Add(keyValuesForRepair[i], keyValuesForRepair[i + 1]);
						break;
					default:
						throw new ArgumentException("Parameter " + args[index] + " is not supported!");
				}

				index++;
			} while (index < args.Length);

			ArgumentNullException.ThrowIfNull(dpnFilePath);
			ArgumentNullException.ThrowIfNull(outputDirectory);

			var conditionsInfo = new ConditionsInfo
			{
				Boundedness = boundedness,
				Soundness = soundness,
				DeadTransitions = deadTransitions
			};

			var dpnToVerify = GetDpnToVerify(dpnFilePath);

			using var source = new CancellationTokenSource(TimeSpan.FromMinutes(30));
			MainVerificationInfo outputRow = null!;
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
					var dpnRepairer = new ClassicalSoundnessRepairer();
					repairResult = dpnRepairer.Repair(dpnToVerify, repairParameters);
				}

				outputRow = new MainVerificationInfo(
					dpnToVerify,
					satisfiesConditions,
					verificationResult,
					repairResult);
			}, source.Token);

			if (!verificationTask.Wait(TimeSpan.FromMinutes(TimeoutInMinutes)))
			{
				var conditionsCount = dpnToVerify
					.Transitions
					.Sum(x => AtomicFormulaCounter.CountAtomicFormulas(x.Guard.ActualConstraintExpression));
				var badCasesPath = Path.Combine(outputDirectory, "bad_cases.txt");
				File.AppendAllText(badCasesPath,
					$"{dpnToVerify.Name}, {dpnToVerify.Places.Count}, {dpnToVerify.Transitions.Count}, {dpnToVerify.Arcs.Count}, {dpnToVerify.Variables.GetAllVariables().Length}, {conditionsCount}, {verificationResult != null}\n");

				throw new TimeoutException($"Process requires more than {TimeoutInMinutes} minutes to verify soundness");
			}

			if (pipeClientHandle != null)
			{
				SendResultToPipe(pipeClientHandle, outputRow);
			}

			if (satisfiesConditions)
			{
				SaveResultInFile(verificationAlgorithmType, outputDirectory, outputRow);
				return 1;
			}

			return -1;
		}

		private static DataPetriNet GetDpnToVerify(string dpnFilePath)
		{
			using var fs = new FileStream(dpnFilePath, FileMode.Open);

			var parser = new PnmlxParser();
			var dpn = parser.Deserialize(fs, new Context());
			return dpn;
		}


		private static void SaveResultInFile(VerificationAlgorithmTypeEnum? verificationType, string? outputDirectory,
			MainVerificationInfo outputRow)
		{
			using var writer = new StreamWriter(outputDirectory + "/" + verificationType + ".csv", true);
			using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
			csv.Context.RegisterClassMap<VerificationOutputClassMap>();
			csv.WriteRecord(outputRow);
			csv.NextRecord();
		}

		private static void SendResultToPipe(string pipeClientHandle, MainVerificationInfo outputRow)
		{
			using PipeStream pipeClient = new AnonymousPipeClientStream(PipeDirection.Out, pipeClientHandle);
			using var sw = new StreamWriter(pipeClient);
			sw.AutoFlush = true;
			var serializer = new XmlSerializer(typeof(MainVerificationInfo));
			serializer.Serialize(sw, outputRow);
		}

		private static bool VerifyConditions(
			ConditionsInfo conditionsInfo, 
			int transitionsCount,
			VerificationResult verificationResult)
		{
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
	}
}