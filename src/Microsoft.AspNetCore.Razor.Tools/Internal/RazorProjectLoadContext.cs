// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NETCOREAPP1_0
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Microsoft.DotNet.ProjectModel.Compilation;
using Microsoft.Extensions.DependencyModel;

namespace Microsoft.DotNet.ProjectModel.Loader
{
    public class RazorProjectLoadContext : AssemblyLoadContext
    {
        private readonly IDictionary<AssemblyName, string> _assemblyPaths;
        private readonly IDictionary<string, string> _nativeLibraries;
        private readonly string _searchPath;

        private static readonly string[] NativeLibraryExtensions;
        private static readonly string[] ManagedAssemblyExtensions = new[]
        {
            ".dll",
            ".ni.dll",
            ".exe",
            ".ni.exe"
        };

        static RazorProjectLoadContext()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                NativeLibraryExtensions = new[] { ".dll" };
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                NativeLibraryExtensions = new[] { ".dylib" };
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                NativeLibraryExtensions = new[] { ".so" };
            }
            else
            {
                NativeLibraryExtensions = new string[0];
            }
        }

        public RazorProjectLoadContext(
            ProjectContext context,
            string configuration)
        {
            var exporter = context.CreateExporter(configuration);
            _assemblyPaths = new Dictionary<AssemblyName, string>(AssemblyNameComparer.OrdinalIgnoreCase);
            _nativeLibraries = new Dictionary<string, string>();
            var rids = DependencyContext.Default?.RuntimeGraph ?? Enumerable.Empty<RuntimeFallbacks>();
            var runtimeIdentifier = context.RuntimeIdentifier;
            var fallbacks = rids.FirstOrDefault(r => r.Runtime.Equals(runtimeIdentifier));

            foreach (var export in exporter.GetAllExports())
            {
                // Process managed assets
                var group = string.IsNullOrEmpty(runtimeIdentifier) ?
                    export.RuntimeAssemblyGroups.GetDefaultGroup() :
                    GetGroup(export.RuntimeAssemblyGroups, runtimeIdentifier, fallbacks);
                if (group != null)
                {
                    foreach (var asset in group.Assets)
                    {
                        _assemblyPaths[asset.GetAssemblyName()] = asset.ResolvedPath;
                    }
                }

                // Process native assets
                group = string.IsNullOrEmpty(runtimeIdentifier) ?
                    export.NativeLibraryGroups.GetDefaultGroup() :
                    GetGroup(export.NativeLibraryGroups, runtimeIdentifier, fallbacks);
                if (group != null)
                {
                    foreach (var asset in group.Assets)
                    {
                        _nativeLibraries[asset.Name] = asset.ResolvedPath;
                    }
                }

                // Process resource assets
                foreach (var asset in export.ResourceAssemblies)
                {
                    var name = asset.Asset.GetAssemblyName();
                    name.CultureName = asset.Locale;
                    _assemblyPaths[name] = asset.Asset.ResolvedPath;
                }
            }

            _searchPath = context.GetOutputPaths(configuration, outputPath: null).CompilationOutputPath;
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            try
            {
                var assembly = Default.LoadFromAssemblyName(assemblyName);
                if (assembly != null)
                {
                    return assembly;
                }
            }
            catch (FileNotFoundException)
            {
            }

            string path;
            if (_assemblyPaths.TryGetValue(assemblyName, out path) || SearchForLibrary(ManagedAssemblyExtensions, assemblyName.Name, out path))
            {
                return LoadFromAssemblyPath(path);
            }

            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string path;
            if (_nativeLibraries.TryGetValue(unmanagedDllName, out path) || SearchForLibrary(NativeLibraryExtensions, unmanagedDllName, out path))
            {
                return LoadUnmanagedDllFromPath(path);
            }

            return LoadUnmanagedDll(unmanagedDllName);
        }

        private static LibraryAssetGroup GetGroup(IEnumerable<LibraryAssetGroup> groups, string runtimeIdentifier, RuntimeFallbacks fallbacks)
        {
            IEnumerable<string> rids = new[] { runtimeIdentifier };
            if (fallbacks != null)
            {
                rids = Enumerable.Concat(rids, fallbacks.Fallbacks);
            }

            foreach (var rid in rids)
            {
                var group = groups.GetRuntimeGroup(rid);
                if (group != null)
                {
                    return group;
                }
            }
            return null;
        }

        private bool SearchForLibrary(string[] extensions, string name, out string path)
        {
            foreach (var extension in extensions)
            {
                var candidate = Path.Combine(_searchPath, name + extension);
                if (File.Exists(candidate))
                {
                    path = candidate;
                    return true;
                }
            }

            path = null;
            return false;
        }

        private class AssemblyNameComparer : IEqualityComparer<AssemblyName>
        {
            public static readonly IEqualityComparer<AssemblyName> OrdinalIgnoreCase = new AssemblyNameComparer();

            private AssemblyNameComparer()
            {
            }

            public bool Equals(AssemblyName x, AssemblyName y)
            {
                // Ignore case because that's what Assembly.Load does.
                return string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.CultureName ?? string.Empty, y.CultureName ?? string.Empty, StringComparison.Ordinal);
            }

            public int GetHashCode(AssemblyName obj)
            {
                var hashCode = 0;
                if (obj.Name != null)
                {
                    hashCode ^= obj.Name.GetHashCode();
                }

                hashCode ^= (obj.CultureName ?? string.Empty).GetHashCode();
                return hashCode;
            }
        }
    }
}
#endif
