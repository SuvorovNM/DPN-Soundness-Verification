using CsvHelper;
using DataPetriNetOnSmt;
using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.SoundnessVerification.Services;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;
using DataPetriNetParsers;
using DataPetriNetVerificationDomain;
using DataPetriNetVerificationDomain.CsvClassMaps;
using Microsoft.Z3;
using System.Diagnostics;
using System.Globalization;
using System.IO.Pipes;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using DataPetriNetOnSmt.SoundnessVerification;

namespace DataPetriNetVerificationApplication
{
    internal class Program
    {
        const string BoundednessParameterName = nameof(ConditionsInfo.Boundedness);
        const string SoundnessParameterName = nameof(ConditionsInfo.Soundness);
        const string DeadTransitionsParameterName = nameof(ConditionsInfo.DeadTransitions);
        const string VerificationAlgorithmTypeParameterName = nameof(VerificationAlgorithmTypeEnum);
        const string PipeClientHandleParameterName = "PipeClientHandle";
        const string DpnFileParameterName = "DpnFile";
        const string OutputDirectoryParameterName = "OutputDirectory";
        const string SaveConstraintGraph = "SaveCG";
        private static Context context = new Context();
        private static TransformerToRefined transformation = new TransformerToRefined();

        static int Main(string[] args)
        {
            //args = @"DpnFile C:\Users\Admin\source\repos\DataPetriNet\DataPetriNetVerificationApplication\bin\Debug\net6.0\\dpn.pnmlx PipeClientHandle 1708 OutputDirectory C:\Users\Admin\source\repos\DataPetriNet\DataPetriNetIterativeVerificationApplication\bin\Debug\net6.0-windows\ VerificationTypeEnum QeWithoutTransformation".Split(" ");

            //args = @"DpnFile C:\Users\Admin\source\repos\DataPetriNet\DataPetriNetVerificationApplication\bin\Debug\net6.0\Error_vars.pnmlx PipeClientHandle 896 OutputDirectory C:\Users\Admin\source\repos\DataPetriNet\DataPetriNetIterativeVerificationApplication\bin\Debug\net6.0-windows\Output VerificationAlgorithmTypeEnum BaseVersion".Split(" ");

            //args = @"DpnFile C:\Users\Admin\source\repos\DataPetriNet\DataPetriNetIterativeVerificationApplication\bin\Release\net6.0-windows\Output\0a41b746-7a47-44b8-b9b0-2831de858668.pnmlx PipeClientHandle 2052 OutputDirectory C:\Users\Admin\source\repos\DataPetriNet\DataPetriNetIterativeVerificationApplication\bin\Debug\net6.0-windows\Output VerificationAlgorithmTypeEnum DirectVersion".Split(" "); ;

            bool? soundness = null;
            bool? boundedness = null;
            byte? deadTransitions = null;
            VerificationAlgorithmTypeEnum? verificationAlgorithmType = null;
            string? pipeClientHandle = null;
            string? dpnFilePath = null;
            string? outputDirectory = null;
            bool saveCG = false;

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
                    case VerificationAlgorithmTypeParameterName:
                        verificationAlgorithmType = Enum.Parse<VerificationAlgorithmTypeEnum>(args[++index]);
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
                        saveCG = bool.Parse(args[++index]);
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

            DataPetriNet dpnToVerify = GetDpnToVerify(dpnFilePath);
            ConstraintExpressionService constraintExpressionService = new ConstraintExpressionService(dpnToVerify.Context);

            CancellationTokenSource source = new CancellationTokenSource(TimeSpan.FromMinutes(30));
            MainVerificationInfo outputRow = null;
            var timer = new Stopwatch();

            timer.Start();

            long ltsTime = 0;
            long cgTime = 0;
            long cgRefinedTime = 0;
            var lts = new ClassicalLabeledTransitionSystem(dpnToVerify);
            ConstraintGraph? cg = null;
            ConstraintGraph? cgRefined = null;
            SoundnessProperties? soundnessProps = null;
            bool satisfiesConditions = false;

            var verificationTask = Task.Run(() =>
            {
                lts.GenerateGraph();
                soundnessProps = SoundnessAnalyzer.CheckSoundness(dpnToVerify, lts);
                timer.Stop();
                ltsTime = timer.ElapsedMilliseconds;

                if (verificationAlgorithmType == VerificationAlgorithmTypeEnum.ImprovedVersion)
                {
                    satisfiesConditions = VerifyConditions(conditionsInfo, dpnToVerify.Transitions.Count, soundnessProps);
                    if (satisfiesConditions && soundnessProps.Soundness)
                    {
                        timer.Restart();
                        cg = new ConstraintGraph(dpnToVerify);
                        cg.GenerateGraph();
                        soundnessProps = SoundnessAnalyzer.CheckSoundness(dpnToVerify, cg);
                        timer.Stop();
                        cgTime = timer.ElapsedMilliseconds;

                        satisfiesConditions = VerifyConditions(conditionsInfo, dpnToVerify.Transitions.Count, soundnessProps);
                        if (satisfiesConditions)
                        {
                            if (soundnessProps.Soundness)
                            {
                                timer.Restart();

                                (var dpnRefined, _) = transformation.TransformUsingLts(dpnToVerify, lts);
                                cgRefined = new ConstraintGraph(dpnRefined);
                                cgRefined.GenerateGraph();
                                soundnessProps = SoundnessAnalyzer.CheckSoundness(dpnRefined, cgRefined);
                                timer.Stop();
                                satisfiesConditions = VerifyConditions(conditionsInfo, dpnToVerify.Transitions.Count, soundnessProps);
                                cgRefinedTime = timer.ElapsedMilliseconds;
                            }
                        }
                    }

                    ConductSoundnessRepair(dpnToVerify, soundnessProps, out var repairTime, out var result);

                    satisfiesConditions &= repairTime != -1;

                    outputRow = new OptimizedVerificationOutput(
                        dpnToVerify,
                        satisfiesConditions,
                        lts,
                        cg,
                        cgRefined,
                        soundnessProps,
                        ltsTime,
                        cgTime,
                        cgRefinedTime,
                        repairTime,
                        result);
                }
                if (verificationAlgorithmType == VerificationAlgorithmTypeEnum.DirectVersion)
                {
                    long transformationTime = 0;
                    if (lts.IsFullGraph)
                    {
                        timer.Restart();
                        (var dpnRefined,_) = transformation.TransformUsingLts(dpnToVerify, lts);
                        timer.Stop();
                        transformationTime = timer.ElapsedMilliseconds;

                        timer.Restart();
                        cgRefined = new ConstraintGraph(dpnRefined);
                        cgRefined.GenerateGraph();
                        soundnessProps = SoundnessAnalyzer.CheckSoundness(dpnRefined, cgRefined);
                        timer.Stop();
                        cgRefinedTime = timer.ElapsedMilliseconds;

                        satisfiesConditions = VerifyConditions(conditionsInfo, dpnToVerify.Transitions.Count, soundnessProps);
                    }
                    else
                    {
                        soundnessProps = SoundnessAnalyzer.CheckSoundness(dpnToVerify, lts);
                        satisfiesConditions = VerifyConditions(conditionsInfo, dpnToVerify.Transitions.Count, soundnessProps);
                    }

                    ConductSoundnessRepair(dpnToVerify, soundnessProps, out var repairTime, out var result);

                    satisfiesConditions &= repairTime != -1;

                    outputRow = new BasicVerificationOutput(
                        dpnToVerify,
                        satisfiesConditions,
                        lts,
                        cgRefined,
                        soundnessProps,
                        ltsTime,
                        transformationTime,
                        cgRefinedTime,
                        repairTime,
                        result);
                }
            }, source.Token);
            if (!verificationTask.Wait(TimeSpan.FromMinutes(120)))
            {
                var conditionsCount = dpnToVerify
                    .Transitions
                    .SelectMany(x => x.Guard.BaseConstraintExpressions)
                    .Count();
                var badCasesPath = Path.Combine(outputDirectory, "bad_cases.txt");
                File.AppendAllText(badCasesPath, $"{dpnToVerify.Places.Count}, {dpnToVerify.Transitions.Count}, {dpnToVerify.Arcs.Count}, {dpnToVerify.Variables.GetAllVariables().Count}, {conditionsCount}\n");

                throw new TimeoutException("Process requires more than 20 minutes to verify soundness");
            }

            SendResultToPipe(pipeClientHandle, outputRow);

            if (satisfiesConditions)
            {
                SaveResultInFile(verificationAlgorithmType, outputDirectory, outputRow);

                if (saveCG)
                {
                    throw new NotImplementedException("Currently, it is prohibited to save CG!");
                }
                return 1;
            }
            return -1;
        }

