// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");

using CsvHelper;
using CsvHelper.Configuration;
using DataPetriNetGeneration;
using DataPetriNetOnSmt.Enums;
using DataPetriNetOnSmt.SoundnessVerification;
using DataPetriNetOnSmt.SoundnessVerification.Services;
using DataPetriNetVeificationTimingEstimator;
using DataPetriNetVeificationTimingEstimator.Enums;
using System.Diagnostics;
using System.Globalization;

const int RecordsPerConfig = 10;
const int MaxParameterValue = 3;

var transitionsCount = 1;
var baseTransitionsCount = 1;
var placesCount = 2;
var basePlacesCount = 2;
var extraArcsCount = 0;
var baseExtraArcsCount = 0;
var variablesCount = 1;
var baseVariablesCount = 1;
var conditionsCount = 0;
var baseConditionsCount = 0;

var parameterToConsider = DpnParameterToConsider.TransitionsCount;
var overallIncreaseCount = 0;
do
{
    var records = new List<VerificationOutput>();
    for (int i = 0; i < RecordsPerConfig; i++)
    {
        var dpnGenerator = new DPNGenerator();
        var dpn = dpnGenerator.Generate(placesCount, transitionsCount, extraArcsCount, variablesCount, conditionsCount);
        var cg = new ConstraintGraph(dpn, new ConstraintExpressionOperationServiceWithEqTacticConcat());
        var timer = new Stopwatch();
        timer.Start();
        cg.GenerateGraph();
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
    }

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
            transitionsCount++;
            if (transitionsCount > MaxParameterValue)
            {
                transitionsCount = baseTransitionsCount;
                parameterToConsider = DpnParameterToConsider.PlacesCount;
                placesCount++;
            }
            break;
        case DpnParameterToConsider.PlacesCount:
            placesCount++;
            if (placesCount > MaxParameterValue)
            {
                placesCount = basePlacesCount;
                parameterToConsider = DpnParameterToConsider.ArcsCount;
                extraArcsCount++;
            }
            break;
        case DpnParameterToConsider.ArcsCount:
            extraArcsCount++;
            if (extraArcsCount > MaxParameterValue)
            {
                extraArcsCount = baseExtraArcsCount;
                parameterToConsider = DpnParameterToConsider.VariablesCount;
                variablesCount++;
            }
            break;
        case DpnParameterToConsider.VariablesCount:
            variablesCount++;
            if (variablesCount > MaxParameterValue)
            {
                variablesCount = baseVariablesCount;
                parameterToConsider = DpnParameterToConsider.ConditionsCount;
                conditionsCount++;
            }
            break;
        case DpnParameterToConsider.ConditionsCount:
            conditionsCount++;
            if (conditionsCount > MaxParameterValue)
            {
                conditionsCount = baseConditionsCount;
                parameterToConsider = DpnParameterToConsider.AllCount;
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
                overallIncreaseCount++;
            }
            break;
        case DpnParameterToConsider.AllCount:
            parameterToConsider = DpnParameterToConsider.TransitionsCount;
            break;
    }
    
} while (overallIncreaseCount <= MaxParameterValue); 
