// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using dotnet_razor_tooling;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;

namespace Microsoft.AspNetCore.Tooling.Razor.Internal
{
    public class ResolveTagHelpersRunCommand : ResolveTagHelpersCommandBase
    {
        protected override int OnExecute()
        {
            int protocol;
            if (ProtocolOption.HasValue())
            {
                var protocolOptionValue = ProtocolOption.Value();
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
            for (var i = 0; i < AssemblyNamesArgument.Values.Count; i++)
            {
                var assemblyName = AssemblyNamesArgument.Values[i];
                var descriptors = descriptorResolver.Resolve(assemblyName, errorSink);
                resolvedDescriptors.AddRange(descriptors);
            }

            ReportResults(resolvedDescriptors, errorSink.Errors);

            return 0;
        }
    }
}