        private static void ConductSoundnessRepair(DataPetriNet dpnToVerify, SoundnessProperties? soundnessProps, out long repairTime, out bool result)
        {
            repairTime = -1;
            result = false;
            if (soundnessProps.StateTypes.Any(state => state.Value == ConstraintStateType.Final))
            {
                var dpnRepairment = new Repairment();

                var repairStopwatch = new Stopwatch();
                repairStopwatch.Start();

                (_, var repairSteps, result) = dpnRepairment.RepairDpn(dpnToVerify);

                repairStopwatch.Stop();

                repairTime = repairStopwatch.ElapsedMilliseconds;
            }
        }

        private static DataPetriNet GetDpnToVerify(string dpnFilePath)
        {
            var xDoc = new XmlDocument();
            xDoc.Load(dpnFilePath);

            var parser = new PnmlParser();
            var dpn = parser.DeserializeDpn(xDoc);
            //dpn.Context = context;
            return dpn;
        }


        private static void SaveResultInFile(VerificationAlgorithmTypeEnum? verificationType, string? outputDirectory, MainVerificationInfo outputRow)
        {
            using (var writer = new StreamWriter(outputDirectory + "/" + verificationType.ToString() + ".csv", true))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                if (verificationType == VerificationAlgorithmTypeEnum.ImprovedVersion)
                {
                    csv.Context.RegisterClassMap<OptimizedVerificationOutputClassMap>();
                    csv.WriteRecord(outputRow);
                    csv.NextRecord();
                }
                if (verificationType == VerificationAlgorithmTypeEnum.DirectVersion)
                {
                    csv.Context.RegisterClassMap<BasicVerificationOutputClassMap>();
                    csv.WriteRecord(outputRow);
                    csv.NextRecord();
                }
            }
        }

        private static void SendResultToPipe(string pipeClientHandle, MainVerificationInfo outputRow)
        {
            using (PipeStream pipeClient =
                                new AnonymousPipeClientStream(PipeDirection.Out, pipeClientHandle))
            {
                using (StreamWriter sw = new StreamWriter(pipeClient))
                {
                    sw.AutoFlush = true;
                    XmlSerializer serializer = new XmlSerializer(typeof(MainVerificationInfo));//null
                    serializer.Serialize(sw, new MainVerificationInfo(outputRow));
                }
            }
        }

        private static bool VerifyConditions(ConditionsInfo conditionsInfo, int transitionsCount, SoundnessProperties soundnessProperties)
        {
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
                satisfiesConditions &= soundnessProperties.DeadTransitions.Length < (conditionsInfo.DeadTransitions.Value * transitionsCount / 100);
            }

            return satisfiesConditions;
        }

        private static DataPetriNet DeserializeDpn(string dpnFilePath)
        {
            DataPetriNet? deserializedDpn;

            var fileInfo = new FileInfo(dpnFilePath);
            if (fileInfo.Exists)
            {
                FileStream fs = new FileStream(dpnFilePath, FileMode.Open);
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(DataPetriNet));
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
