// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Tooling.Razor.Internal
{
    public abstract class ResolveTagHelpersCommandBase
    {
        protected const string CommandName = "resolve-taghelpers";

        protected CommandOption ProtocolOption { get; set; }

        protected CommandArgument AssemblyNamesArgument { get; set; }

        public static void Register<TResolveTagHelpersCommand>(CommandLineApplication app) where
            TResolveTagHelpersCommand : ResolveTagHelpersCommandBase, new()
        {
            var command = new TResolveTagHelpersCommand();
            command.Register(app);
        }

        protected virtual void Configure(CommandLineApplication config)
        {
        }

        protected abstract int OnExecute();

        protected void ReportResults(IEnumerable<TagHelperDescriptor> descriptors, IEnumerable<RazorError> errors)
        {
            var resolvedResult = new ResolvedTagHelperDescriptorsResult
            {
                Descriptors = descriptors,
                Errors = errors
            };

            var serializedResult = JsonConvert.SerializeObject(resolvedResult, Formatting.Indented);

            Reporter.Output.WriteLine(serializedResult);
        }

        protected void ReportError(string message)
        {
            var error = new RazorError(message, SourceLocation.Zero, length: 0);
            ReportResults(descriptors: null, errors: new[] { error });
        }

        private void Register(CommandLineApplication app)
        {
            app.Command(CommandName, config =>
            {
                config.Description = "Resolves TagHelperDescriptors in the specified assembly(s).";
                config.HelpOption("-?|-h|--help");
                ProtocolOption = config.Option(
                    "-p|--protocol",
                    "Protocol to resolve TagHelperDescriptors with.",
                    CommandOptionType.SingleValue);

                Configure(config);

                AssemblyNamesArgument = config.Argument(
                    "[assemblyName]",
                    "Assembly name to resolve TagHelperDescriptors in.",
                    multipleValues: true);

                config.OnExecute((Func<int>)OnExecute);
            });
        }

        private class ResolvedTagHelperDescriptorsResult
        {
            public IEnumerable<TagHelperDescriptor> Descriptors { get; set; }

            public IEnumerable<RazorError> Errors { get; set; }
        }
    }
}
