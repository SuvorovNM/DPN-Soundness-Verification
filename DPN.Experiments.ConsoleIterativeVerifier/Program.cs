// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Xml.Serialization;
using DataPetriNetIterativeVerificationApplication;
using DPN.Experiments.Common;
using DPN.Soundness;
using Newtonsoft.Json;

const string dirPath = @"C:\Users\Suvor\RiderProjects\DPN-Soundness-Verification\DPN.Experiments.IterativeVerificationApp\bin\Debug\net8.0-windows\Output\test\";
const string filePath = dirPath + "DirectVersion.csv";
const string outputDirPath = @"C:\Users\Suvor\RiderProjects\DPN-Soundness-Verification\DPN.Experiments.IterativeVerificationApp\bin\Debug\net8.0-windows\Output\console-test\";
const bool doRepair = true;
const SoundnessType soundnessType = SoundnessType.Classical;
const int NumberOfAttempts = 3;

var ids = File.ReadAllLines(filePath)
	.Select(l => l.Split(',').First())
	.ToHashSet();

foreach (var id in ids)
{
	for (var r = 0; r < NumberOfAttempts; r++)
	{
		await using var pipeServer = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
		var processInfo = FormProcessInfo(pipeServer, id);
		var listenTask = ListenToPipe(pipeServer, CancellationToken.None);
		var proc = Process.Start(processInfo)!;
		pipeServer.DisposeLocalCopyOfClientHandle();
		try
		{
			await proc.WaitForExitAsync();
			await listenTask;
		}
		catch (Exception)
		{
			proc.Kill();
			throw;
		}
	}
}

Console.WriteLine("Hello, World!");

ProcessStartInfo FormProcessInfo(AnonymousPipeServerStream serverPipe, string dpnId)
{
	const string VerificationAlgorithmTypeParameterName = nameof(VerificationAlgorithmTypeEnum);
	const string SoundnessTypeParameterName = nameof(SoundnessType);
	const string WithRepairParameterName = "WithRepair";
	const string PipeClientHandleParameterName = "PipeClientHandle";
	const string DpnFileParameterName = "DpnFile";
	const string OutputDirectoryParameterName = "OutputDirectory";
	
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
	var outputDirectoryPath = outputDirPath;
	var verificationAlgorithmType = nameof(VerificationAlgorithmTypeEnum.DirectVersion);

	var argumentsString = DpnFileParameterName + " " + (dirPath + dpnId)+".pnmlx" +
	                      " " + PipeClientHandleParameterName + " " + pipeHandle +
	                      " " + OutputDirectoryParameterName + " " + outputDirectoryPath +
	                      " " + VerificationAlgorithmTypeParameterName + " " + verificationAlgorithmType +
	                      " " + SoundnessTypeParameterName + " " + soundnessType +
	                      " " + WithRepairParameterName + " " + doRepair;

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

	var lastString = stringBuilder.ToString();//Encoding.UTF8.GetString(buffer);

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
		
		Console.WriteLine($"Executed the algorithm on the DPN with ID {verificationOutput.Id}. Verification time: {verificationOutput.VerificationTime}. Repair time: {verificationOutput.RepairTime}");
	}
}