using QtCore;
using QtGui;
using System;
using System.Collections.Generic;
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
    [DllImport("qyoto-qtgui-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    static extern void Init_qyoto_qtgui();

    [DllImport("qyoto-qtgui-native", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
    public static extern IntPtr ConstructQListWizardButton();

    [DllImport("qyoto-qtgui-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern void AddWizardButtonToQList(IntPtr ptr, int i);

    [DllImport("qyoto-qtgui-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern void InstallListWizardButtonToQListWizardButton(SmokeMarshallers.GetIntPtr callback);

    private static SmokeMarshallers.GetIntPtr dListWizardButtonToQListWizardButton = ListWizardButtonToQListWizardButton;

    public static IntPtr ListWizardButtonToQListWizardButton(IntPtr ptr) {
        List<QWizard.WizardButton> list = (List<QWizard.WizardButton>) ((GCHandle) ptr).Target;
        IntPtr QList = ConstructQListWizardButton();
        foreach (QWizard.WizardButton wb in list) {
            AddWizardButtonToQList(QList, (int) wb);
        }
        return QList;
    }

    public static void InitSmoke() {
        Init_qyoto_qtgui();

        InstallListWizardButtonToQListWizardButton(dListWizardButtonToQListWizardButton);
    }
}
