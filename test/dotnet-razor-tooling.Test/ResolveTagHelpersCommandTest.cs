// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using dotnet_razor_tooling;
using Microsoft.AspNetCore.Tooling.Razor.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Tooling.Razor
{
    public class ResolveTagHelpersCommandTest
    {
        [Fact]
        public void ResolveProjectContext_ThrowsWhenNoTargetFrameworks()
        {
            // Arrange
            var projectFilePath = "TestFiles/notfmproject.json";
            var expectedErrorMessage = string.Format(CultureInfo.CurrentCulture, Resources.InvalidProjectFile, projectFilePath);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => ResolveTagHelpersCommand.ResolveProjectContext(projectFilePath));
            Assert.Equal(expectedErrorMessage, ex.Message);
        }

        [Fact]
        public void ResolveProjectContext_ResolvesProjectContextsCorrectly()
        {
            // Arrange
            var projectFilePath = "TestFiles/dnxcoreproject.json";

            // Act
            var projectContext = ResolveTagHelpersCommand.ResolveProjectContext(projectFilePath);

            // Assert
            Assert.Equal("DNXCore", projectContext.TargetFramework.Framework);
        }
    }
}
