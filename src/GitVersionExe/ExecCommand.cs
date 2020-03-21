using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using GitVersion.Exceptions;
using GitVersion.Extensions;
using GitVersion.Extensions.VersionAssemblyInfoResources;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.Model;
using GitVersion.OutputVariables;
using Microsoft.Extensions.Options;

namespace GitVersion
{
    public class ExecCommand : IExecCommand
    {
        private static readonly bool RunningOnUnix = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        private readonly IFileSystem fileSystem;
        private readonly IBuildServerResolver buildServerResolver;
        private readonly ILog log;
        private readonly IOptions<Arguments> options;

        public ExecCommand(IFileSystem fileSystem, IBuildServerResolver buildServerResolver, ILog log, IOptions<Arguments> options)
        {
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            this.buildServerResolver = buildServerResolver ?? throw new ArgumentNullException(nameof(buildServerResolver));
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public void Execute(VersionVariables variables)
        {
            log.Info($"Running on {(RunningOnUnix ? "Unix" : "Windows")}.");

            var arguments = options.Value;

            if (arguments.Output.Contains(OutputType.BuildServer))
            {
                var buildServer = buildServerResolver.Resolve();
                buildServer?.WriteIntegration(Console.WriteLine, variables);
            }
            if (arguments.Output.Contains(OutputType.Json))
            {
                switch (arguments.ShowVariable)
                {
                    case null:
                        Console.WriteLine(variables.ToString());
                        break;

                    default:
                        if (!variables.TryGetValue(arguments.ShowVariable, out var part))
                        {
                            throw new WarningException($"'{arguments.ShowVariable}' variable does not exist");
                        }

                        Console.WriteLine(part);
                        break;
                }
            }

            if (arguments.UpdateWixVersionFile)
            {
                using var wixVersionFileUpdater = new WixVersionFileUpdater(arguments.TargetPath, variables, fileSystem, log);
                wixVersionFileUpdater.Update();
            }

            using var assemblyInfoUpdater = new AssemblyInfoFileUpdater(arguments.UpdateAssemblyInfoFileName, arguments.TargetPath, variables, fileSystem, log, arguments.EnsureAssemblyInfo);
            if (arguments.UpdateAssemblyInfo)
            {
                assemblyInfoUpdater.Update();
                assemblyInfoUpdater.CommitChanges();
            }

            RunExecCommandIfNeeded(arguments, arguments.TargetPath, variables, log);
            RunMsBuildIfNeeded(arguments, arguments.TargetPath, variables, log);
        }

        private static bool RunMsBuildIfNeeded(Arguments args, string workingDirectory, VersionVariables variables, ILog log)
        {
#pragma warning disable CS0612 // Type or member is obsolete
            if (string.IsNullOrEmpty(args.Proj)) return false;

            args.Exec = "dotnet";
            args.ExecArgs = $"msbuild \"{args.Proj}\" {args.ProjArgs}";
#pragma warning restore CS0612 // Type or member is obsolete

            return RunExecCommandIfNeeded(args, workingDirectory, variables, log);
        }

        private static bool RunExecCommandIfNeeded(Arguments args, string workingDirectory, VersionVariables variables, ILog log)
        {
#pragma warning disable CS0612 // Type or member is obsolete
            if (string.IsNullOrEmpty(args.Exec)) return false;

            log.Info($"Launching {args.Exec} {args.ExecArgs}");
            var results = ProcessHelper.Run(
                m => log.Info(m), m => log.Error(m),
                null, args.Exec, args.ExecArgs, workingDirectory,
                GetEnvironmentalVariables(variables));

            if (results != 0)
                throw new WarningException($"Execution of {args.Exec} failed, non-zero return code");
#pragma warning restore CS0612 // Type or member is obsolete

            return true;
        }

        private static KeyValuePair<string, string>[] GetEnvironmentalVariables(VersionVariables variables)
        {
            return variables
                .Select(v => new KeyValuePair<string, string>("GitVersion_" + v.Key, v.Value))
                .ToArray();
        }
    }
}
