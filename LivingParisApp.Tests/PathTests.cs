using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;
using LivingParisApp.Services.EnvironmentSetup;

namespace LivingParisApp.Tests;

public class PathTests {
    private readonly ITestOutputHelper _output;

    public PathTests(ITestOutputHelper output) {
        _output = output;
    }

    [Fact]
    public void AppSettings_Should_Exist_In_Config_Directory() {
        string solutionRoot = Constants.GetSolutionDirectoryInfo().FullName;
        string appSettingsPath = Path.Combine(solutionRoot, "config", "appsettings.json");
        Assert.True(File.Exists(appSettingsPath), $"The file {appSettingsPath} does not exist.");
    }
}


