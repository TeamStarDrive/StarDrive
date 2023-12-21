using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyCompany("Zero Sum Games")]
[assembly: AssemblyCopyright("Copyright � Zero Sum Games 2022")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyProduct("StarDrive BlackBox")]

#if !STEAM
[assembly: AssemblyTitle("StarDrive BlackBox")] 
#endif
#if STEAM
[assembly: AssemblyTitle("StarDrive BlackBox")]
#endif


[assembly: AssemblyTrademark("")]
[assembly: CompilationRelaxations(8)]
[assembly: ComVisible(false)]
#if !DEBUG // only enable these settings for Release builds, because we need breakpoint support
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.Default | DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
#endif
[assembly: Guid("b38aad3b-18b8-41a8-b758-0e5614dafc49")]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows=true)]

[assembly: AssemblyVersion("1.41.14824")]
[assembly: AssemblyInformationalVersion("1.41.14824 develop/mars-1.41")]
