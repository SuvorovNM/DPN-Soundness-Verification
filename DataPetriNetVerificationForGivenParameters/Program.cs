using CsvHelper;
using DataPetriNetVeificationTimingEstimator;
using System.Globalization;
using Newtonsoft.Json;
using DataPetriNetVerificationForGivenParameters;
using DataPetriNetGeneration;
using Microsoft.Z3;
using DataPetriNetOnSmt.SoundnessVerification;
using DataPetriNetOnSmt.SoundnessVerification.Services;
using System.Diagnostics;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt;
using DataPetriNetOnSmt.Abstractions;
using DataPetriNetOnSmt.SoundnessVerification.TransitionSystems;

JsonSerializer serializer = new JsonSerializer();

// Check
VerificationInput props = new VerificationInput();
using (StreamReader sw = new StreamReader(@"../../../Parameters.json"))
using (JsonReader reader = new JsonTextReader(sw))
{
    props = serializer.Deserialize<VerificationInput>(reader);
}

var records = new List<VerificationOutput>();


switch (props.Protocol)
{
    case 1:
        Protocol1();
        WriteAllRecords();
        break;
    case 2:
        Protocol2();
        WriteAllRecords();
        break;
    case 3:
        Protocol3();
        break;
    default:
        throw new ArgumentException($"No protocol with value {props.Protocol}");
}

void WriteAllRecords()
{
    using (var writer = new StreamWriter("results.csv", true))
    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
    {
        foreach (var record in records)
        {
            csv.WriteRecord(record);
            csv.NextRecord();
        }
    }
}

bool VerifyWithTransformation(
    DataPetriNet dpn,
    AbstractConstraintExpressionService expressionService,
    string outputFileName)
{
    var transformationTimer = new Stopwatch();
    transformationTimer.Start();
    var transformedDpn = new TransformationToAtomicConstraints().Transform(dpn).dpn;
    transformationTimer.Stop();
    var cg = new ConstraintGraph(transformedDpn, expressionService);
    var verificationTimer = new Stopwatch();

    verificationTimer.Start();
    try
    {
        cg.GenerateGraph();
    }
    catch (Exception ex)
    {
        Console.Write($"{DateTime.Now.ToLongTimeString()}: Fail! {ex.Message}");
        verificationTimer.Stop();
    }
    var typedStates = ConstraintGraphAnalyzer.GetStatesDividedByTypes(cg, dpn.Places.Where(x => x.IsFinal).ToArray());

    var deadTransitions = dpn.Transitions
                    .Select(x => x.Id)
                    .Except(cg.ConstraintArcs.Where(x => !x.Transition.IsSilent).Select(x => x.Transition.Id))
                    .ToList();

    var isSound = cg.IsFullGraph
        && !typedStates[StateType.NoWayToFinalMarking].Any()
        && !typedStates[StateType.UncleanFinal].Any()
        && !typedStates[StateType.Deadlock].Any()
        && deadTransitions.Count == 0;

    verificationTimer.Stop();

    var outputRow = new VerificationOutput
    {
        PlacesCount = dpn.Places.Count,
        TransitionsCount = dpn.Transitions.Count,
        ArcsCount = dpn.Arcs.Count,
        VarsCount = props.VarsCount,
        ConditionsCount = props.ConditionsCount,
        Boundedness = cg.IsFullGraph,
        ConstraintStates = cg.ConstraintStates.Count,
        ConstraintArcs = cg.ConstraintArcs.Count,
        DeadTransitions = deadTransitions.Count,
        Deadlocks = typedStates[StateType.Deadlock].Count > 0,
        Soundness = isSound,
        Milliseconds = verificationTimer.ElapsedMilliseconds
    };

    Console.Write($"{DateTime.Now.ToLongTimeString()}: Success with transformation! Time: {verificationTimer.ElapsedMilliseconds} ");

    using (var writer = new StreamWriter(outputFileName, true))
    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
    {
        csv.WriteRecord(outputRow);
        csv.NextRecord();
    }

    outputRow.Milliseconds += transformationTimer.ElapsedMilliseconds;

    using (var writer = new StreamWriter(outputFileName.Substring(0, outputFileName.Length - 4) +"_withTransformationTime.csv", true))
    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
    {
        csv.WriteRecord(outputRow);
        csv.NextRecord();
    }

    return true;//dpnSatisifiesConditions
}

