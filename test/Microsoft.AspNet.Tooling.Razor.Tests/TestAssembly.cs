// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNet.Tooling.Razor.Tests
{
    public class TestAssembly : Assembly
    {
        private IEnumerable<Type> _exportedTypes;

        public TestAssembly(params Type[] exportedTypes)
        {
            _exportedTypes = exportedTypes;
        }

        public override IEnumerable<Type> ExportedTypes
        {
            get
            {
                return _exportedTypes;
            }
        }

        public override IEnumerable<TypeInfo> DefinedTypes
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override IEnumerable<Module> Modules
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}