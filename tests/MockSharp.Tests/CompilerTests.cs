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

        var dialogs = await new Compiler().CompileAsync(code, @"D:/App/Demo.dll", "8.0.0");

        dialogs.ShouldBeEmpty();
    }
}