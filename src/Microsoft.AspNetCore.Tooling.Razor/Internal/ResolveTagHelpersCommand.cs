// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor;
using Microsoft.AspNet.Razor.Compilation.TagHelpers;
using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json;

namespace Microsoft.AspNet.Tooling.Razor.Internal
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
                var assemblyNames = config.Argument(
                    "[name]",
                    "Assembly name to resolve TagHelperDescriptors in.",
                    multipleValues: true);

                config.OnExecute(() =>
                {
                    var protocol = int.Parse(protocolOption.Value());

                    var descriptorResolver = new AssemblyTagHelperDescriptorResolver
                    {
                        Protocol = protocol
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

                    Console.WriteLine(serializedResult);

                    return errorSink.Errors.Any() ? 1 : 0;
                });
            });
        }

        private class ResolvedTagHelperDescriptorsResult
        {
            public IEnumerable<TagHelperDescriptor> Descriptors { get; set; }

            public IEnumerable<RazorError> Errors { get; set; }
        }
    }
}
