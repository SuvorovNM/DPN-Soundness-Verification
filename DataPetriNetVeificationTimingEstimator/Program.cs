// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");

using CsvHelper;
using DataPetriNetVeificationTimingEstimator;
using DataPetriNetVeificationTimingEstimator.Enums;
using DataPetriNetVerificationForGivenParameters;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Globalization;

const int RecordsPerConfig = 10;
const int MaxParameterValue = 5;

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

    var props = new VerificationInput()
    {
        PlacesCount = placesCount,
        TransitionsCount = transitionsCount,
        ExtraArcsCount = extraArcsCount,
        VarsCount = variablesCount,
        ConditionsCount = conditionsCount,
        NumberOfRecords = RecordsPerConfig
    };

    JsonSerializer serializer = new JsonSerializer();
    using (StreamWriter sw = new StreamWriter(@"../../../../DataPetriNetVerificationForGivenParameters/Parameters.json"))
    using (JsonWriter writer = new JsonTextWriter(sw))
    {
        serializer.Serialize(writer, props);
    }

    var successInTime = true;
    try
    {
        var processInfo = new ProcessStartInfo();
        processInfo.UseShellExecute = false;
        processInfo.RedirectStandardOutput = true;
        processInfo.RedirectStandardError = true;
        processInfo.FileName = "C:\\Users\\Admin\\source\\repos\\DataPetriNet\\DataPetriNetVerificationForGivenParameters\\bin\\Debug\\net6.0\\DataPetriNetVerificationForGivenParameters.exe";
        processInfo.WorkingDirectory = "C:\\Users\\Admin\\source\\repos\\DataPetriNet\\DataPetriNetVerificationForGivenParameters\\bin\\Debug\\net6.0";
        processInfo.ErrorDialog = true;

        var proc = Process.Start(processInfo);
        successInTime = proc.WaitForExit(720000);
        if (!successInTime)
        {
            proc.Kill();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.ToString());
    }

    if (successInTime)
    {
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
    }
    else
    {
        Console.Write("Failed waiting, trying again!\n");
    }
} while (overallIncreaseCount <= MaxParameterValue);
/* && 
    placesCount <= MaxParameterValue && 
    transitionsCount <= MaxParameterValue &&
    extraArcsCount <= MaxParameterValue &&
    variablesCount <= MaxParameterValue &&
    conditionsCount <= MaxParameterValue*/