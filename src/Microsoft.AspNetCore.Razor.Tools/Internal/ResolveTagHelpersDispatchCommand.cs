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
            var projectFileInfo = new FileInfo(ProjectArgument.Value);

            if (!VerifyProjectIsBuilt(projectFileInfo))
            {
                // Project was not built. Error was reported, exit early.
                return 0;
            }

            NuGetFramework framework;
            if (!TryResolveFramework(projectFileInfo, out framework))
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

            var toolName = typeof(Design.Program).GetTypeInfo().Assembly.GetName().Name;
            var dispatchCommand = DotnetToolDispatcher.CreateDispatchCommand(
                dispatchArgs,
                framework,
                ConfigurationOption.Value(),
                outputPath: null,
                buildBasePath: BuildBasePathOption.Value(),
                projectDirectory: projectFileInfo.DirectoryName,
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

        private bool VerifyProjectIsBuilt(FileInfo projectFileInfo)
        {
            var projectDirectory = projectFileInfo.Directory;
            var objDirectories = projectDirectory.GetDirectories("obj", SearchOption.TopDirectoryOnly);

            if (objDirectories.Length == 0)
            {
                ReportError(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        ToolResources.ProjectMustBeBuiltBeforeExecutingRazorTooling,
                        projectFileInfo.DirectoryName));
                return false;
            }

            return true;
        }

        private bool TryResolveFramework(FileInfo projectFileInfo, out NuGetFramework resolvedFramework)
        {
            string frameworkValue;
            if (FrameworkOption.HasValue())
            {
                frameworkValue = FrameworkOption.Value();
            }
            else
            {
                EnsureToolTargetsAreImported(projectFileInfo);

                const string ValueDelimiter = "______FRAMEWORK_______";
                const string ResolveFrameworkTargetName = "ResolveRazorTargetFramework";

                var thisAssembly = typeof(Program).GetTypeInfo().Assembly;
                var toolVersion = thisAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    ?.InformationalVersion
                    ?? thisAssembly.GetName().Version.ToString();
                var args = new[]
                {
                    $"/t:{ResolveFrameworkTargetName}",
                    $"/p:_RazorToolsVersion={toolVersion}",
                    "/v:m",
                    "/consoleloggerparameters:DisableConsoleColor",
                };
                var command = Command.CreateDotNet("msbuild", args);
                var outputWriter = new StringWriter();

                var exitCode = command
                    .ForwardStdErr(outputWriter)
                    .ForwardStdOut(outputWriter)
                    .WorkingDirectory(projectFileInfo.DirectoryName)
                    .Execute()
                    .ExitCode;

                if (exitCode != 0)
                {
                    ReportError(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ToolResources.CouldNotResolveFramework,
                            outputWriter.ToString()));

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

        private static void EnsureToolTargetsAreImported(FileInfo projectFileInfo)
        {
            const string ToolsImportTargetsName = "Microsoft.AspNetCore.Razor.ToolsImports.targets";

            var toolImportTargetsFileName = $"{projectFileInfo.Name}.{ToolsImportTargetsName}";
            var projectDirectory = projectFileInfo.Directory;
            var objDirectory = projectDirectory.GetDirectories("obj", SearchOption.TopDirectoryOnly)[0];
            var toolImportTargetsFilePath = Path.Combine(objDirectory.FullName, toolImportTargetsFileName);
            if (!File.Exists(toolImportTargetsFilePath))
            {
                var toolType = typeof(Program);
                var toolAssembly = toolType.GetTypeInfo().Assembly;
                var toolNamespace = toolType.Namespace;
                var toolImportTargetsResourceName = $"{toolNamespace}.compiler.resources.{ToolsImportTargetsName}";
                using (var resourceStream = toolAssembly.GetManifestResourceStream(toolImportTargetsResourceName))
                {
                    var targetBytes = new byte[resourceStream.Length];
                    resourceStream.Read(targetBytes, 0, targetBytes.Length);

                    File.WriteAllBytes(toolImportTargetsFilePath, targetBytes);
                }
            }
        }
    }
}
