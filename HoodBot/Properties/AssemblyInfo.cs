using System;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("HoodBot")]
[assembly: AssemblyDescription("All-purpose wiki bot")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyCopyright("Copyright © Robert Morley 2019")]
[assembly: AssemblyProduct("HoodBot")]
[assembly: AssemblyTrademark("")]
[assembly: ComVisible(false)]
[assembly: NeutralResourcesLanguage("en-CA", UltimateResourceFallbackLocation.MainAssembly)]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif