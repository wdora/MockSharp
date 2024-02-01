using MockSharp.Core;
using Shouldly;

namespace MockSharp.Tests
{
    public class CompletionTests
    {
        [Theory]
        [InlineData("Con", "Console")]
        [InlineData("ne", "new")]
        [InlineData("Console.WriteLine(Da", "DateTime")]
        public async Task GetCompletionList_WithDefaultNs(string code, string want)
        {
            var position = code.Length;

            var list = await new CsharpCompletionService(new AssemblyResolve()).GetCompletionList(code, position);

            list.ShouldContain(x => x.Text == want);
        }
    }
}
