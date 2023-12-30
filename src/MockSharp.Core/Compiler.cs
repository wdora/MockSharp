using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Collections.Immutable;

namespace MockSharp.Core
{
    public class Compiler
    {
        public async Task<ImmutableArray<Diagnostic>> CompileAsync(string code, string assemblyPath, string targetFrameworkMoniker, CancellationToken cancellationToken = default)
        {
            // OutputKind.ConsoleApplication, Platform.AnyCpu, OptimizationLevel.Release
            var compilationOptions = new CSharpCompilationOptions(OutputKind.ConsoleApplication,
                mainTypeName: null,
                scriptClassName: "Program",
                usings: ["System"],
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

            using var peStream = File.OpenWrite(assemblyPath);

            using var pdbStream = File.OpenWrite(Path.ChangeExtension(assemblyPath, "pdb"));

            var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);

            var references = ResolverAssemblies(targetFrameworkMoniker);

            return CSharpCompilation.Create(assemblyName, [syntaxTree], references, compilationOptions)
                .Emit(peStream: peStream,
                    pdbStream: pdbStream,
                    options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb),
                    cancellationToken: cancellationToken)
                .Diagnostics;
        }

        private IEnumerable<MetadataReference> ResolverAssemblies(string targetFrameworkMoniker)
        {
            var netcoreRoot = @"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref";

            var targetRoot = Path.Combine(netcoreRoot, targetFrameworkMoniker, "ref", $"net{targetFrameworkMoniker.Substring(0, targetFrameworkMoniker.LastIndexOf('.'))}");

            return Directory
                .GetFiles(targetRoot)
                .Where(f => f.EndsWith(".dll"))
                .Select(x => MetadataReference.CreateFromFile(x))
                .ToImmutableList();
        }
    }
}
