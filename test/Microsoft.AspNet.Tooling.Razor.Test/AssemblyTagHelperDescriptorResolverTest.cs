// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNet.Razor;
using Microsoft.AspNet.Razor.Compilation.TagHelpers;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.AspNet.Razor.Test.Internal;
using Microsoft.AspNet.Tooling.Razor.Tests;
using Microsoft.Extensions.PlatformAbstractions;
using Xunit;

namespace Microsoft.AspNet.Tooling.Razor
{
    public class RazorPluginTest
    {
        private const string DefaultPrefix = "";

        private static readonly Type CustomTagHelperType = typeof(CustomTagHelper);
        private static readonly string CustomTagHelperAssembly = CustomTagHelperType.Assembly.GetName().Name;
        private static readonly TagHelperDescriptor CustomTagHelperDescriptor =
            new TagHelperDescriptor
            {
                Prefix = DefaultPrefix,
                TagName = "custom",
                TypeName = CustomTagHelperType.FullName,
                AssemblyName = CustomTagHelperAssembly
            };

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(2)]
        public void Resolve_ThrowsWhenInvalidProtocol(int protocol)
        {
            // Arrange
            var descriptorResolver = new AssemblyTagHelperDescriptorResolver()
            {
                Protocol = protocol
            };
            var assemblyLoadContext = new TestAssemblyLoadContext();
            var typeName = typeof(TagHelperDescriptor).FullName;
            var errorSink = new ErrorSink();
            var expectedMessage =
                $"'{typeName}'s cannot be resolved with protocol '{protocol}'. Protocol not supported.";

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => descriptorResolver.Resolve(CustomTagHelperAssembly, assemblyLoadContext, errorSink));
            Assert.Equal(expectedMessage, exception.Message, StringComparer.Ordinal);
        }

        [Fact]
        public void Resolve_ResolvesTagHelperDescriptors()
        {
            // Arrange
            var assembly = new TestAssembly(typeof(CustomTagHelper));
            var assemblyNameLookups = new Dictionary<string, Assembly>
            {
                { CustomTagHelperAssembly, assembly }
            };
            var assemblyLoadContext = new TestAssemblyLoadContext(assemblyNameLookups);
            var descriptorResolver = new AssemblyTagHelperDescriptorResolver();
            var errorSink = new ErrorSink();

            // Act
            var descriptors = descriptorResolver.Resolve(CustomTagHelperAssembly, assemblyLoadContext, errorSink);

            // Assert
            Assert.NotNull(descriptors);
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(CustomTagHelperAssembly, descriptor.AssemblyName, StringComparer.Ordinal);
            Assert.Equal(CustomTagHelperDescriptor, descriptor, CaseSensitiveTagHelperDescriptorComparer.Default);
            Assert.Empty(errorSink.Errors);
        }

        [Fact]
        public void Resolve_ResolvesDesignTimeTagHelperDescriptors()
        {
            // Arrange
            var assembly = new TestAssembly(typeof(DesignTimeTagHelper));
            var assemblyNameLookups = new Dictionary<string, Assembly>
            {
                { CustomTagHelperAssembly, assembly }
            };
            var assemblyLoadContext = new TestAssemblyLoadContext(assemblyNameLookups);
            var descriptorResolver = new AssemblyTagHelperDescriptorResolver();
            var expectedDescriptor = new TagHelperDescriptor
            {
                Prefix = DefaultPrefix,
                TagName = "design-time",
                TypeName = typeof(DesignTimeTagHelper).FullName,
                AssemblyName = typeof(DesignTimeTagHelper).Assembly.GetName().Name,
                AllowedChildren = new[] { "br" },
                TagStructure = TagStructure.NormalOrSelfClosing,
                DesignTimeDescriptor = new TagHelperDesignTimeDescriptor
                {
                    OutputElementHint = "strong"
                }
            };
            var errorSink = new ErrorSink();

            // Act
            var descriptors = descriptorResolver.Resolve(CustomTagHelperAssembly, assemblyLoadContext, errorSink);

            // Assert
            Assert.NotNull(descriptors);
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(CustomTagHelperAssembly, descriptor.AssemblyName, StringComparer.Ordinal);
            Assert.Equal(expectedDescriptor, descriptor, CaseSensitiveTagHelperDescriptorComparer.Default);
            Assert.Empty(errorSink.Errors);
        }

        [Fact]
        public void Resolve_CreatesErrors()
        {
            // Arrange
            var assemblyLoadContext = new ThrowingAssemblyLoadContext("Invalid assembly");
            var descriptorResolver = new AssemblyTagHelperDescriptorResolver();
            var errorSink = new ErrorSink();

            // Act
            var descriptors = descriptorResolver.Resolve("invalid", assemblyLoadContext, errorSink);

            // Assert
            Assert.Empty(descriptors);
            var error = Assert.Single(errorSink.Errors);
            Assert.Equal(
                "Cannot resolve TagHelper containing assembly 'invalid'. Error: Invalid assembly: invalid",
                error.Message,
                StringComparer.Ordinal);
            Assert.Equal(SourceLocation.Zero, error.Location);
            Assert.Equal(7, error.Length);
        }

        private class ThrowingAssemblyLoadContext : TestAssemblyLoadContext, IAssemblyLoadContext
        {
            private readonly string _errorMessage;

            public ThrowingAssemblyLoadContext(string errorMessage)
            {
                _errorMessage = errorMessage;
            }

            Assembly IAssemblyLoadContext.Load(AssemblyName assemblyName)
            {
                throw new Exception(_errorMessage + ": " + assemblyName.Name);
            }
        }
    }

    // Needs to be a public, non nested type to be a valid TagHelper
    public class CustomTagHelper : TagHelper
    {
    }

    [RestrictChildren("br")]
    [OutputElementHint("strong")]
    [HtmlTargetElement("design-time", TagStructure = TagStructure.NormalOrSelfClosing)]
    public class DesignTimeTagHelper : TagHelper
    {
    }
}