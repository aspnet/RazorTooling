// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;
using Microsoft.DotNet.Cli.Utils;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Tools.Test.Infrastructure
{
    public class ToolTestFixture : IDisposable
    {
        private readonly string _defaultNugetPackageLocation;

        public ToolTestFixture()
        {
            PackSrcDirectory();

            _defaultNugetPackageLocation = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
            Environment.SetEnvironmentVariable("NUGET_PACKAGES", TestArtifactsTempPackagesDirectory);

            InitializeTestApps();
        }

        public string RootDirectory => Path.GetFullPath(Path.Combine("..", ".."));

        public string SrcDirectory => Path.Combine(RootDirectory, "src");

        public string TestArtifactsDirectoryName => "testartifacts";

        public string TestArtifactsDirectory => Path.Combine(RootDirectory, TestArtifactsDirectoryName);

        public string TestArtifactsTempSrcPackagesDirectory => Path.Combine(TestArtifactsDirectory, "srcpackages");

        public string TestArtifactsInitialPackagesDirectory => Path.Combine(TestArtifactsDirectory, "initialpackages");

        public string TestArtifactsTempPackagesDirectory => Path.Combine(TestArtifactsDirectory, "packages");

        public string TestAppDirectory => Path.Combine(RootDirectory, "testapps");

        public StringBuilder DotNet(string projectDirectory, string commandName, params string[] args)
        {
            var command = Command.CreateDotNet(commandName, args);
            var outputWriter = new StringWriter();

            var exitCode = command
                .ForwardStdErr(Console.Error)
                .ForwardStdOut(outputWriter)
                .WorkingDirectory(projectDirectory)
                .Execute()
                .ExitCode;

            var outputBuilder = outputWriter.GetStringBuilder();

            if (exitCode != 0)
            {
                Assert.True(false, outputBuilder.ToString());
            }

            return outputBuilder;
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("NUGET_PACKAGES", _defaultNugetPackageLocation);
            Directory.Delete(TestArtifactsTempPackagesDirectory, recursive: true);
            Directory.Delete(TestArtifactsTempSrcPackagesDirectory, recursive: true);
        }

        private void PackSrcDirectory()
        {
            Console.WriteLine("Packing src projects...");

            DotNet(SrcDirectory, "restore");

            foreach (var directory in Directory.EnumerateDirectories(SrcDirectory))
            {
                DotNet(directory, "pack", "-o", TestArtifactsTempSrcPackagesDirectory);
            }

            Console.WriteLine("Done.");
        }

        private void InitializeTestApps()
        {
            var outputWriter = new StringWriter();
            var outputBuilder = outputWriter.GetStringBuilder();

            if (Directory.Exists(TestArtifactsTempPackagesDirectory))
            {
                Directory.Delete(TestArtifactsTempPackagesDirectory, recursive: true);
            }

            var nugetConfigXmlLocation = Path.Combine(RootDirectory, "NuGet.config");
            var nugetConfigXml = XDocument.Load(nugetConfigXmlLocation);
            var addElements = nugetConfigXml
                .Root
                .Element("packageSources")
                .Elements("add");
            var nugetConfigSources = new List<string>
            {
                "-s",
                TestArtifactsTempSrcPackagesDirectory,
                "-s",
                TestArtifactsInitialPackagesDirectory,
            };
            foreach (var element in addElements)
            {
                nugetConfigSources.Add("-s");
                var source = element.Attribute("value").Value;

                nugetConfigSources.Add(source);
            }

            foreach (var testProjectDirectory in Directory.EnumerateDirectories(TestAppDirectory))
            {
                Console.WriteLine($"Initializing test app {Path.GetFileName(testProjectDirectory)}.");

                DotNet(testProjectDirectory, "restore", nugetConfigSources.ToArray());

                Console.WriteLine("Done.");
            }
        }
    }
}
