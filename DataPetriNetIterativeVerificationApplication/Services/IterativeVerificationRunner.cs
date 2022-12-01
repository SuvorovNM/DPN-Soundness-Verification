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
    public class VerificationRunner
    {
        const string BoundednessParameterName = nameof(ConditionsInfo.Boundedness);
        const string SoundnessParameterName = nameof(ConditionsInfo.Soundness);
        const string DeadTransitionsParameterName = nameof(ConditionsInfo.DeadTransitions);
        const string VerificationTypeParameterName = nameof(VerificationTypeEnum);
        const string VerificationAlgorithmTypeParameterName = nameof(VerificationAlgorithmTypeEnum);
        const string PipeClientHandleParameterName = "PipeClientHandle";
        const string DpnFileParameterName = "DpnFile";
        const string OutputDirectoryParameterName = "OutputDirectory";
        const string DpnFileSuffix = "\\dpn.pnmlx";

        private DPNGenerator dpnGenerator;
        private PnmlParser parser;
        private Context context;
        public VerificationRunner()
        {
            context = new Context();
            dpnGenerator = new DPNGenerator(context);
            parser = new PnmlParser();
        }

        public async Task RunRandomVerificationLoop(
            VerificationInputForRandom verificationInput,
            ObservableCollection<VerificationOutputWithNumber> currentverificationResults,
            CancellationToken token)
        {
            var processPath = GetProcessPath();
            var rnd = new Random();

            while (true)
            {
                var successfulCase = false;

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

                    var dpn = dpnGenerator.Generate(
                        placesCount,
                        transitionsCount,
                        arcsCount,
                        varsCount,
                        conditionsCount);
                    dpn.Name = Guid.NewGuid().ToString();
                    var dpnPath = await SaveDpnToXml(verificationInput, dpn, token);

                    Process proc = null;
                    using (var pipeServer = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable))
                    {
                        // Consider here base version as well?
                        var processInfo = FormProcessInfo(verificationInput, VerificationAlgorithmTypeEnum.OptimizedVersion, dpnPath, pipeServer, processPath);
                        var listenTask = ListenToPipe(pipeServer, currentverificationResults, token);
                        proc = Process.Start(processInfo);
                        pipeServer.DisposeLocalCopyOfClientHandle();
                        try
                        {
                            await proc.WaitForExitAsync(token);
                            await listenTask;
                        }
                        catch (OperationCanceledException ex)
                        {
                            proc.Kill();
                            throw;
                        }
                    }

                    successfulCase = proc?.ExitCode >= 0;
                } while (!successfulCase);



            }
        }

        private async Task<string> SaveDpnToXml(VerificationInputBasis verificationInput, DataPetriNet dpn, CancellationToken token)
        {
            var dpnPath = Path.Combine(verificationInput.OutputDirectory, dpn.Name + ".pnmlx");
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

            return dpnPath;
        }

        public async Task RunIterativeVerificationLoop(
            VerificationInputForIterative verificationInput,
            ObservableCollection<VerificationOutputWithNumber> currentverificationResults,
            CancellationToken token)
        {
            var processPath = GetProcessPath();

            for (int n = verificationInput.IterationsInfo.InitialN; true; n += verificationInput.IterationsInfo.IncrementValue)
            {
                for (int i = 0; i < verificationInput.IterationsInfo.DpnsPerConfiguration; i++)
                {
                    var successfulCase = false;
                    var verificationTypes = new List<VerificationAlgorithmTypeEnum> 
                    {
                        VerificationAlgorithmTypeEnum.OptimizedVersion,
                        VerificationAlgorithmTypeEnum.BaseVersion                        
                    };

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

                        var dpnPath = await SaveDpnToXml(verificationInput, dpn, token);

                        Process proc = null;
                        using (var pipeServer = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable))
                        {

                            var processInfo = FormProcessInfo(verificationInput, verificationTypes[0], dpnPath, pipeServer, processPath);
                            var listenTask = ListenToPipe(pipeServer, currentverificationResults, token);
                            proc = Process.Start(processInfo);
                            pipeServer.DisposeLocalCopyOfClientHandle();
                            try
                            {
                                await proc.WaitForExitAsync(token);
                                await listenTask;
                            }
                            catch (Exception ex)//OperationCanceledException
                            {
                                proc.Kill();
                                throw;
                            }
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

        private static ProcessPath GetProcessPath()
        {
            ProcessPath processPath;
            using (StreamReader r = new StreamReader("Configuration.json"))
            {
                string json = r.ReadToEnd();
                processPath = JsonConvert.DeserializeObject<ProcessPath>(json);
            }

            return processPath;
        }

        private async Task ListenToPipe(
            AnonymousPipeServerStream pipeStream,
            ObservableCollection<VerificationOutputWithNumber> currentverificationResults,
            CancellationToken token)
        {
            byte[] buffer = new byte[655350];
            string lastString = string.Empty;
            await pipeStream.ReadAsync(buffer, 0, buffer.Length, token);
            lastString = Encoding.UTF8.GetString(buffer);

            MainVerificationInfo? verificationOutput = null;

            if (lastString != string.Empty)
            {
                XmlSerializer serializer = new XmlSerializer(typeof(MainVerificationInfo));
                using (TextReader reader = new StringReader(lastString))
                {
                    try
                    {
                        verificationOutput = (MainVerificationInfo?)serializer.Deserialize(reader);
                    }
                    catch (Exception ex)
                    {

                    }
                }
                if (verificationOutput != null)
                {
                    currentverificationResults.Add(new VerificationOutputWithNumber(verificationOutput, currentverificationResults.Count));
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
            var verificationAlgorithmType = currentVerificationAlgorithmType.ToString();

            var argumentsString = DpnFileParameterName + " " + dpnFilePath +
                " " + PipeClientHandleParameterName + " " + pipeHandle +
                " " + OutputDirectoryParameterName + " " + outputDirectoryPath +
                " " + VerificationAlgorithmTypeParameterName + " " + verificationAlgorithmType;

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
