using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Collections.Immutable;

namespace MockSharp.Core
{
    public class CsharpCompileService
    {
        IAssemblyResolve assemblyResolve;

        public CsharpCompileService(IAssemblyResolve assemblyResolve)
        {
            this.assemblyResolve = assemblyResolve;
        }

        static string[] NamespaceDefault { get; } = {
            "System",
            "System.Threading",
            "System.Threading.Tasks",
            "System.Collections",
            "System.Collections.Generic",
            "System.Text",
            "System.Text.RegularExpressions",
            "System.Linq",
            "System.IO",
            "System.Reflection",
        };

        static string GlobalUsing = string.Join("\r\n", NamespaceDefault.Select(x => $"global using {x};"));

        static string assemblyAttr = """
using System.Reflection;
using System.Runtime.Versioning;

[assembly: AssemblyTitle("MockApp")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: TargetFramework(".NETCoreApp,Version=v6.0")]
""";

        static string runtimeConfig = """
{
  "runtimeOptions": {
    "framework": {
      "name": "Microsoft.NETCore.App",
      "version": "6.0.0"
    }
  }
}
""";

        public async Task<ImmutableArray<Diagnostic>> CompileAsync(string code, string targetFrameworkVersion, CancellationToken cancellationToken = default)
        {
            // OutputKind.ConsoleApplication, Platform.AnyCpu, OptimizationLevel.Release
            var compilationOptions = default(CSharpCompilationOptions);
            //    new CSharpCompilationOptions(OutputKind.ConsoleApplication,
            //    mainTypeName: null,
            //    scriptClassName: "Program",
            //    usings: [],
            //    optimizationLevel: OptimizationLevel.Debug,
            //    checkOverflow: false,
            //    allowUnsafe: true,
            //    platform: Platform.AnyCpu,
            //    warningLevel: 4,
            //    deterministic: true,
            //    xmlReferenceResolver: null,
            //    sourceReferenceResolver: SourceFileResolver.Default,
            //    assemblyIdentityComparer: AssemblyIdentityComparer.Default,
            //    nullableContextOptions: NullableContextOptions.Enable
            //);

            var moniker = targetFrameworkVersion.Substring(0, targetFrameworkVersion.LastIndexOf('.'));

            var syntaxTrees = GetSyntaxTrees(code, moniker);

            var assemblyPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".dll");

#if DEBUG
            assemblyPath = "D:\\Code\\GitHub\\Demo\\Demo\\ConsoleApp6\\bin\\Debug\\net6.0\\a.dll";
#endif
            var pdbPath = Path.ChangeExtension(assemblyPath, "pdb");

            using var peStream = File.OpenWrite(assemblyPath);

            using var pdbStream = File.OpenWrite(pdbPath);

            var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);

            var references = assemblyResolve
                .GetReferences(targetFrameworkVersion)
                .Select(x => MetadataReference.CreateFromFile(x))
                .ToImmutableList();

            var compliation = CSharpCompilation.Create(assemblyName, syntaxTrees, references, compilationOptions)
                .Emit(peStream: peStream,
                    pdbStream: pdbStream,
                    options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb),
                    cancellationToken: cancellationToken);

            var runtimeConfigPath = Path.ChangeExtension(assemblyPath, "runtimeconfig.json");

            await File.WriteAllTextAsync(runtimeConfigPath, runtimeConfig.Replace("6.0.0", targetFrameworkVersion));

            return compliation.Diagnostics;
        }

        private IEnumerable<SyntaxTree> GetSyntaxTrees(string code, string targetFrameworkMoniker)
        {
            yield return SyntaxFactory.ParseSyntaxTree(code);

            yield return SyntaxFactory.ParseSyntaxTree(GlobalUsing);

            yield return SyntaxFactory.ParseSyntaxTree(assemblyAttr.Replace("6.0", targetFrameworkMoniker));
        }

        private IEnumerable<MetadataReference> ResolverAssemblies(string targetFramework, string moniker)
        {
            //var sdkPath = @"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref";

            var sdkPath = Path.Combine(Environment.GetEnvironmentVariable("ProgramW6432")!, "dotnet", "packs", "Microsoft.NETCore.App.Ref");

            var targetRoot = Path.Combine(sdkPath, targetFramework, "ref", $"net{moniker}");

            return Directory
                .GetFiles(targetRoot)
                .Where(f => f.EndsWith(".dll"))
                .Select(x => MetadataReference.CreateFromFile(x))
                .ToImmutableList();
        }
    }
}
