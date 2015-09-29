// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Dnx.Runtime.Common.CommandLine;

namespace Microsoft.AspNet.Tooling.Razor
{
    internal static class ResolveProtocolCommand
    {
        public static void Register(CommandLineApplication app)
        {
            app.Command("resolve-protocol", config =>
            {
                config.Description = "Resolves protocol used to resolve TagHeleprDescriptors.";
                config.HelpOption("-?|-h|--help");
                var clientProtocol = config.Argument(
                    "[clientProtocol]",
                    "Client protocol used to consume returned TagHelperDescriptors.");

                config.OnExecute(() =>
                {
                    var pluginProtocol = new RazorPlugin(messageBroker: null).Protocol;
                    var resolvedProtocol = ResolveProtocol(clientProtocol.Value, pluginProtocol);

                    Console.WriteLine(resolvedProtocol);

                    return 0;
                });
            });
        }

        public static int ResolveProtocol(CommandOption clientProtocolCommand, int pluginProtocol)
        {
            int resolvedProtocol;
            if (clientProtocolCommand.HasValue())
            {
                resolvedProtocol = ResolveProtocol(clientProtocolCommand.Value(), pluginProtocol);
            }
            else
            {
                // Client protocol wasn't provided, use the plugin's protocol.
                resolvedProtocol = pluginProtocol;
            }

            return resolvedProtocol;
        }

        private static int ResolveProtocol(string clientProtocolString, int pluginProtocol)
        {
            var clientProtocol = int.Parse(clientProtocolString);

            // Client and plugin protocols are max values; meaning support is <= value. The goal in this method is
            // to return the maximum protocol supported by both parties (client and plugin).
            var resolvedProtocol = Math.Min(clientProtocol, pluginProtocol);

            return resolvedProtocol;
        }
    }
}
