// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Tooling.Razor.Internal;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.Tooling.Razor
{
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                var app = new CommandLineApplication
                {
                    Name = "razor-tooling",
                    FullName = "Microsoft Razor Tooling Utility",
                    Description = "Resolves Razor tooling specific information.",
                    ShortVersionGetter = GetInformationalVersion,
                };
                app.HelpOption("-?|-h|--help");

                ResolveProtocolCommand.Register(app);
                ResolveTagHelpersCommand.Register(app);

                app.OnExecute(() =>
                {
                    app.ShowHelp();
                    return 2;
                });

                return app.Execute(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }

        private static string GetInformationalVersion()
        {
            var assembly = typeof(Program).GetTypeInfo().Assembly;
            var attributes = assembly.GetCustomAttributes(
                typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute[];

            var versionAttribute = attributes.Length == 0 ?
                assembly.GetName().Version.ToString() :
                attributes[0].InformationalVersion;

            return versionAttribute;
        }
    }
}
