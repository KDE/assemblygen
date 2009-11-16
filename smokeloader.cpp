#include <QLibrary>
#include <smoke.h>
#include <stdio.h>
#include <QtDebug>

typedef void (*InitSmokeFn)();

extern "C" Q_DECL_EXPORT Smoke* InitSmoke(const char* module)
{
    QString lib = "smoke" + QString(module);
    QByteArray symbol = "init_" + QByteArray(module) + "_Smoke";

    QLibrary qLib(lib);
    InitSmokeFn init = (InitSmokeFn) qLib.resolve(symbol.constData());

    if (!qLib.isLoaded() || !init) {
        qWarning() << qLib.errorString();
        return 0;
    }

    (*init)();
    symbol = module + QByteArray("_Smoke");
    void* smoke = qLib.resolve(symbol.constData());

    if (!smoke) {
        qWarning() << qLib.errorString();
        return 0;
    }
    return *(Smoke**) smoke;
}

extern "C" Q_DECL_EXPORT void DestroySmoke(Smoke* smoke)
{
    delete smoke;
}
