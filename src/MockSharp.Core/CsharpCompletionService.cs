using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;

namespace MockSharp.Core
{
    public class CsharpCompletionService
    {
        IAssemblyResolve assemblyResolve;

        public CsharpCompletionService(IAssemblyResolve assemblyResolve)
        {
            this.assemblyResolve = assemblyResolve;
        }

        public async Task<IEnumerable<CsharpCompletionItem>> GetCompletionList(string code, int position, string targetFramework = "8.0.1")
        {
            var workspace = new AdhocWorkspace();

            var referenceDlls = assemblyResolve.GetReferences(targetFramework).Select(x => MetadataReference.CreateFromFile(x));

            string projName = "projName";

            string assemblyName = "assemblyName";

            var projInfo = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Default, projName, assemblyName, LanguageNames.CSharp, metadataReferences: referenceDlls);

            var proj = workspace.AddProject(projInfo);

            proj = proj.AddDocument("autoglobalusing", CsharpCompileService.GlobalUsing).Project;

            var text = SourceText.From(code);

            var document = proj.AddDocument("documentName", text);

            var completionService = CompletionService.GetService(document)!;

            var completionList = await completionService.GetCompletionsAsync(document, position);

            var filterText = text.GetSubText(completionList.Span).ToString();

            return completionList.ItemsList
                .Where(item => completionService.FilterItems(document, [item], filterText).Any())
                .Select(x => new CsharpCompletionItem(x.DisplayText, x.Tags.First()));
        }
    }

    public interface IAssemblyResolve
    {
        IEnumerable<string> GetReferences(string targetFramework);
    }

    public class AssemblyResolve : IAssemblyResolve
    {
        public IEnumerable<string> GetReferences(string targetFrameworkVersion)
        {
            //var sdkPath = @"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref";

            var sdkPath = Path.Combine(Environment.GetEnvironmentVariable("ProgramW6432")!, "dotnet", "packs", "Microsoft.NETCore.App.Ref");

            var moniker = targetFrameworkVersion.Substring(0, targetFrameworkVersion.LastIndexOf('.'));

            var targetRoot = Path.Combine(sdkPath, targetFrameworkVersion, "ref", $"net{moniker}");

            //var targetRoot = @"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\8.0.1\ref\net8.0";

            return Directory
                .GetFiles(targetRoot)
                .Where(f => f.EndsWith(".dll"));
        }
    }

    public record CsharpCompletionItem(string Text, string Type);
}