bool VerifyWithoutTransformation(
    DataPetriNet dpn,
    AbstractConstraintExpressionService expressionService,
    string outputFileName)
{
    var cg = new ConstraintGraph(dpn, expressionService);
    var timer = new Stopwatch();
    timer.Start();
    try
    {
        cg.GenerateGraph();
    }
    catch (Exception ex)
    {
        Console.Write($"{DateTime.Now.ToLongTimeString()}: Fail! {ex.Message}");
        timer.Stop();
    }
    var typedStates = ConstraintGraphAnalyzer.GetStatesDividedByTypes(cg, dpn.Places.Where(x => x.IsFinal).ToArray());

    var deadTransitions = dpn.Transitions
                    .Select(x => x.Id)
                    .Except(cg.ConstraintArcs.Where(x => !x.Transition.IsSilent).Select(x => x.Transition.Id))
                    .ToList();

    var isSound = cg.IsFullGraph
        && !typedStates[StateType.NoWayToFinalMarking].Any()
        && !typedStates[StateType.UncleanFinal].Any()
        && !typedStates[StateType.Deadlock].Any()
        && deadTransitions.Count == 0;

    timer.Stop();

    var dpnSatisifiesConditions = deadTransitions.Count < 0.5 * props.TransitionsCount
        && cg.IsFullGraph;
        //&& cg.ConstraintArcs.Count < 2000;

    if (dpnSatisifiesConditions)
    {
        var outputRow = new VerificationOutput
        {
            PlacesCount = props.PlacesCount,
            TransitionsCount = props.TransitionsCount,
            ArcsCount = dpn.Arcs.Count,
            VarsCount = props.VarsCount,
            ConditionsCount = props.ConditionsCount,
            Boundedness = cg.IsFullGraph,
            ConstraintStates = cg.ConstraintStates.Count,
            ConstraintArcs = cg.ConstraintArcs.Count,
            DeadTransitions = deadTransitions.Count,
            Deadlocks = typedStates[StateType.Deadlock].Count > 0,
            Soundness = isSound,
            Milliseconds = timer.ElapsedMilliseconds
        };

        records.Add(outputRow);
        Console.Write($"{DateTime.Now.ToLongTimeString()}: Success with no transformation! Time: {timer.ElapsedMilliseconds} ");

        using (var writer = new StreamWriter(outputFileName, true))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.WriteRecord(outputRow);
            csv.NextRecord();
        }
    }

    return dpnSatisifiesConditions;
}

void Protocol3()
{
    var dpnSatisifiesConditions = false;
    do
    {
        var dpnGenerator = new DPNGenerator(new Context());
        var dpn = dpnGenerator.Generate(props.PlacesCount, props.TransitionsCount, props.ExtraArcsCount, props.VarsCount, props.ConditionsCount);

        var QeExpressionService = new ConstraintExpressionOperationServiceWithEqTacticConcat(dpn.Context);

        dpnSatisifiesConditions = VerifyWithoutTransformation(dpn, QeExpressionService, "QeWithoutTransformation.csv");

        if (dpnSatisifiesConditions)
        {
            // Refactor!
            //var manualExpressionService = new ConstraintExpressionServiceForRealsWithManualConcat(dpn.Context);
            //VerifyWithoutTransformation(dpn, manualExpressionService, "nSQEWithoutTransformation.csv");

            VerifyWithTransformation(dpn, QeExpressionService, "QeWithTransformation.csv");
            //VerifyWithTransformation(dpn, manualExpressionService, "nSQEWithTransformation.csv");
        }
        dpnGenerator.Dispose();

    } while (!dpnSatisifiesConditions);
}

