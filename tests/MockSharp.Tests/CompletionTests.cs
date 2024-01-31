using MockSharp.Core;
using Shouldly;

namespace MockSharp.Tests
{
    public class CompletionTests
    {
        [Fact]
        public async Task GetCompletionList()
        {
            var code = "System.Con";

            var position = code.Length;

            var list = await new CsharpCompletionService(new AssemblyResolve()).GetCompletionList(code, position);

            var want = "Console";

            list.ShouldContain(x => x.Text == want);
        }

        [Fact]
        public async Task GetCompletionList_WithDefaultNs()
        {
            var code = "Con";

            var position = code.Length;

            var list = await new CsharpCompletionService(new AssemblyResolve()).GetCompletionList(code, position);

            var want = "Console";

            list.ShouldContain(x => x.Text == want);
        }
    }
}
