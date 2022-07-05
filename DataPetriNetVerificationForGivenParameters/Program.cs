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

JsonSerializer serializer = new JsonSerializer();

// Check
VerificationInput props = new VerificationInput();
using (StreamReader sw = new StreamReader(@"../../../Parameters.json"))
using (JsonReader reader = new JsonTextReader(sw))
{
    props = serializer.Deserialize<VerificationInput>(reader);
}

var records = new List<VerificationOutput>();
for (int i = 0; i < props.NumberOfRecords; i++)
{
    var dpnGenerator = new DPNGenerator(new Context());
    var dpn = dpnGenerator.Generate(props.PlacesCount, props.TransitionsCount, props.ExtraArcsCount, props.VarsCount, props.ConditionsCount);
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