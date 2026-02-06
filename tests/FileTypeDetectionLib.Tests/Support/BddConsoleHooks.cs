using System;
using Reqnroll;

namespace FileTypeDetectionLib.Tests.Support;

[Binding]
public sealed class BddConsoleHooks
{
    private readonly ScenarioContext _scenarioContext;

    public BddConsoleHooks(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [BeforeScenario(Order = -1000)]
    public void BeforeScenario()
    {
        Console.WriteLine($"[BDD] Szenario startet: {_scenarioContext.ScenarioInfo.Title}");
    }

    [BeforeStep]
    public void BeforeStep()
    {
        var step = _scenarioContext.StepContext.StepInfo;
        Console.WriteLine($"[BDD] {step.StepDefinitionType}: {step.Text}");
    }

    [AfterStep]
    public void AfterStep()
    {
        if (_scenarioContext.TestError is null)
        {
            Console.WriteLine("[BDD] Ergebnis: OK");
            return;
        }

        Console.WriteLine($"[BDD] Ergebnis: FEHLER - {_scenarioContext.TestError.Message}");
    }

    [AfterScenario(Order = 1000)]
    public void AfterScenario()
    {
        var result = _scenarioContext.TestError is null ? "ERFOLGREICH" : "FEHLGESCHLAGEN";
        Console.WriteLine($"[BDD] Szenario Ende: {result}");
    }
}