using CsvHelper;
using DataPetriNetOnSmt;
using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.SoundnessVerification;
using DataPetriNetOnSmt.SoundnessVerification.Services;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;
using DataPetriNetParsers;
using DataPetriNetVerificationDomain;
using DataPetriNetVerificationDomain.ConstraintGraphVisualized;
using DataPetriNetVerificationDomain.CsvClassMaps;
using Microsoft.Z3;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO.Pipes;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace DataPetriNetVerificationApplication
{
    internal class Program
    {
        const string BoundednessParameterName = nameof(ConditionsInfo.Boundedness);
        const string SoundnessParameterName = nameof(ConditionsInfo.Soundness);
        const string DeadTransitionsParameterName = nameof(ConditionsInfo.DeadTransitions);
        const string VerificationTypeParameterName = nameof(VerificationTypeEnum);
        const string PipeClientHandleParameterName = "PipeClientHandle";
        const string DpnFileParameterName = "DpnFile";
        const string OutputDirectoryParameterName = "OutputDirectory";
        const string SaveConstraintGraph = "SaveCG";
        private static Context context = new Context();
        private static TransformationToRefined transformation = new TransformationToRefined();

        static int Main(string[] args)
        {
            //args = @"DpnFile C:\Users\Admin\source\repos\DataPetriNet\DataPetriNetVerificationApplication\bin\Debug\net6.0\\dpn.pnmlx PipeClientHandle 1708 OutputDirectory C:\Users\Admin\source\repos\DataPetriNet\DataPetriNetIterativeVerificationApplication\bin\Debug\net6.0-windows\ VerificationTypeEnum QeWithoutTransformation".Split(" ");

            bool? soundness = null;
            bool? boundedness = null;
            byte? deadTransitions = null;
            VerificationTypeEnum? verificationType = null;
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
                    case VerificationTypeParameterName:
                        verificationType = Enum.Parse<VerificationTypeEnum>(args[++index]);
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
            AbstractConstraintExpressionService constraintExpressionService = new ConstraintExpressionOperationServiceWithEqTacticConcat(dpnToVerify.Context);

            CancellationTokenSource source = new CancellationTokenSource(TimeSpan.FromMinutes(30));
            var timer = new Stopwatch();

            timer.Start();
            (var dpnRefined, var lts) = transformation.Transform(dpnToVerify);
            timer.Stop();
            var millisecondsToTransform = timer.ElapsedMilliseconds;

            VerificationOutput outputRow;
            bool satisfiesConditions;
            SoundnessProperties soundnessProperties;
            LabeledTransitionSystem ltsToSave;

            if (!lts.IsFullGraph)
            {
                soundnessProperties = ConstraintGraphAnalyzer.CheckSoundness(dpnToVerify, lts);

                satisfiesConditions = VerifyConditions(conditionsInfo, dpnToVerify.Transitions.Count, soundnessProperties);

                outputRow = new VerificationOutput(
                    dpnToVerify,
                    verificationType.Value,
                    satisfiesConditions,
                    lts,
                    new ConstraintGraph(),
                    soundnessProperties,
                    -1,
                    lts.Milliseconds);

                ltsToSave = lts;
            }
            else
            {
                timer.Start();
                var cgRefined = new ConstraintGraph(dpnRefined, constraintExpressionService);

                var task = Task.Run(() => cgRefined.GenerateGraph(), source.Token);

                if (task.Wait(TimeSpan.FromMinutes(30)))
                {
                    soundnessProperties = ConstraintGraphAnalyzer.CheckSoundness(dpnToVerify, cgRefined);

                    timer.Stop();
                }
                else
                {
                    throw new TimeoutException("Process requires more than 15 minutes to verify soundness");
                }

                satisfiesConditions = VerifyConditions(conditionsInfo, dpnToVerify.Transitions.Count, soundnessProperties);
                outputRow = new VerificationOutput(
                    dpnToVerify,
                    verificationType.Value,
                    satisfiesConditions,
                    lts,
                    cgRefined,
                    soundnessProperties,
                    millisecondsToTransform,
                    timer.ElapsedMilliseconds);

                ltsToSave = cgRefined;
            }

            SendResultToPipe(pipeClientHandle, outputRow);

            if (satisfiesConditions)
            {
                SaveResultInFile(verificationType, outputDirectory, outputRow);

                if (saveCG)
                {
                    var constraintGraphToSave = new LtsToVisualize(ltsToSave, soundnessProperties);

                    using (var fs = new FileStream(Path.Combine(outputDirectory, dpnToVerify.Name + "_" + verificationType + ".cgml"), FileMode.Create))
                    {
                        var cgmlParser = new CgmlParser();
                        var xmlDocument = cgmlParser.Serialize(constraintGraphToSave);
                        xmlDocument.Save(fs);
                    }
                }
                return 1;
            }
            return -1;
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

        [Obsolete("Nsqe is not adapted to the new algorithm")]
        private static DataPetriNet GetDpnToVerify(VerificationTypeEnum? verificationType, string dpnFilePath)
        {
            var xDoc = new XmlDocument();
            xDoc.Load(dpnFilePath);

            var parser = new PnmlParser();
            var dpn = parser.DeserializeDpn(xDoc);
            dpn.Context = context;

            return verificationType switch
            {
                VerificationTypeEnum.QeWithoutTransformation or VerificationTypeEnum.NsqeWithoutTransformation =>
                    dpn,
                VerificationTypeEnum.NsqeWithTransformation or VerificationTypeEnum.QeWithTransformation =>
                    new TransformationToAtomicConstraints().Transform(dpn).dpn,
                _ => throw new ArgumentException("Verification type " + nameof(verificationType) + " is not supported!")
            };
        }

        [Obsolete("Nsqe is not adapted to the new algorithm")]
        private static AbstractConstraintExpressionService GetExpressionService(VerificationTypeEnum? verificationType)
        {
            return verificationType switch
            {
                VerificationTypeEnum.QeWithoutTransformation or VerificationTypeEnum.QeWithTransformation =>
                    new ConstraintExpressionOperationServiceWithEqTacticConcat(context),
                VerificationTypeEnum.NsqeWithoutTransformation or VerificationTypeEnum.NsqeWithTransformation =>
                    new ConstraintExpressionOperationServiceWithManualConcat(context),
                _ => throw new ArgumentException("Verification type " + nameof(verificationType) + " is not supported!")
            };
        }

        private static void SaveResultInFile(VerificationTypeEnum? verificationType, string? outputDirectory, VerificationOutput outputRow)
        {
            using (var writer = new StreamWriter(outputDirectory + "/" + verificationType.ToString() + ".csv", true))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.Context.RegisterClassMap<VerificationOutputClassMap>();
                csv.WriteRecord(outputRow);
                csv.NextRecord();
            }
        }

        private static void SendResultToPipe(string pipeClientHandle, VerificationOutput outputRow)
        {
            using (PipeStream pipeClient =
                                new AnonymousPipeClientStream(PipeDirection.Out, pipeClientHandle))
            {
                using (StreamWriter sw = new StreamWriter(pipeClient))
                {
                    sw.AutoFlush = true;
                    XmlSerializer serializer = new XmlSerializer(typeof(VerificationOutput));
                    serializer.Serialize(sw, outputRow);
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
                satisfiesConditions &= soundnessProperties.DeadTransitions.Count < (conditionsInfo.DeadTransitions.Value * transitionsCount / 100);
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
