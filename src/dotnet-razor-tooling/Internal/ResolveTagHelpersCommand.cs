// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using dotnet_razor_tooling;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ProjectModel;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json;
using NuGet.Frameworks;

namespace Microsoft.AspNetCore.Tooling.Razor.Internal
{
    public static class ResolveTagHelpersCommand
    {
        private const string CommandName = "resolve-taghelpers";

        internal static void Register(CommandLineApplication app, params string[] programArgs)
        {
            app.Command(CommandName, config =>
            {
                config.Description = "Resolves TagHelperDescriptors in the specified assembly(s).";
                config.HelpOption("-?|-h|--help");
                var protocolOption = config.Option(
                    "-p|--protocol",
                    "Protocol to resolve TagHelperDescriptors with.",
                    CommandOptionType.SingleValue);
                var buildBasePathOption = config.Option(
                    "-b|--build-base-path",
                    "The project's build base path directory.",
                    CommandOptionType.SingleValue);
                var frameworkOption = config.Option(
                    "-f|--framework",
                    "The project's framework.",
                    CommandOptionType.SingleValue);
                var configurationOption = config.Option(
                    "-c|--configuration",
                    "The project's build configuration.",
                    CommandOptionType.SingleValue);
                CommandArgument projectArgument = null;

                if (DotnetToolDispatcher.IsDispatcher(programArgs))
                {
                    projectArgument = config.Argument(
                        "[project]",
                        "Path to the project.json for the project resolving TagHelperDescriptors.",
                        multipleValues: false);
                }
                else
                {
                    DotnetToolDispatcher.EnsureValidDispatchRecipient(config, programArgs);
                }

                var assemblyNamesArgument = config.Argument(
                    "[assemblyName]",
                    "Assembly name to resolve TagHelperDescriptors in.",
                    multipleValues: true);

                if (projectArgument != null)
                {
                    config.OnExecute(
                        () => Dispatch(
                            projectArgument,
                            assemblyNamesArgument,
                            protocolOption,
                            frameworkOption,
                            configurationOption,
                            buildBasePathOption));
                }
                else
                {
                    config.OnExecute(() => Run(assemblyNamesArgument, protocolOption));
                }
            });
        }

        private static int Dispatch(
            CommandArgument projectArgument,
            CommandArgument assemblyNameArgument,
            CommandOption protocolOption,
            CommandOption frameworkOption,
            CommandOption configurationOption,
            CommandOption buildBasePathOption)
        {
            var projectFilePath = projectArgument.Value;
            var projectFile = ProjectReader.GetProject(projectFilePath);
            var targetFrameworks = projectFile
                .GetTargetFrameworks()
                .Select(frameworkInformation => frameworkInformation.FrameworkName);

            NuGetFramework framework;
            if (!TryResolveFramework(frameworkOption, targetFrameworks, projectFilePath, out framework))
            {
                // Could not resolve framework for dispatch. Error was reported, exit early.
                return 0;
            }

            var runtimeIdentifiers = PlatformServices.Default.Runtime.GetAllCandidateRuntimeIdentifiers();
            var projectContext = new ProjectContextBuilder()
                .WithProject(projectFile)
                .WithTargetFramework(framework)
                .WithRuntimeIdentifiers(runtimeIdentifiers)
                .Build();
            var configurationValue = configurationOption.Value() ?? Constants.DefaultConfiguration;
            var dispatchArgs = new List<string>
            {
                CommandName,
                assemblyNameArgument.Value,
            };

            if (protocolOption.HasValue())
            {
                dispatchArgs.Add("--protocol");
                dispatchArgs.Add(protocolOption.Value());
            }
            var dispatchCommand = DotnetToolDispatcher.CreateDispatchCommand(
                dispatchArgs,
                projectContext,
                configurationOption.Value(),
                outputPath: null,
                buildBasePath: buildBasePathOption.Value());

            using (var errorWriter = new StringWriter())
            {
                var commandExitCode = dispatchCommand
                    .ForwardStdErr(errorWriter)
                    .ForwardStdOut()
                    .Execute()
                    .ExitCode;

                if (commandExitCode != 0)
                {
                    ReportError(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            Resources.FailedToExecuteRazorTooling,
                            errorWriter.ToString()));
                }

                return 0;
            }
        }

        private static int Run(CommandArgument assemblyNamesArgument, CommandOption protocolOption)
        {
            int protocol;
            if (protocolOption.HasValue())
            {
                var protocolOptionValue = protocolOption.Value();
                if (!int.TryParse(protocolOptionValue, out protocol))
                {
                    ReportError(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            Resources.CouldNotParseProvidedProtocol,
                            protocolOptionValue));
                    return 0;
                }
            }
            else
            {
                protocol = AssemblyTagHelperDescriptorResolver.DefaultProtocolVersion;
            }

            var descriptorResolver = new AssemblyTagHelperDescriptorResolver()
            {
                ProtocolVersion = protocol
            };

            var errorSink = new ErrorSink();
            var resolvedDescriptors = new List<TagHelperDescriptor>();
            for (var i = 0; i < assemblyNamesArgument.Values.Count; i++)
            {
                var assemblyName = assemblyNamesArgument.Values[i];
                var descriptors = descriptorResolver.Resolve(assemblyName, errorSink);
                resolvedDescriptors.AddRange(descriptors);
            }

            ReportResults(resolvedDescriptors, errorSink.Errors);

            return 0;
        }

        private static bool TryResolveFramework(
            CommandOption providedFrameworkOption,
            IEnumerable<NuGetFramework> availableFrameworks,
            string projectFilePath,
            out NuGetFramework resolvedFramework)
        {
            NuGetFramework framework;
            if (providedFrameworkOption.HasValue())
            {
                var frameworkOptionValue = providedFrameworkOption.Value();
                framework = NuGetFramework.Parse(frameworkOptionValue);

                if (!availableFrameworks.Contains(framework))
                {
                    ReportError(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            Resources.ProjectDoesNotSupportProvidedFramework,
                            projectFilePath,
                            frameworkOptionValue));

                    resolvedFramework = null;
                    return false;
                }
            }
            else
            {
                framework = availableFrameworks.First();
            }

            resolvedFramework = framework;
            return true;
        }

        private static void ReportResults(IEnumerable<TagHelperDescriptor> descriptors, IEnumerable<RazorError> errors)
        {
            var resolvedResult = new ResolvedTagHelperDescriptorsResult
            {
                Descriptors = descriptors,
                Errors = errors
            };

            var serializedResult = JsonConvert.SerializeObject(resolvedResult, Formatting.Indented);

            Reporter.Output.WriteLine(serializedResult);
        }

        private static void ReportError(string message)
        {
            var error = new RazorError(message, SourceLocation.Zero, length: 0);
            ReportResults(descriptors: null, errors: new[] { error });
        }

        private class ResolvedTagHelperDescriptorsResult
        {
            public IEnumerable<TagHelperDescriptor> Descriptors { get; set; }

            public IEnumerable<RazorError> Errors { get; set; }
        }
    }
}
