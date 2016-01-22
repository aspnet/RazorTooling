// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.Tooling.Razor.Internal
{
    public static class ResolveProtocolCommand
    {
        internal static void Register(CommandLineApplication app)
        {
            app.Command("resolve-protocol", config =>
            {
                config.Description = "Resolves protocol used to resolve TagHeleprDescriptors.";
                config.HelpOption("-?|-h|--help");
                var clientProtocolArgument = config.Argument(
                    "[clientProtocol]",
                    "Client protocol used to consume returned TagHelperDescriptors.");

                config.OnExecute(() =>
                {
                    var pluginProtocol = new AssemblyTagHelperDescriptorResolver().Protocol;
                    var clientProtocolString = clientProtocolArgument.Value;
                    var clientProtocol = int.Parse(clientProtocolString);
                    var resolvedProtocol = ResolveProtocol(clientProtocol, pluginProtocol);

                    Console.WriteLine(resolvedProtocol);

                    return 0;
                });
            });
        }

        /// <summary>
        /// Internal for testing.
        /// </summary>
        public static int ResolveProtocol(int clientProtocol, int pluginProtocol)
        {
            // Protocols start at 1 and increase.
            clientProtocol = Math.Max(1, clientProtocol);

            // Client and plugin protocols are max values; meaning support is <= value. The goal in this method is
            // to return the maximum protocol supported by both parties (client and plugin).
            var resolvedProtocol = Math.Min(clientProtocol, pluginProtocol);

            return resolvedProtocol;
        }
    }
}
