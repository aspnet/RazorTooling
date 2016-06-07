// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NETCOREAPP1_0
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.AspNetCore.Razor.Runtime.TagHelpers;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.InternalAbstractions;
using Microsoft.DotNet.ProjectModel;
using Microsoft.DotNet.ProjectModel.Graph;
using Microsoft.DotNet.ProjectModel.Loader;
using Microsoft.Extensions.CommandLineUtils;
using NuGet.Frameworks;

namespace Microsoft.AspNetCore.Razor.Tools.Internal
{
    public class PackageOnlyResolveTagHelpersRunCommand : ResolveTagHelpersRunCommand
    {
        private readonly TagHelperTypeResolver packageTagHelperTypeResolver;

        public PackageOnlyResolveTagHelpersRunCommand(AssemblyLoadContext loadContext)
        {
            packageTagHelperTypeResolver = new PackageTagHelperTypeResolver(loadContext);
        }

        protected override AssemblyTagHelperDescriptorResolver CreateDescriptorResolver() =>
            new AssemblyTagHelperDescriptorResolver(packageTagHelperTypeResolver);

        public static bool TryPackageOnlyTagHelperResolution(
            CommandArgument assemblyNamesArgument,
            CommandOption protocolOption,
            CommandOption buildBasePathOption,
            CommandOption configurationOption,
            Project project,
            NuGetFramework framework,
            out int exitCode)
        {
            exitCode = 0;

            if (framework.IsDesktop())
            {
                return false;
            }

            var runtimeIdentifiers = RuntimeEnvironmentRidExtensions.GetAllCandidateRuntimeIdentifiers();
            var projectContext = new ProjectContextBuilder()
                .WithProject(project)
                .WithTargetFramework(framework)
                .WithRuntimeIdentifiers(runtimeIdentifiers)
                .Build();
            var configuration = configurationOption.Value() ?? Constants.DefaultConfiguration;

            var projectRuntimeOutputPath = projectContext.GetOutputPaths(configuration, buildBasePathOption.Value())?.RuntimeOutputPath;
            var projectAssemblyName = project.GetCompilerOptions(framework, configuration).OutputName;
            var projectOutputAssembly = Path.Combine(projectRuntimeOutputPath, projectAssemblyName + ".dll");

            if (File.Exists(projectOutputAssembly))
            {
                // There's already build output. Dispatch to build output; this ensures dependencies have been resolved.
                return false;
            }

            var projectLibraries = projectContext.LibraryManager.GetLibraries();
            var libraryLookup = projectLibraries.ToDictionary(
                library => library.Identity.Name,
                library => library,
                StringComparer.Ordinal);

            foreach (var assemblyName in assemblyNamesArgument.Values)
            {
                if (!IsPackageOnly(assemblyName, libraryLookup))
                {
                    return false;
                }
            }

            var loadContext = projectContext.CreateLoadContext(configuration: "Debug");
            var runner = new PackageOnlyResolveTagHelpersRunCommand(loadContext)
            {
                AssemblyNamesArgument = assemblyNamesArgument,
                ProtocolOption = protocolOption
            };

            exitCode = runner.OnExecute();

            return true;
        }

        private static bool IsPackageOnly(string libraryName, IDictionary<string, LibraryDescription> libraryLookup)
        {
            LibraryDescription library;
            if (!libraryLookup.TryGetValue(libraryName, out library) ||
                library.Identity.Type != LibraryType.Package)
            {
                return false;
            }

            foreach (var dependency in library.Dependencies)
            {
                if (!IsPackageOnly(dependency.Name, libraryLookup))
                {
                    return false;
                }
            }

            return true;
        }

        private class PackageTagHelperTypeResolver : TagHelperTypeResolver
        {
            private readonly AssemblyLoadContext _loadContext;

            public PackageTagHelperTypeResolver(AssemblyLoadContext loadContext)
            {
                _loadContext = loadContext;
            }

            protected override IEnumerable<TypeInfo> GetExportedTypes(AssemblyName assemblyName)
            {
                var assembly = _loadContext.LoadFromAssemblyName(assemblyName);
                var exportedTypes = assembly.ExportedTypes;
                var exportedTypeInfos = exportedTypes.Select(type => type.GetTypeInfo());

                return exportedTypeInfos;
            }
        }
    }
}
#endif
