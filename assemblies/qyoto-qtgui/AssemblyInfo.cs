using Qyoto;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Version number is the same as the version of the latest QtWebKit
// for which the classes have been generated.
[assembly: AssemblyVersion("2.0.0.0")]

// Unnecessary, as the keyfile is given to the compiler as a parameter.
// It may be useful to have it here, though.
[assembly: AssemblyKeyFile ("key.snk")]

[assembly: AssemblySmokeInitializer(typeof(InitQtGui))]

internal class InitQtGui {
    [DllImport("qyoto-qtgui-native", CharSet=CharSet.Ansi)]
    static extern void Init_qyoto_qtgui();

    public static void InitSmoke() {
        Init_qyoto_qtgui();
    }
}
