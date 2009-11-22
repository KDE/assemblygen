/*
    Generator for .NET assemblies utilizing SMOKE libraries
    Copyright (C) 2009 Arno Rehn <arno@arnorehn.de>

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along
    with this program; if not, write to the Free Software Foundation, Inc.,
    51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/

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
