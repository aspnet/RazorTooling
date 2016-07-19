// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Razor.Design.Internal;
using Microsoft.AspNetCore.Razor.Runtime.TagHelpers;
using Microsoft.AspNetCore.Razor.Test.Internal;
using Microsoft.AspNetCore.Razor.Tools.Test.Infrastructure;
using Microsoft.AspNetCore.Testing.xunit;
using Newtonsoft.Json;
using RazorToolingTestApp.Library;
using RazorToolingTestApp.LibraryPackage;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Tools.Test
{
    [Collection(nameof(ToolTestFixture))]
    public class ResolveTagHelperTests
    {
        private const string TestProjectName = "RazorToolingTestApp";
        private const string TestTagHelperProjectName = TestProjectName + ".Library";

        public ResolveTagHelperTests(ToolTestFixture toolTestFixture)
        {
            Fixture = toolTestFixture;
        }

        public ToolTestFixture Fixture { get; }

        public string TestProjectDirectory => Path.Combine(Fixture.TestAppDirectory, TestProjectName);

        public string TestProjectFile => Path.Combine(TestProjectDirectory, "project.json");

        [Fact]
        public void ResolveTagHelpersGeneratesExpectedOutput_WithDefaultParameters()
        {
            // Act & Assert
            ResolveTagHelpersGeneratesExpectedOutput();
        }

        [Fact]
        public void ResolveTagHelpersGeneratesExpectedOutput_WithNetCoreAppFramework()
        {
            // Act & Assert
            ResolveTagHelpersGeneratesExpectedOutput("-f", "netcoreapp1.0");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]

        public void ResolveTagHelpersGeneratesExpectedOutput_WithDesktopFramework()
        {
            // Act & Assert
            ResolveTagHelpersGeneratesExpectedOutput("-f", "net451");
        }

        [Theory]
        [InlineData("Debug")]
        [InlineData("Release")]
        public void ResolveTagHelpersGeneratesExpectedOutput_WithConfiguration(string configuration)
        {
            // Act & Assert
            ResolveTagHelpersGeneratesExpectedOutput("-c", configuration);
        }

        [Fact]
        public void ResolveTagHelpersGeneratesExpectedOutput_WithBuildBasePath()
        {
            // Arrange
            var testBin = Path.Combine(Fixture.TestArtifactsDirectory, "testbin");

            // Act & Assert
            ResolveTagHelpersGeneratesExpectedOutput("-b", testBin);
        }

        [Fact]
        public void ResolveTagHelpersDoesNotDispatchForPackageAssemblies()
        {
            // Arrange
            var tagHelperAssemblyName = typeof(MultiEnumTagHelper).GetTypeInfo().Assembly.GetName().Name;
            var tagHelperDescriptorFactory = new TagHelperDescriptorFactory(designTime: true);
            var tagHelperTypeResolver = new TagHelperTypeResolver();
            var errorSink = new ErrorSink();
            var tagHelperTypes = tagHelperTypeResolver.Resolve(tagHelperAssemblyName, SourceLocation.Zero, errorSink);
            var expectedDescriptors = tagHelperTypes.SelectMany(type =>
                tagHelperDescriptorFactory.CreateDescriptors(tagHelperAssemblyName, type, errorSink));

            // Ensure descriptors were determined without error.
            Assert.Empty(errorSink.Errors);

            // Act
            var output = DotNet(
                "razor-tooling",
                "resolve-taghelpers",
                TestProjectFile,
                tagHelperAssemblyName,
                "-b",
                "doesnotexist").ToString();

            // Assert
            var resolveTagHelpersResult = JsonConvert.DeserializeObject<ResolvedTagHelperDescriptorsResult>(output);
            Assert.Empty(resolveTagHelpersResult.Errors);
            Assert.Equal(
                expectedDescriptors,
                resolveTagHelpersResult.Descriptors,
                CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void ResolveTagHelpersDispatchesForPackageAssembliesIfAppIsBuilt()
        {
            // Arrange
            DotNet("build");

            var tagHelperAssemblyName = typeof(MultiEnumTagHelper).GetTypeInfo().Assembly.GetName().Name;
            var tagHelperDescriptorFactory = new TagHelperDescriptorFactory(designTime: true);
            var tagHelperTypeResolver = new TagHelperTypeResolver();
            var errorSink = new ErrorSink();
            var tagHelperTypes = tagHelperTypeResolver.Resolve(tagHelperAssemblyName, SourceLocation.Zero, errorSink);
            var expectedDescriptors = tagHelperTypes.SelectMany(type =>
                tagHelperDescriptorFactory.CreateDescriptors(tagHelperAssemblyName, type, errorSink));

            // Ensure descriptors were determined without error.
            Assert.Empty(errorSink.Errors);

            // Act
            var output = DotNet(
                "razor-tooling",
                "resolve-taghelpers",
                TestProjectFile,
                tagHelperAssemblyName).ToString();

            // Assert
            var resolveTagHelpersResult = JsonConvert.DeserializeObject<ResolvedTagHelperDescriptorsResult>(output);
            Assert.Empty(resolveTagHelpersResult.Errors);
            Assert.Equal(
                expectedDescriptors,
                resolveTagHelpersResult.Descriptors,
                CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        private void ResolveTagHelpersGeneratesExpectedOutput(params string[] buildArgs)
        {
            // Build the project so TagHelpers can be resolved.
            DotNet("build", buildArgs);

            var tagHelperDescriptorFactory = new TagHelperDescriptorFactory(designTime: true);
            var errorSink = new ErrorSink();
            var expectedDescriptors = tagHelperDescriptorFactory.CreateDescriptors(
                TestTagHelperProjectName,
                typeof(InformationalTagHelper),
                errorSink);
            var resolveArgs = new[] { "resolve-taghelpers", TestProjectFile, TestTagHelperProjectName }
                .Concat(buildArgs)
                .ToArray();

            // Ensure no errors were created on descriptor creation.
            Assert.Empty(errorSink.Errors);

            var output = DotNet("razor-tooling", resolveArgs).ToString();

            var resolveTagHelpersResult = JsonConvert.DeserializeObject<ResolvedTagHelperDescriptorsResult>(output);
            Assert.Empty(resolveTagHelpersResult.Errors);
            Assert.Equal(
                expectedDescriptors,
                resolveTagHelpersResult.Descriptors,
                CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        private StringBuilder DotNet(string commandName, params string[] args) =>
            Fixture.DotNet(TestProjectDirectory, commandName, args);
    }
}
