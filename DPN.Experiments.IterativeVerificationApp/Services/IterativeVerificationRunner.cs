using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Pipes;
using DataPetriNetGeneration;
using Microsoft.Z3;
using Newtonsoft.Json;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Threading;
using DPN.Parsers;
using System.Xml.Linq;
using System.Collections.ObjectModel;
using DPN.Experiments.Common;
using DPN.Models;
using DPN.Soundness;

namespace DataPetriNetIterativeVerificationApplication.Services
{
	public class VerificationRunner
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

		private readonly DPNGenerator dpnGenerator;
		private readonly PnmlParser parser;

		public VerificationRunner()
		{
			dpnGenerator = new DPNGenerator(new Context());
			parser = new PnmlParser();
		}

		public async Task RunRandomVerificationLoop(
			VerificationInputForRandom verificationInput,
			ObservableCollection<VerificationOutputWithNumber> currentverificationResults,
			CancellationToken token)
		{
			var processPath = GetProcessPath();
			var rnd = new Random();

			try
			{
				while (true)
				{
					bool successfulCase;

					do
					{
						if (token.IsCancellationRequested)
						{
							return;
						}


						var placesCount = rnd.Next(verificationInput.MinPlaces, verificationInput.MaxPlaces + 1);
						var transitionsCount = rnd.Next(verificationInput.MinTransitions, verificationInput.MaxTransitions + 1);
						var arcsCount = rnd.Next(verificationInput.MinArcs, verificationInput.MaxArcs + 1);
						var varsCount = rnd.Next(verificationInput.MinVars, verificationInput.MaxVars + 1);
						var conditionsCount = rnd.Next(verificationInput.MinConditions, verificationInput.MaxConditions + 1);
						var soundnessPreference = verificationInput.ConditionsInfo.Soundness.GetValueOrDefault();

						var dpn = dpnGenerator.Generate(
							placesCount,
							transitionsCount,
							arcsCount,
							varsCount,
							conditionsCount,
							soundnessPreference);
						dpn.Name = Guid.NewGuid().ToString();
						var dpnPath = await SaveDpnToXml(verificationInput, dpn, token);

						Process proc;
						await using (var pipeServer = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable))
						{
							// Consider here base version as well?
							var processInfo = FormProcessInfo(verificationInput, VerificationAlgorithmTypeEnum.ImprovedVersion, dpnPath, pipeServer, processPath);
							var listenTask = ListenToPipe(pipeServer, currentverificationResults, token);
							proc = Process.Start(processInfo)!;
							pipeServer.DisposeLocalCopyOfClientHandle();
							try
							{
								await proc.WaitForExitAsync(token);
								await listenTask;
							}
							catch (OperationCanceledException)
							{
								proc.Kill();
								throw;
							}
						}

						successfulCase = proc.ExitCode >= 0;
					} while (!successfulCase);
				}
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex.Message);
			}
		}

		private async Task<string> SaveDpnToXml(VerificationInputBasis verificationInput, DataPetriNet dpn, CancellationToken token)
		{
			var dpnPath = Path.Combine(verificationInput.OutputDirectory, dpn.Name + ".pnmlx");
			Directory.CreateDirectory(verificationInput.OutputDirectory);

			var fs = new FileStream(dpnPath, FileMode.Create);
			try
			{
				var xmlDocument = parser.SerializeDpn(dpn);
				await xmlDocument.SaveAsync(fs, SaveOptions.None, token);
			}
			finally
			{
				fs.Close();
			}

			return dpnPath;
		}

		public async Task RunIterativeVerificationLoop(
			VerificationInputForIterative verificationInput,
			ObservableCollection<VerificationOutputWithNumber> currentverificationResults,
			CancellationToken token)
		{
			var processPath = GetProcessPath();

			for (int n = verificationInput.IterationsInfo.InitialN;; n += verificationInput.IterationsInfo.IncrementValue)
			{
				for (var i = 0; i < verificationInput.IterationsInfo.DpnsPerConfiguration; i++)
				{
					bool successfulCase;
					var verificationTypes = new List<VerificationAlgorithmTypeEnum>
					{
						VerificationAlgorithmTypeEnum.ImprovedVersion,
						//VerificationAlgorithmTypeEnum.DirectVersion                        
					};

					do
					{
						if (token.IsCancellationRequested)
						{
							return;
						}

						var soundnessPreference = verificationInput.ConditionsInfo.Soundness.GetValueOrDefault();

						var dpn = dpnGenerator.Generate(
							(int)Math.Round(verificationInput.DpnInfo.Places * n),
							(int)Math.Round(verificationInput.DpnInfo.Transitions * n),
							(int)Math.Round(verificationInput.DpnInfo.ExtraArcs * n),
							(int)Math.Round(verificationInput.DpnInfo.Variables * n),
							(int)Math.Round(verificationInput.DpnInfo.Conditions * n),
							soundnessPreference);
						dpn.Name = Guid.NewGuid().ToString();

						var dpnPath = await SaveDpnToXml(verificationInput, dpn, token);

						Process? proc;
						await using (var pipeServer = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable))
						{
							var processInfo = FormProcessInfo(verificationInput, verificationTypes[0], dpnPath, pipeServer, processPath);
							var listenTask = ListenToPipe(pipeServer, currentverificationResults, token);
							proc = Process.Start(processInfo)!;
							pipeServer.DisposeLocalCopyOfClientHandle();
							try
							{
								await proc.WaitForExitAsync(token);
								await listenTask;
							}
							catch (Exception)
							{
								proc.Kill();
								throw;
							}
						}

						successfulCase = proc.ExitCode == 1;
						if (successfulCase)
						{
							foreach (var verificationType in verificationTypes.Skip(1))
							{
								if (token.IsCancellationRequested)
								{
									return;
								}

								using (var pipeServer = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable))
								{
									var processInfo = FormProcessInfo(verificationInput, verificationType, dpnPath, pipeServer, processPath);
									var listenTask = ListenToPipe(pipeServer, currentverificationResults, token);
									proc = Process.Start(processInfo)!;
									pipeServer.DisposeLocalCopyOfClientHandle();
									await proc.WaitForExitAsync(token);
									await listenTask;
								}
							}
						}
					} while (!successfulCase);
				}
			}
		}

		private static ProcessPath GetProcessPath()
		{
			ProcessPath processPath;
			using (var r = new StreamReader("Configuration.json"))
			{
				processPath = JsonConvert.DeserializeObject<ProcessPath>(r.ReadToEnd())!;
			}

			return new ProcessPath
			{
				DPNVerificationApplicationPath = Path.GetFullPath(processPath.DPNVerificationApplicationPath),
				DPNVerificationApplicationWorkingDir = Path.GetFullPath(processPath.DPNVerificationApplicationWorkingDir)
			};
		}

		private static async Task ListenToPipe(
			AnonymousPipeServerStream pipeStream,
			ObservableCollection<VerificationOutputWithNumber> currentVerificationResults,
			CancellationToken token)
		{
			var buffer = new byte[655350];
			await pipeStream.ReadExactlyAsync(buffer, 0, buffer.Length, token);
			var lastString = Encoding.UTF8.GetString(buffer);

			MainVerificationInfo? verificationOutput = null;

			if (lastString != string.Empty)
			{
				var serializer = new XmlSerializer(typeof(MainVerificationInfo));
				using (TextReader reader = new StringReader(lastString))
				{
					try
					{
						verificationOutput = (MainVerificationInfo?)serializer.Deserialize(reader);
					}
					catch (Exception ex)
					{
						await Console.Error.WriteLineAsync(ex.Message);
					}
				}

				if (verificationOutput != null)
				{
					currentVerificationResults.Add(new VerificationOutputWithNumber(verificationOutput, currentVerificationResults.Count));
				}
			}
		}

		private ProcessStartInfo FormProcessInfo(
			VerificationInputBasis verificationInput,
			VerificationAlgorithmTypeEnum currentVerificationAlgorithmType,
			string dpnFilePath,
			AnonymousPipeServerStream serverPipe,
			ProcessPath path)
		{
			var processInfo = new ProcessStartInfo
			{
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				FileName = path.DPNVerificationApplicationPath,
				WorkingDirectory = Path.GetDirectoryName(path.DPNVerificationApplicationPath),
				ErrorDialog = true,
				CreateNoWindow = true
			};

			var pipeHandle = serverPipe.GetClientHandleAsString();
			var outputDirectoryPath = verificationInput.OutputDirectory;
			var verificationAlgorithmType = currentVerificationAlgorithmType.ToString();

			var argumentsString = DpnFileParameterName + " " + dpnFilePath +
			                      " " + PipeClientHandleParameterName + " " + pipeHandle +
			                      " " + OutputDirectoryParameterName + " " + outputDirectoryPath +
			                      " " + VerificationAlgorithmTypeParameterName + " " + verificationAlgorithmType +
			                      " " + SoundnessTypeParameterName + " " + verificationInput.SoundnessType +
			                      " " + WithRepairParameterName + " " + verificationInput.WithRepair;

			if (verificationInput.ConditionsInfo.DeadTransitions != null)
			{
				argumentsString += " " + DeadTransitionsParameterName + " " + verificationInput.ConditionsInfo.DeadTransitions.Value;
			}

			if (verificationInput.ConditionsInfo.Boundedness != null)
			{
				argumentsString += " " + BoundednessParameterName + " " + verificationInput.ConditionsInfo.Boundedness.Value;
			}

			if (verificationInput.ConditionsInfo.Soundness != null)
			{
				argumentsString += " " + SoundnessParameterName + " " + verificationInput.ConditionsInfo.Soundness.Value;
			}

			processInfo.Arguments = argumentsString;

			return processInfo;
		}
	}
}