#include <QLibrary>
#include <smoke.h>
#include <stdio.h>

typedef void (*InitSmokeFn)();

extern "C" Q_DECL_EXPORT Smoke* InitSmoke(const char* module)
{
    QString lib = "smoke" + QString(module);
    QByteArray symbol = "init_" + QByteArray(module) + "_Smoke";
    InitSmokeFn init = (InitSmokeFn) QLibrary::resolve(lib, symbol.constData());
    (*init)();
    symbol = module + QByteArray("_Smoke");
    return *(Smoke**) QLibrary::resolve(lib, symbol.constData());
}

extern "C" Q_DECL_EXPORT void DestroySmoke(Smoke* smoke)
{
    delete smoke;
}
