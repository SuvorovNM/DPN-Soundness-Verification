// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");

using CsvHelper;
using CsvHelper.Configuration;
using DataPetriNetGeneration;
using DataPetriNetOnSmt;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.SoundnessVerification;
using DataPetriNetOnSmt.SoundnessVerification.Services;
using DataPetriNetVeificationTimingEstimator;
using DataPetriNetVeificationTimingEstimator.Enums;
using Microsoft.Z3;
using System.Diagnostics;
using System.Globalization;

const int RecordsPerConfig = 10;
const int MaxParameterValue = 20;

using (var writer = new StreamWriter("results.csv", false))
using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
{
    csv.WriteHeader<VerificationOutput>();
    csv.NextRecord();
}

var transitionsCount = 3;
var baseTransitionsCount = 3;
var placesCount = 4;
var basePlacesCount = 4;
var extraArcsCount = 2;
var baseExtraArcsCount = 2;
var variablesCount = 3;
var baseVariablesCount = 3;
var conditionsCount = 1;
var baseConditionsCount = 1;

var parameterToConsider = DpnParameterToConsider.ConditionsCount;
var overallIncreaseCount = 0;

do
{
    Console.WriteLine($"Starting to execute at PlacesCount={placesCount},TransitionsCount={transitionsCount},ArcsCount={extraArcsCount},VarsCount={variablesCount},ConditionsCount={conditionsCount}");
    var records = new List<VerificationOutput>();
    for (int i = 0; i < RecordsPerConfig; i++)
    {
        var dpnGenerator = new DPNGenerator(new Context());
        var dpn = dpnGenerator.Generate(placesCount, transitionsCount, extraArcsCount, variablesCount, conditionsCount);
        var cg = new ConstraintGraph(dpn, new ConstraintExpressionOperationServiceWithEqTacticConcat(dpn.Context));
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
            PlacesCount = placesCount,
            TransitionsCount = transitionsCount,
            ArcsCount = dpn.Arcs.Count,
            VarsCount = variablesCount,
            ConditionsCount = conditionsCount,
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
    //GC.Collect();
    using (var writer = new StreamWriter("results.csv", true))
    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
    {
        foreach (var record in records)
        {
            csv.WriteRecord(record);
            csv.NextRecord();
        }
    }

    switch (parameterToConsider)
    {
        case DpnParameterToConsider.TransitionsCount:
            if (transitionsCount >= MaxParameterValue)
            {
                transitionsCount = baseTransitionsCount;
                parameterToConsider = DpnParameterToConsider.PlacesCount;
                placesCount++;
            }
            else
            {
                transitionsCount++;
            }
            break;
        case DpnParameterToConsider.PlacesCount:
            if (placesCount >= MaxParameterValue)
            {
                placesCount = basePlacesCount;
                parameterToConsider = DpnParameterToConsider.ArcsCount;
                extraArcsCount++;
            }
            else
            {
                placesCount++;
            }
            break;
        case DpnParameterToConsider.ArcsCount:
            if (extraArcsCount >= MaxParameterValue)
            {
                extraArcsCount = baseExtraArcsCount;
                parameterToConsider = DpnParameterToConsider.VariablesCount;
                variablesCount++;
            }
            else
            {
                extraArcsCount++;
            }
            break;
        case DpnParameterToConsider.VariablesCount:
            if (variablesCount >= MaxParameterValue)
            {
                variablesCount = baseVariablesCount;
                parameterToConsider = DpnParameterToConsider.ConditionsCount;
                conditionsCount++;
            }
            else
            {
                variablesCount++;
            }
            break;
        case DpnParameterToConsider.ConditionsCount:
            if (conditionsCount >= MaxParameterValue)
            {
                conditionsCount = baseConditionsCount;
                parameterToConsider = DpnParameterToConsider.AllCount;
                overallIncreaseCount++;
            }
            else
            {
                conditionsCount++;
            }
            break;
        case DpnParameterToConsider.AllCount:
            parameterToConsider = DpnParameterToConsider.TransitionsCount;
            baseTransitionsCount++;
            basePlacesCount++;
            baseExtraArcsCount++;
            baseVariablesCount++;
            baseConditionsCount++;
            transitionsCount = baseTransitionsCount;
            placesCount = basePlacesCount;
            extraArcsCount = baseExtraArcsCount;
            variablesCount = baseVariablesCount;
            conditionsCount = baseConditionsCount;
            break;
    }
    Console.Write("Success!\n");
} while (overallIncreaseCount <= MaxParameterValue);
/* && 
    placesCount <= MaxParameterValue && 
    transitionsCount <= MaxParameterValue &&
    extraArcsCount <= MaxParameterValue &&
    variablesCount <= MaxParameterValue &&
    conditionsCount <= MaxParameterValue*/