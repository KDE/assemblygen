using Qyoto;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyVersion("2.0.0.0")]

// Unnecessary, as the keyfile is given to the compiler as a parameter.
// It may be useful to have it here, though.
[assembly: AssemblyKeyFile ("key.snk")]

[assembly: AssemblySmokeInitializer(typeof(InitQtDBus))]

internal class InitQtDBus {
    [DllImport("qyoto-qtdbus-native", CharSet=CharSet.Ansi)]
    static extern void Init_qyoto_qtdbus();

    public static void InitSmoke() {
        Init_qyoto_qtdbus();
    }
}
