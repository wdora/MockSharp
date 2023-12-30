using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Collections.Immutable;

namespace MockSharp.Core
{
    public class Compiler
    {
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

        public async Task<ImmutableArray<Diagnostic>> CompileAsync(string code, string assemblyPath, string targetFrameworkMoniker, CancellationToken cancellationToken = default)
        {
            // OutputKind.ConsoleApplication, Platform.AnyCpu, OptimizationLevel.Release
            var compilationOptions = new CSharpCompilationOptions(OutputKind.ConsoleApplication,
                mainTypeName: null,
                scriptClassName: "Program",
                usings: [],
                optimizationLevel: OptimizationLevel.Release,
                checkOverflow: false,
                allowUnsafe: true,
                platform: Platform.AnyCpu,
                warningLevel: 4,
                deterministic: true,
                xmlReferenceResolver: null,
                sourceReferenceResolver: SourceFileResolver.Default,
                assemblyIdentityComparer: AssemblyIdentityComparer.Default,
                nullableContextOptions: NullableContextOptions.Enable
            );

            var syntaxTree = SyntaxFactory.ParseSyntaxTree(code);

            var importsSyntax = SyntaxFactory.ParseSyntaxTree(GlobalUsing);

            using var peStream = File.OpenWrite(assemblyPath);

            using var pdbStream = File.OpenWrite(Path.ChangeExtension(assemblyPath, "pdb"));

            await File.WriteAllTextAsync(Path.ChangeExtension(assemblyPath, "runtimeconfig.json"), """
{
  "runtimeOptions": {
    "framework": {
      "name": "Microsoft.NETCore.App",
      "version": "{version}"
    }
  }
}
""".Replace("{version}", targetFrameworkMoniker));

            var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);

            var references = ResolverAssemblies(targetFrameworkMoniker);

            return CSharpCompilation.Create(assemblyName, [syntaxTree, importsSyntax], references, compilationOptions)
                .Emit(peStream: peStream,
                    pdbStream: pdbStream,
                    options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb),
                    cancellationToken: cancellationToken)
                .Diagnostics;
        }

        private IEnumerable<MetadataReference> ResolverAssemblies(string targetFrameworkMoniker)
        {
            //var netcoreRoot = @"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref";

            var root = Path.Combine(Environment.GetEnvironmentVariable("ProgramW6432")!, "dotnet", "packs", "Microsoft.NETCore.App.Ref");

            var targetRoot = Path.Combine(root, targetFrameworkMoniker, "ref", $"net{targetFrameworkMoniker.Substring(0, targetFrameworkMoniker.LastIndexOf('.'))}");

            return Directory
                .GetFiles(targetRoot)
                .Where(f => f.EndsWith(".dll"))
                .Select(x => MetadataReference.CreateFromFile(x))
                .ToImmutableList();
        }
    }
}
