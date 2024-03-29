﻿// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");

using CsvHelper;
using DataPetriNetVeificationTimingEstimator;
using DataPetriNetVeificationTimingEstimator.Enums;
using DataPetriNetVerificationForGivenParameters;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Globalization;

const int RecordsPerConfig = 1;
const int MaxParameterValue = 250;
const int IncrementAmount = 5;

var transitionsCount = 75;
var baseTransitionsCount = 75;
var placesCount = 60;
var basePlacesCount = 60;
var extraArcsCount = 37;
var baseExtraArcsCount = 37;
var variablesCount = 37;
var baseVariablesCount = 37;
var conditionsCount = 75;
var baseConditionsCount = 75;

var parameterToConsider = DpnParameterToConsider.TransitionsCount;
var overallIncreaseCount = 5;
var counter = 0;

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
        Protocol = 3,
        VerificationType = (int)VerificationType.OwnImplementation,
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
        proc.WaitForExit();
        /*successInTime = proc.WaitForExit(3500000);
        if (!successInTime)
        {
            proc.Kill();
        }*/
        //proc.WaitForExit();
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.ToString());
    }

    if (successInTime)
    {
        Protocol2();
    }
    else
    {
        Console.Write("Failed waiting, trying again!\n");
    }
} while (overallIncreaseCount <= MaxParameterValue);

void Protocol2()
{
    if (counter < 2)
    {
        counter++;
    }
    else
    {
        transitionsCount += IncrementAmount; 
        placesCount = (int)(transitionsCount * 0.8);
        extraArcsCount = (int)(transitionsCount * 0.5);
        variablesCount = (int)(transitionsCount * 0.5);
        conditionsCount = transitionsCount;
        overallIncreaseCount = transitionsCount;
        counter = 0;
    }
}

void Protocol1()
{
    switch (parameterToConsider)
    {
        case DpnParameterToConsider.TransitionsCount:
            if (transitionsCount >= MaxParameterValue)
            {
                transitionsCount = baseTransitionsCount;
                parameterToConsider = DpnParameterToConsider.PlacesCount;
                placesCount += IncrementAmount;
            }
            else
            {
                transitionsCount += IncrementAmount;
            }
            break;
        case DpnParameterToConsider.PlacesCount:
            if (placesCount >= MaxParameterValue)
            {
                placesCount = basePlacesCount;
                parameterToConsider = DpnParameterToConsider.ArcsCount;
                extraArcsCount += IncrementAmount;
            }
            else
            {
                placesCount += IncrementAmount;
            }
            break;
        case DpnParameterToConsider.ArcsCount:
            if (extraArcsCount >= MaxParameterValue)
            {
                extraArcsCount = baseExtraArcsCount;
                parameterToConsider = DpnParameterToConsider.VariablesCount;
                variablesCount += IncrementAmount;
            }
            else
            {
                extraArcsCount += IncrementAmount;
            }
            break;
        case DpnParameterToConsider.VariablesCount:
            if (variablesCount >= MaxParameterValue)
            {
                variablesCount = baseVariablesCount;
                parameterToConsider = DpnParameterToConsider.ConditionsCount;
                conditionsCount += IncrementAmount;
            }
            else
            {
                variablesCount += IncrementAmount;
            }
            break;
        case DpnParameterToConsider.ConditionsCount:
            if (conditionsCount >= 145)
            {
                conditionsCount = baseConditionsCount;
                parameterToConsider = DpnParameterToConsider.AllCount;
                overallIncreaseCount += IncrementAmount;
            }
            else
            {
                conditionsCount += IncrementAmount;
            }
            break;
        case DpnParameterToConsider.AllCount:
            parameterToConsider = DpnParameterToConsider.TransitionsCount;
            baseTransitionsCount += IncrementAmount;
            basePlacesCount += IncrementAmount;
            baseExtraArcsCount += IncrementAmount;
            baseVariablesCount += IncrementAmount;
            baseConditionsCount += IncrementAmount;
            transitionsCount = baseTransitionsCount;
            placesCount = basePlacesCount;
            extraArcsCount = baseExtraArcsCount;
            variablesCount = baseVariablesCount;
            conditionsCount = baseConditionsCount;
            break;
    }
    Console.Write($"{DateTime.Now.ToLongTimeString()}: Success!\n");
}