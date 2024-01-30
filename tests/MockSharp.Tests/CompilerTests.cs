using Microsoft.CodeAnalysis;
using MockSharp.Core;
using Shouldly;
using Xunit.Abstractions;

namespace MockSharp.Tests;

public class CompilerTests
{
    ITestOutputHelper outputHelper;

    public CompilerTests(ITestOutputHelper outputHelper)
    {
        this.outputHelper = outputHelper;
    }

    [Fact]
    public async Task GetAvailablePlatforms()
    {
        var sdks = await new SdkFactory().GetSdksAsync();

        outputHelper.WriteLine(sdks.FirstOrDefault()?.ToString());

        sdks.ShouldNotBeEmpty();
    }


    [Fact]
    public async Task Compile_Regular()
    {
        var code = """
using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        Console.ReadLine();
    }
}
""";

        var dialogs = await new CsharpCompileService(new AssemblyResolve()).CompileAsync(code, "6.0.26");

        dialogs.FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error).ShouldBe(null);
    }

    [Fact]
    public async Task Compile_Script()
    {
        var code = "Console.WriteLine(Environment.Version);";

        var dialogs = await new CsharpCompileService(new AssemblyResolve()).CompileAsync(code, "8.0.1");

        dialogs.FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error).ShouldBe(null);

    }
}