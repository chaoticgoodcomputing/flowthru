// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Net;
using System.Reflection;

namespace Flowthru.Spark.Utils
{
    /// <summary>
    /// Provides the <see cref="AssemblyInfo"/> for the "Flowthru.Spark" assembly
    /// within the current execution context.
    /// </summary>
    internal static class AssemblyInfoProvider
    {
        private const string FlowthruSparkAssemblyName = "Flowthru.Spark";

        private static readonly Lazy<AssemblyInfo> s_microsoftSparkAssemblyInfo =
          new Lazy<AssemblyInfo>(() => CreateAssemblyInfo(FlowthruSparkAssemblyName));

        internal static AssemblyInfo MicrosoftSparkAssemblyInfo() => s_microsoftSparkAssemblyInfo.Value;

        private static AssemblyInfo CreateAssemblyInfo(string assemblyName)
        {
            Assembly assembly = AppDomain
              .CurrentDomain.GetAssemblies()
              .Single(asm => asm.GetName().Name == assemblyName);

            AssemblyName asmName = assembly.GetName();
            return new AssemblyInfo
            {
                AssemblyName = asmName.Name,
                AssemblyVersion = asmName.Version.ToString(),
                HostName = Dns.GetHostName(),
            };
        }

        internal class AssemblyInfo
        {
            internal string AssemblyName { get; set; }
            internal string AssemblyVersion { get; set; }
            internal string HostName { get; set; }
        }
    }
}
