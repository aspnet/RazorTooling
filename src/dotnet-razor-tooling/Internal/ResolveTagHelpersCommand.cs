// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using dotnet_razor_tooling;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.Loader;
using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Tooling.Razor.Internal
{
    public static class ResolveTagHelpersCommand
    {
        internal static void Register(CommandLineApplication app)
        {
            app.Command("resolve-taghelpers", config =>
            {
                config.Description = "Resolves TagHelperDescriptors in the specified assembly(s).";
                config.HelpOption("-?|-h|--help");
                var protocolOption = config.Option(
                    "-p|--protocol",
                    "Protocol to resolve TagHelperDescriptors with.",
                    CommandOptionType.SingleValue);
                var outputPath = config.Option(
                    "-o|--outputPath",
                    "The projects output path.",
                    CommandOptionType.SingleValue);
                var project = config.Argument(
                    "[project]",
                    "Path to the project.json for the project resolving TagHelperDescriptors.",
                    multipleValues: false);
                var assemblyNames = config.Argument(
                    "[assemblyName]",
                    "Assembly name to resolve TagHelperDescriptors in.",
                    multipleValues: true);

                config.OnExecute(() =>
                {
                    var projectFilePath = project.Value;
                    var projectContext = ResolveProjectContext(projectFilePath);
                    var outputPathValue = outputPath.Value();
                    var assemblyLoadContext = projectContext.CreateLoadContext(outputPath: outputPathValue);
                    var protocol = protocolOption.HasValue() ?
                        int.Parse(protocolOption.Value()) :
                        AssemblyTagHelperDescriptorResolver.DefaultProtocolVersion;

                    var descriptorResolver = new AssemblyTagHelperDescriptorResolver(assemblyLoadContext)
                    {
                        ProtocolVersion = protocol
                    };

                    var errorSink = new ErrorSink();
                    var resolvedDescriptors = new List<TagHelperDescriptor>();
                    for (var i = 0; i < assemblyNames.Values.Count; i++)
                    {
                        var assemblyName = assemblyNames.Values[i];
                        var descriptors = descriptorResolver.Resolve(assemblyName, errorSink);
                        resolvedDescriptors.AddRange(descriptors);
                    }

                    var resolvedResult = new ResolvedTagHelperDescriptorsResult
                    {
                        Descriptors = resolvedDescriptors,
                        Errors = errorSink.Errors
                    };
                    var serializedResult = JsonConvert.SerializeObject(resolvedResult, Formatting.Indented);

                    Reporter.Output.WriteLine(serializedResult);

                    return 0;
                });
            });
        }

        public static ProjectContext ResolveProjectContext(string projectFilePath)
        {
            var projectContexts = ProjectContext.CreateContextForEachFramework(projectFilePath);
            var projectContext = projectContexts.FirstOrDefault();

            if (projectContext == null)
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.CurrentCulture, Resources.InvalidProjectFile, projectFilePath));
            }

            return projectContext;
        }

        private class ResolvedTagHelperDescriptorsResult
        {
            public IEnumerable<TagHelperDescriptor> Descriptors { get; set; }

            public IEnumerable<RazorError> Errors { get; set; }
        }
    }
}
