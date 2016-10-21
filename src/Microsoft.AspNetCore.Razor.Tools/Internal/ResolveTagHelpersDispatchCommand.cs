// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Razor.Design;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Internal;
using NuGet.Frameworks;

namespace Microsoft.AspNetCore.Razor.Tools.Internal
{
    public class ResolveTagHelpersDispatchCommand : ResolveTagHelpersCommandBase
    {
        private CommandOption BuildBasePathOption { get; set; }

        private CommandOption FrameworkOption { get; set; }

        private CommandOption ConfigurationOption { get; set; }

        private CommandArgument ProjectArgument { get; set; }

        protected override void Configure(CommandLineApplication config)
        {
            BuildBasePathOption = config.Option(
                "-b|--build-base-path",
                "The project's build base path directory.",
                CommandOptionType.SingleValue);
            FrameworkOption = config.Option(
                "-f|--framework",
                "The project's framework.",
                CommandOptionType.SingleValue);
            ConfigurationOption = config.Option(
                "-c|--configuration",
                "The project's build configuration.",
                CommandOptionType.SingleValue);
            ProjectArgument = config.Argument(
                "[project]",
                "Path to the project.json for the project resolving TagHelperDescriptors.",
                multipleValues: false);
        }

        protected override int OnExecute()
        {
            var projectFullPath = new FileInfo(ProjectArgument.Value).FullName;
            var projectDirectory = Path.GetDirectoryName(projectFullPath);

            NuGetFramework framework;
            if (!TryResolveFramework(projectDirectory, out framework))
            {
                // Could not resolve framework for dispatch. Error was reported, exit early.
                return 0;
            }

            var dispatchArgs = new List<string>
            {
                CommandName,
            };

            dispatchArgs.AddRange(AssemblyNamesArgument.Values);

            if (ProtocolOption.HasValue())
            {
                dispatchArgs.Add("--protocol");
                dispatchArgs.Add(ProtocolOption.Value());
            }

#if DEBUG
            var commandLineArgs = Environment.GetCommandLineArgs();
            if (commandLineArgs.Length > 1 && commandLineArgs[1] == "--debug")
            {
                dispatchArgs.Insert(0, commandLineArgs[1]);
            }
#endif

            var environment = new EnvironmentProvider();
            var packagedCommandSpecFactory = new PackagedCommandSpecFactory();

            var toolName = typeof(Design.Program).GetTypeInfo().Assembly.GetName().Name;
            var dispatchCommand = DotnetToolDispatcher.CreateDispatchCommand(
                dispatchArgs,
                framework,
                ConfigurationOption.Value(),
                outputPath: null,
                buildBasePath: BuildBasePathOption.Value(),
                projectDirectory: projectDirectory,
                toolName: toolName);

            using (var errorWriter = new StringWriter())
            {
                var commandExitCode = dispatchCommand
#if DEBUG
                    .ForwardStdErr(Console.Error)
#else
                    .ForwardStdErr(errorWriter)
#endif
                    .ForwardStdOut()
                    .Execute()
                    .ExitCode;

                if (commandExitCode != 0)
                {
                    ReportError(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ToolResources.FailedToExecuteRazorTooling,
                            errorWriter.ToString()));
                }

                return 0;
            }
        }

        private bool TryResolveFramework(string projectDirectory, out NuGetFramework resolvedFramework)
        {
            string frameworkValue;
            if (FrameworkOption.HasValue())
            {
                frameworkValue = FrameworkOption.Value();
            }
            else
            {
                const string ValueDelimiter = "______FRAMEWORK_______";
                const string ResolveFrameworkTargetName = "ResolveRazorTargetFramework";

                var args = new[] { $"/t:{ResolveFrameworkTargetName}", "/v:m" };
                var command = Command.CreateDotNet("msbuild", args);
                var outputWriter = new StringWriter();

                var exitCode = command
                    .ForwardStdErr(Console.Error)
                    .ForwardStdOut(outputWriter)
                    .WorkingDirectory(projectDirectory)
                    .Execute()
                    .ExitCode;

                if (exitCode != 0)
                {
                    resolvedFramework = null;
                    return false;
                }

                var output = outputWriter.ToString();
                var valueStart = output.IndexOf(ValueDelimiter) + ValueDelimiter.Length;
                var valueEnd = output.LastIndexOf(ValueDelimiter);
                frameworkValue = output.Substring(valueStart, valueEnd - valueStart);
            }

            resolvedFramework = NuGetFramework.Parse(frameworkValue);
            return true;
        }
    }
}
