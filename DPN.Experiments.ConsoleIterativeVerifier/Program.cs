// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Xml.Serialization;
using DataPetriNetIterativeVerificationApplication;
using DPN.Experiments.Common;
using DPN.Soundness;
using Newtonsoft.Json;

const string inputDirectoryPath = @"InputDirectory";
const string inputFilePath = inputDirectoryPath + "DirectVersion.csv";
const string outputDirectoryPath = @"OutputDirectory";
const bool doRepair = true;
const SoundnessType soundnessType = SoundnessType.Classical;
const int numberOfAttempts = 3;

var ids = File.ReadAllLines(inputFilePath)
	.Select(l => l.Split(',').First())
	.ToHashSet();

foreach (var id in ids)
{
	try
	{
		File.Copy(inputDirectoryPath + id + ".pnmlx", outputDirectoryPath + id + ".pnmlx");
	}
	catch (Exception ex)
	{
		Console.WriteLine(ex.Message);
	}

	Console.WriteLine($"{DateTime.Now}: Starting working on {id}");
	for (var r = 0; r < numberOfAttempts; r++)
	{
		await using var pipeServer = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
		var processInfo = FormProcessInfo(pipeServer, id);
		var listenTask = ListenToPipe(pipeServer, CancellationToken.None);
		var proc = Process.Start(processInfo)!;
		pipeServer.DisposeLocalCopyOfClientHandle();
		try
		{
			await proc.WaitForExitAsync();
			if (proc.ExitCode < 0)
			{
				break;
			}

			await listenTask;
		}
		catch (Exception)
		{
			proc.Kill();
			throw;
		}
	}
}

Console.WriteLine("Finished execution!");

ProcessStartInfo FormProcessInfo(AnonymousPipeServerStream serverPipe, string dpnId)
{
	const string verificationAlgorithmTypeParameterName = nameof(VerificationAlgorithmTypeEnum);
	const string soundnessTypeParameterName = nameof(SoundnessType);
	const string withRepairParameterName = "WithRepair";
	const string pipeClientHandleParameterName = "PipeClientHandle";
	const string dpnFileParameterName = "DpnFile";
	const string outputDirectoryParameterName = "OutputDirectory";

	ProcessPath processPath;
	using (var r = new StreamReader("Configuration.json"))
	{
		var paths = JsonConvert.DeserializeObject<ProcessPath>(r.ReadToEnd())!;
		processPath = new ProcessPath
		{
			DPNVerificationApplicationPath = Path.GetFullPath(paths.DPNVerificationApplicationPath),
			DPNVerificationApplicationWorkingDir = Path.GetFullPath(paths.DPNVerificationApplicationWorkingDir)
		};
	}

	var processInfo = new ProcessStartInfo
	{
		UseShellExecute = false,
		RedirectStandardOutput = true,
		RedirectStandardError = true,
		FileName = processPath.DPNVerificationApplicationPath,
		WorkingDirectory = Path.GetDirectoryName(processPath.DPNVerificationApplicationPath),
		ErrorDialog = true,
		CreateNoWindow = true
	};

	var pipeHandle = serverPipe.GetClientHandleAsString();
	var verificationAlgorithmType = nameof(VerificationAlgorithmTypeEnum.DirectVersion);

	var argumentsString = dpnFileParameterName + " " + (inputDirectoryPath + dpnId) + ".pnmlx" +
	                      " " + pipeClientHandleParameterName + " " + pipeHandle +
	                      " " + outputDirectoryParameterName + " " + outputDirectoryPath +
	                      " " + verificationAlgorithmTypeParameterName + " " + verificationAlgorithmType +
	                      " " + soundnessTypeParameterName + " " + soundnessType +
	                      " " + withRepairParameterName + " " + doRepair;

	processInfo.Arguments = argumentsString;

	return processInfo;
}

static async Task ListenToPipe(
	AnonymousPipeServerStream pipeStream,
	CancellationToken token)
{
	var buffer = new byte[65536];
	var stringBuilder = new StringBuilder();

	var endOfStream = false;
	while (!endOfStream)
	{
		int bytesRead = await pipeStream.ReadAsync(buffer, token);
		endOfStream = bytesRead == 0;

		if (bytesRead > 0)
		{
			stringBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
		}
	}

	var lastString = stringBuilder.ToString();

	MainVerificationInfo? verificationOutput;

	if (lastString != string.Empty)
	{
		var serializer = new XmlSerializer(typeof(MainVerificationInfo));
		using TextReader reader = new StringReader(lastString);
		try
		{
			verificationOutput = (MainVerificationInfo)serializer.Deserialize(reader)!;
			Console.WriteLine($"{DateTime.Now}: Executed the algorithm on the DPN with ID {verificationOutput.Id}. Verification time: {verificationOutput.VerificationTime}. Repair time: {verificationOutput.RepairTime}");
		}
		catch (Exception ex)
		{
			await Console.Error.WriteLineAsync(ex.Message);
		}
	}
}