using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyCompany("Zero Sum Games")]
[assembly: AssemblyCopyright("Copyright © Zero Sum Games 2012")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyProduct("StarDrive")]

#if !STEAM
[assembly: AssemblyTitle("StarDrive")] 
#endif
#if STEAM
[assembly: AssemblyTitle("StarDrive")]
#endif


[assembly: AssemblyTrademark("")]
[assembly: AssemblyVersion("1.0.9.0")]
[assembly: CompilationRelaxations(8)]
[assembly: ComVisible(false)]
#if !DEBUG // only enable these settings for Release builds, because we need breakpoint support
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.Default | DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
#endif
[assembly: Guid("b38aad3b-18b8-41a8-b758-0e5614dafc49")]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows=true)]

[assembly: AssemblyInformationalVersion("Texas_Alpha_1_Release_Candidate_209")]
