using Qyoto;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyVersion("2.0.0.0")]

// Unnecessary, as the keyfile is given to the compiler as a parameter.
// It may be useful to have it here, though.
[assembly: AssemblyKeyFile ("key.snk")]

[assembly: AssemblySmokeInitializer(typeof(InitSolid))]

internal class InitSolid {
    [DllImport("kimono-solid-native", CharSet=CharSet.Ansi)]
    static extern void Init_kimono_solid();

    public static void InitSmoke() {
        Init_kimono_solid();
    }
}
