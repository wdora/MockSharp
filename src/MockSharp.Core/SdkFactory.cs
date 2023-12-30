using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MockSharp.Core
{
    public class SdkFactory
    {
        public async Task<ImmutableArray<SdkInfo>> GetSdksAsync()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new NotImplementedException();

            var root = Path.Combine(Environment.GetEnvironmentVariable("ProgramW6432")!, "dotnet", "sdk");

            return Directory
                .EnumerateDirectories(root)
                .Select(x => Version.TryParse(Path.GetFileName(x), out var version) ? new SdkInfo(x, version) : default!)
                .Where(x => x != default)
                .OrderByDescending(x => x.Version)
                .ToImmutableArray();
        }
    }

    public record SdkInfo(string Path, Version Version);
}
