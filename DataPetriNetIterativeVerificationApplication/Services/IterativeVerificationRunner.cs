using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Pipes;
using DataPetriNetGeneration;
using Microsoft.Z3;
using DataPetriNetVerificationDomain;
using Newtonsoft.Json;
using System.Xml.Serialization;
using DataPetriNetOnSmt;
using System.Runtime.Serialization;
using System.Diagnostics;
using System.Threading;
using DataPetriNetIterativeVerificationApplication.Extensions;
using DataPetriNetParsers;
using System.Xml.Linq;
using System.Collections.ObjectModel;

namespace DataPetriNetIterativeVerificationApplication.Services
{
    public class IterativeVerificationRunner
    {
        const string BoundednessParameterName = nameof(ConditionsInfo.Boundedness);
        const string SoundnessParameterName = nameof(ConditionsInfo.Soundness);
        const string DeadTransitionsParameterName = nameof(ConditionsInfo.DeadTransitions);
        const string VerificationTypeParameterName = nameof(VerificationTypeEnum);
        const string PipeClientHandleParameterName = "PipeClientHandle";
        const string DpnFileParameterName = "DpnFile";
        const string OutputDirectoryParameterName = "OutputDirectory";
        const string DpnFileSuffix = "\\dpn.pnmlx";

        private DPNGenerator dpnGenerator;
        private PnmlParser parser;
        private Context context;
        public IterativeVerificationRunner()
        {
            context = new Context();
            dpnGenerator = new DPNGenerator(context);
            parser = new PnmlParser();
        }
        public async Task RunVerificationLoop(
            VerificationInput verificationInput,
            ObservableCollection<VerificationOutputWithNumber> currentverificationResults,
            CancellationToken token)
        {
            ProcessPath processPath;
            using (StreamReader r = new StreamReader("Configuration.json"))
            {
                string json = r.ReadToEnd();
                processPath = JsonConvert.DeserializeObject<ProcessPath>(json);
            }

            for (int n = verificationInput.IterationsInfo.InitialN; true; n += verificationInput.IterationsInfo.IncrementValue)
            {
                for (int i = 0; i < verificationInput.IterationsInfo.DpnsPerConfiguration; i++)
                {
                    var successfulCase = false;
                    var verificationTypes = verificationInput.VerificationType
                        .GetFlags()
                        .Select(x => (VerificationTypeEnum)x)
                        .Except(new List<VerificationTypeEnum> { VerificationTypeEnum.None })
                        .ToList();

                    do
                    {
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }

                        var dpn = dpnGenerator.Generate(
                            (int)(verificationInput.DpnInfo.Places * n),
                            (int)(verificationInput.DpnInfo.Transitions * n),
                            (int)(verificationInput.DpnInfo.ExtraArcs * n),
                            (int)(verificationInput.DpnInfo.Variables * n),
                            (int)(verificationInput.DpnInfo.Conditions * n));
                        dpn.Name = Guid.NewGuid().ToString();

                        var dpnPath = Path.Combine(verificationInput.OutputDirectory, dpn.Name + ".pnml");
                        Directory.CreateDirectory(verificationInput.OutputDirectory);

                        FileStream fs = new FileStream(dpnPath, FileMode.Create);
                        try
                        {
                            var xmlDocument = parser.SerializeDpn(dpn);
                            await xmlDocument.SaveAsync(fs, SaveOptions.None, token);
                        }
                        finally
                        {
                            fs.Close();
                        }

                        Process proc = null;
                        using (var pipeServer = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable))
                        {

                            var processInfo = FormProcessInfo(verificationInput, verificationTypes[0], dpnPath, pipeServer, processPath);
                            var listenTask = ListenToPipe(pipeServer, currentverificationResults, token);
                            proc = Process.Start(processInfo);
                            pipeServer.DisposeLocalCopyOfClientHandle();
                            await proc.WaitForExitAsync(token);
                            await listenTask;
                        }

                        successfulCase = proc?.ExitCode >= 0;
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
                                    proc = Process.Start(processInfo);
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

        private async Task ListenToPipe(
            AnonymousPipeServerStream pipeStream,
            ObservableCollection<VerificationOutputWithNumber> currentverificationResults,
            CancellationToken token)
        {
            byte[] buffer = new byte[65535];
            await pipeStream.ReadAsync(buffer, 0, buffer.Length, token);
            string lastString = Encoding.UTF8.GetString(buffer);

            VerificationOutput? verificationOutput = null;

            XmlSerializer serializer = new XmlSerializer(typeof(VerificationOutput));
            using (TextReader reader = new StringReader(lastString))
            {
                verificationOutput = (VerificationOutput?)serializer.Deserialize(reader);
            }
            if (verificationOutput != null)
            {
                currentverificationResults.Add(new VerificationOutputWithNumber(verificationOutput, currentverificationResults.Count));
            }
        }

        private ProcessStartInfo FormProcessInfo(
            VerificationInput verificationInput,
            VerificationTypeEnum currentVerificationType,
            string dpnFilePath,
            AnonymousPipeServerStream serverPipe,
            ProcessPath path)
        {
            var processInfo = new ProcessStartInfo();
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = true;
            processInfo.RedirectStandardError = true;
            processInfo.FileName = path.DPNVerificationApplicationPath;
            processInfo.WorkingDirectory = path.DPNVerificationApplicationWorkingDir;
            processInfo.ErrorDialog = true;
            processInfo.CreateNoWindow = true;

            var pipeHandle = serverPipe.GetClientHandleAsString();
            var outputDirectoryPath = verificationInput.OutputDirectory;
            var verificationType = currentVerificationType.ToString();

            var argumentsString = DpnFileParameterName + " " + dpnFilePath +
                " " + PipeClientHandleParameterName + " " + pipeHandle +
                " " + OutputDirectoryParameterName + " " + outputDirectoryPath +
                " " + VerificationTypeParameterName + " " + verificationType;

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