void Protocol2()
{

    var dpnSatisifiesConditions = false;
    do
    {
        var dpnGenerator = new DPNGenerator(new Context());
        var dpn = dpnGenerator.Generate(props.PlacesCount, props.TransitionsCount, props.ExtraArcsCount, props.VarsCount, props.ConditionsCount);
        var cg = new ConstraintGraph(dpn, props.VerificationType == 1 ? new ConstraintExpressionOperationServiceWithManualConcat(dpn.Context) : new ConstraintExpressionOperationServiceWithManualConcat(dpn.Context));
        var timer = new Stopwatch();
        timer.Start();
        try
        {
            cg.GenerateGraph();
        }
        catch (Exception ex)
        {
            Console.Write($"{DateTime.Now.ToLongTimeString()}: Fail! ");
            timer.Stop();
            continue;
        }
        var typedStates = ConstraintGraphAnalyzer.GetStatesDividedByTypes(cg, dpn.Places.Where(x => x.IsFinal).ToArray());

        var deadTransitions = dpn.Transitions
                        .Select(x => x.Id)
                        .Except(cg.ConstraintArcs.Where(x => !x.Transition.IsSilent).Select(x => x.Transition.Id))
                        .ToList();

        var isSound = !typedStates[StateType.NoWayToFinalMarking].Any()
            && !typedStates[StateType.UncleanFinal].Any()
            && !typedStates[StateType.Deadlock].Any()
            && deadTransitions.Count == 0;

        timer.Stop();

        dpnSatisifiesConditions = deadTransitions.Count < 0.6 * props.TransitionsCount;

        if (dpnSatisifiesConditions)
        {
            var outputRow = new VerificationOutput
            {
                PlacesCount = props.PlacesCount,
                TransitionsCount = props.TransitionsCount,
                ArcsCount = dpn.Arcs.Count,
                VarsCount = props.VarsCount,
                ConditionsCount = props.ConditionsCount,
                Boundedness = cg.IsFullGraph,
                ConstraintStates = cg.ConstraintStates.Count,
                ConstraintArcs = cg.ConstraintArcs.Count,
                DeadTransitions = deadTransitions.Count,
                Deadlocks = typedStates[StateType.Deadlock].Count > 0,
                Soundness = isSound,
                Milliseconds = timer.ElapsedMilliseconds
            };

            records.Add(outputRow);
            Console.Write($"{DateTime.Now.ToLongTimeString()}: Success! ");
        }
        dpnGenerator.Dispose();

    } while (!dpnSatisifiesConditions);
}

void Protocol1()
{
    for (int i = 0; i < props.NumberOfRecords; i++)
    {
        var dpnGenerator = new DPNGenerator(new Context());
        var dpn = dpnGenerator.Generate(props.PlacesCount, props.TransitionsCount, props.ExtraArcsCount, props.VarsCount, props.ConditionsCount);
        var cg = new ConstraintGraph(dpn, props.VerificationType == 1 ? new ConstraintExpressionOperationServiceWithEqTacticConcat(dpn.Context) : new ConstraintExpressionOperationServiceWithManualConcat(dpn.Context));
        var timer = new Stopwatch();
        timer.Start();
        try
        {
            cg.GenerateGraph();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{i}: " + ex.ToString());
            timer.Stop();
            continue;
        }
        var typedStates = ConstraintGraphAnalyzer.GetStatesDividedByTypes(cg, dpn.Places.Where(x => x.IsFinal).ToArray());

        var deadTransitions = dpn.Transitions
                        .Select(x => x.Id)
                        .Except(cg.ConstraintArcs.Where(x => !x.Transition.IsSilent).Select(x => x.Transition.Id))
                        .ToList();

        var isSound = !typedStates[StateType.NoWayToFinalMarking].Any()
            && !typedStates[StateType.UncleanFinal].Any()
            && !typedStates[StateType.Deadlock].Any()
            && deadTransitions.Count == 0;

        timer.Stop();

        var outputRow = new VerificationOutput
        {
            PlacesCount = props.PlacesCount,
            TransitionsCount = props.TransitionsCount,
            ArcsCount = dpn.Arcs.Count,
            VarsCount = props.VarsCount,
            ConditionsCount = props.ConditionsCount,
            Boundedness = cg.IsFullGraph,
            ConstraintStates = cg.ConstraintStates.Count,
            ConstraintArcs = cg.ConstraintArcs.Count,
            DeadTransitions = deadTransitions.Count,
            Deadlocks = typedStates[StateType.Deadlock].Count > 0,
            Soundness = isSound,
            Milliseconds = timer.ElapsedMilliseconds
        };

        records.Add(outputRow);
        Console.Write($"{i}... ");
        dpnGenerator.Dispose();
    }
}