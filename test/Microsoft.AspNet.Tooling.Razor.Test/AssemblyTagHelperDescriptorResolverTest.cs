// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNet.Razor;
using Microsoft.AspNet.Razor.Compilation.TagHelpers;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.AspNet.Razor.Test.Internal;
using Microsoft.Extensions.PlatformAbstractions;
using Xunit;

namespace Microsoft.AspNet.Tooling.Razor
{
    public class RazorPluginTest
    {
        private const string DefaultPrefix = "";

        private static readonly TypeInfo CustomTagHelperTypeInfo = typeof(CustomTagHelper).GetTypeInfo();
        private static readonly string CustomTagHelperAssembly = CustomTagHelperTypeInfo.Assembly.GetName().Name;
        private static readonly TagHelperDescriptor CustomTagHelperDescriptor =
            new TagHelperDescriptor
            {
                Prefix = DefaultPrefix,
                TagName = "custom",
                TypeName = CustomTagHelperTypeInfo.FullName,
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
            var assemblyLoadContext = PlatformServices.Default.AssemblyLoadContextAccessor.Default;
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
            var assemblyLoadContext = PlatformServices.Default.AssemblyLoadContextAccessor.Default;
            var assemblyNameLookups = new Dictionary<string, IEnumerable<ITypeInfo>>
            {
                { CustomTagHelperAssembly, new[] { new RuntimeTypeInfo(typeof(CustomTagHelper).GetTypeInfo()) } }
            };
            var descriptorResolver = new TestAssemblyTagHelperDescriptorResolver(assemblyNameLookups);
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
            var assemblyLoadContext = PlatformServices.Default.AssemblyLoadContextAccessor.Default;
            var assemblyNameLookups = new Dictionary<string, IEnumerable<ITypeInfo>>
            {
                { CustomTagHelperAssembly, new[] { new RuntimeTypeInfo(typeof(DesignTimeTagHelper).GetTypeInfo()) } }
            };
            var descriptorResolver = new TestAssemblyTagHelperDescriptorResolver(assemblyNameLookups);
            var expectedDescriptor = new TagHelperDescriptor
            {
                Prefix = DefaultPrefix,
                TagName = "design-time",
                TypeName = typeof(DesignTimeTagHelper).FullName,
                AssemblyName = typeof(DesignTimeTagHelper).GetTypeInfo().Assembly.GetName().Name,
                AllowedChildren = new[] { "br" },
                TagStructure = TagStructure.NormalOrSelfClosing,
#if DNX451
                DesignTimeDescriptor = new TagHelperDesignTimeDescriptor
                {
                    OutputElementHint = "strong"
                }
#endif
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
            var assemblyLoadContext = PlatformServices.Default.AssemblyLoadContextAccessor.Default;
            var assemblyNameLookups = new Dictionary<string, IEnumerable<ITypeInfo>>
            {
                { CustomTagHelperAssembly, new[] { new RuntimeTypeInfo(typeof(InvalidTagHelper).GetTypeInfo()) } }
            };
            var descriptorResolver = new TestAssemblyTagHelperDescriptorResolver(assemblyNameLookups);
            var errorSink = new ErrorSink();

            // Act
            var descriptors = descriptorResolver.Resolve(CustomTagHelperAssembly, assemblyLoadContext, errorSink);

            // Assert
            Assert.NotEmpty(descriptors);
            var error = Assert.Single(errorSink.Errors);
            Assert.Equal(
                "Tag helpers cannot target tag name 'inv@lid' because it contains a '@' character.",
                error.Message,
                StringComparer.Ordinal);
            Assert.Equal(SourceLocation.Zero, error.Location);
            Assert.Equal(0, error.Length);
        }

        private class TestAssemblyTagHelperDescriptorResolver : AssemblyTagHelperDescriptorResolver
        {
            private readonly IDictionary<string, IEnumerable<ITypeInfo>> _assemblyTypeLookups;

            public TestAssemblyTagHelperDescriptorResolver(
                IDictionary<string, IEnumerable<ITypeInfo>> assemblyTypeLookups)
            {
                _assemblyTypeLookups = assemblyTypeLookups;
            }

            protected override IEnumerable<ITypeInfo> GetTagHelperTypes(
                string assemblyName,
                IAssemblyLoadContext assemblyLoadContext,
                ErrorSink errorSink)
            {
                return _assemblyTypeLookups[assemblyName];
            }
        }

        private class CustomTagHelper : TagHelper
        {
        }

        [RestrictChildren("br")]
        [OutputElementHint("strong")]
        [HtmlTargetElement("design-time", TagStructure = TagStructure.NormalOrSelfClosing)]
        private class DesignTimeTagHelper : TagHelper
        {
        }

        [HtmlTargetElement("inv@lid")]
        private class InvalidTagHelper : TagHelper
        {
        }
    }
}