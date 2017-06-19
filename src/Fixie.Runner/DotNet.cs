namespace Fixie.Runner
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using Cli;

    static class DotNet
    {
        static readonly string DotNetPath;

        static DotNet()
        {
            DotNetPath = FindPath();
        }

        public static int Run(params string[] arguments)
        {
            var dotnet = new ProcessStartInfo
            {
                FileName = DotNetPath,
                Arguments = CommandLine.Serialize(arguments)
            };

            var process = Process.Start(dotnet);
            process.WaitForExit();
            return process.ExitCode;
        }

        static string FindPath()
        {
            var fileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";

            //If `dotnet` is the currently running process, return the full path to that executable.

            var mainModule = Process.GetCurrentProcess().MainModule;

            var currentProcessIsDotNet =
                !string.IsNullOrEmpty(mainModule?.FileName) &&
                Path.GetFileName(mainModule.FileName)
                    .Equals(fileName, StringComparison.OrdinalIgnoreCase);

            if (currentProcessIsDotNet)
                return mainModule.FileName;

            // Find "dotnet" by using the location of the shared framework.

            var fxDepsFile = AppContext.GetData("FX_DEPS_FILE") as string;

            if (string.IsNullOrEmpty(fxDepsFile))
                throw new CommandLineException("Failed to locate `dotnet`, could not look up FX_DEPS_FILE in the AppContext.");

            var dotnetDirectory = new FileInfo(fxDepsFile) // Microsoft.NETCore.App.deps.json
                .Directory? // (version)
                .Parent? // Microsoft.NETCore.App
                .Parent? // shared
                .Parent; // DOTNET_HOME

            if (dotnetDirectory == null)
                throw new CommandLineException("Failed to locate `dotnet`. Could not traverse directories from FX_DEPS_FILE to DOTNET_HOME.");

            var dotnetPath = Path.Combine(dotnetDirectory.FullName, fileName);

            if (!File.Exists(dotnetPath))
                throw new CommandLineException($"Failed to locate `dotnet`. The path does not exist: {dotnetPath}");

            return dotnetPath;
        }
    }
}