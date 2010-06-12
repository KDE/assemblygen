using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Qyoto;

[assembly: AssemblyVersion("2.0.0.0")]

// Unnecessary, as the keyfile is given to the compiler as a parameter.
// It may be useful to have it here, though.
[assembly: AssemblyKeyFile ("key.snk")]

[assembly: AssemblySmokeInitializer(typeof(InitKimonoKFile))]

internal class InitKimonoKFile {
    [DllImport("kimono-kfile-native", CharSet=CharSet.Ansi)]
    static extern void Init_kimono_kfile();

    public static void InitSmoke() {
        Init_kimono_kfile();
    }
}
