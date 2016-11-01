// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Microsoft.AspNetCore.Razor.Runtime.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Design.Internal
{
    public class AssemblyTagHelperDescriptorResolverTest
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
                ProtocolVersion = protocol
            };
            var typeName = typeof(TagHelperDescriptor).FullName;
            var errorSink = new ErrorSink();
            var expectedMessage =
                $"'{typeName}'s cannot be resolved with protocol '{protocol}'. Protocol not supported.";

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => descriptorResolver.Resolve(CustomTagHelperAssembly, errorSink));
            Assert.Equal(expectedMessage, exception.Message, StringComparer.Ordinal);
        }

        [Fact]
        public void Resolve_ResolvesTagHelperDescriptors()
        {
            // Arrange
            var assemblyNameLookups = new Dictionary<string, IEnumerable<TypeInfo>>
            {
                { CustomTagHelperAssembly, new[] { typeof(CustomTagHelper).GetTypeInfo() } }
            };
            var descriptorResolver = new AssemblyTagHelperDescriptorResolver(new TestTagHelperTypeResolver(assemblyNameLookups));
            var errorSink = new ErrorSink();

            // Act
            var descriptors = descriptorResolver.Resolve(CustomTagHelperAssembly, errorSink);

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
            var assemblyNameLookups = new Dictionary<string, IEnumerable<TypeInfo>>
            {
                { CustomTagHelperAssembly, new[] { typeof(DesignTimeTagHelper).GetTypeInfo() } }
            };
            var descriptorResolver = new AssemblyTagHelperDescriptorResolver(new TestTagHelperTypeResolver(assemblyNameLookups));
            var expectedDescriptor = new TagHelperDescriptor
            {
                Prefix = DefaultPrefix,
                TagName = "design-time",
                TypeName = typeof(DesignTimeTagHelper).FullName,
                AssemblyName = typeof(DesignTimeTagHelper).GetTypeInfo().Assembly.GetName().Name,
                AllowedChildren = new[] { "br" },
                TagStructure = TagStructure.NormalOrSelfClosing,
                DesignTimeDescriptor = new TagHelperDesignTimeDescriptor
                {
                    OutputElementHint = "strong"
                }
            };
            var errorSink = new ErrorSink();

            // Act
            var descriptors = descriptorResolver.Resolve(CustomTagHelperAssembly, errorSink);

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
            var assemblyNameLookups = new Dictionary<string, IEnumerable<TypeInfo>>
            {
                { CustomTagHelperAssembly, new[] { typeof(InvalidTagHelper).GetTypeInfo() } }
            };

            var descriptorResolver = new AssemblyTagHelperDescriptorResolver(new TestTagHelperTypeResolver(assemblyNameLookups));
            var errorSink = new ErrorSink();

            // Act
            var descriptors = descriptorResolver.Resolve(CustomTagHelperAssembly, errorSink);

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

        private class TestTagHelperTypeResolver : TagHelperTypeResolver
        {
            private readonly IDictionary<string, IEnumerable<TypeInfo>> _assemblyTypeLookups;

            public TestTagHelperTypeResolver(IDictionary<string, IEnumerable<TypeInfo>> assemblyTypeLookups)
            {
                _assemblyTypeLookups = assemblyTypeLookups;
            }

            protected override IEnumerable<TypeInfo> GetExportedTypes(AssemblyName assemblyName)
            {
                return _assemblyTypeLookups[assemblyName.Name];
            }
        }
    }

    public class CustomTagHelper : TagHelper
    {
    }

    [RestrictChildren("br")]
    [OutputElementHint("strong")]
    [HtmlTargetElement("design-time", TagStructure = TagStructure.NormalOrSelfClosing)]
    public class DesignTimeTagHelper : TagHelper
    {
    }

    [HtmlTargetElement("inv@lid")]
    public class InvalidTagHelper : TagHelper
    {
    }
}