using System.Runtime.CompilerServices;

// Compile-time facade only. Ancot 1.6's DrawAt signature names the old assembly.
// The real 1.7 host redirects UnityEngine* to UnityStubs at runtime. Never ship
// this DLL in a mod's Assemblies folder; it contains no engine implementation.
[assembly: TypeForwardedTo(typeof(UnityEngine.Vector3))]
