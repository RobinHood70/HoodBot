using System;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

[assembly: AssemblyCompany("")]
[assembly: AssemblyCopyright("Copyright © Robert Morley 2019")]
[assembly: AssemblyProduct("HoodBot")]
[assembly: AssemblyTrademark("")]
[assembly: CLSCompliant(true)]
[assembly: ComVisible(false)]
[assembly: NeutralResourcesLanguage("en-CA", UltimateResourceFallbackLocation.MainAssembly)]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif