// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Indicate that this assembly is generated by MCG
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public sealed class McgGeneratedAssemblyAttribute : Attribute
    {
        public McgGeneratedAssemblyAttribute()
        {
        }
    }
}
