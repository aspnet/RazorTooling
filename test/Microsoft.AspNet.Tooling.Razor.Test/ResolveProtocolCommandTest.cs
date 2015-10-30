// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNet.Tooling.Razor
{
    public class ResolveProtocolCommandTest
    {
        [Theory]
        [InlineData(1, 1, 1)]
        [InlineData(1, 3, 1)]
        [InlineData(3, 1, 1)]
        [InlineData(-1, 1, 1)]
        [InlineData(23, 15, 15)]
        public void ResolveProtocol_WorksCorrectly(int clientProtocol, int pluginProtocol, int expectedProtocol)
        {
            // Act
            var resolvedProtocol = ResolveProtocolCommand.ResolveProtocol(clientProtocol, pluginProtocol);

            // Assert
            Assert.Equal(expectedProtocol, resolvedProtocol);
        }
    }
}
