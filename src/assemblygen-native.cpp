/*
    Generator for .NET assemblies utilizing SMOKE libraries
    Copyright (C) 2009, 2010 Arno Rehn <arno@arnorehn.de>

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

#include <QFileInfo>
#include <QLibrary>
#include <QMetaProperty>
#include <QRegExp>
#include <QtDebug>

#include <smoke.h>

typedef void (*InitSmokeFn)();

extern "C" {

Q_DECL_EXPORT Smoke* InitSmoke(const char* lib)
{
    QFileInfo fileInfo(lib);

    QByteArray module = fileInfo.baseName().replace(QRegExp("^(lib)?smoke"), QString()).toLatin1();
    QByteArray symbol = "init_" + module + "_Smoke";

    QLibrary qLib(lib);
    InitSmokeFn init = (InitSmokeFn) qLib.resolve(symbol.constData());

    if (!qLib.isLoaded() || !init) {
        qWarning() << qLib.errorString();
        return 0;
    }

    (*init)();
    symbol = module + "_Smoke";
    void* smoke = qLib.resolve(symbol.constData());

    if (!smoke) {
        qWarning() << qLib.errorString();
        return 0;
    }
    return *(Smoke**) smoke;
}

Q_DECL_EXPORT void DestroySmoke(Smoke* smoke)
{
    delete smoke;
}

Q_DECL_EXPORT bool GetModuleIndexFromClassName(const char* name, Smoke** smoke, short* index)
{
    Smoke::ModuleIndex mi = Smoke::classMap[name];
    *smoke = mi.smoke;
    *index = mi.index;
    if (!mi.index)
        return false;
    return true;
}

Q_DECL_EXPORT qint64 GetEnumValue(Smoke* smoke, Smoke::Method* meth)
{
    Smoke::ClassFn fn = smoke->classes[meth->classId].classFn;
    Smoke::StackItem ret;
    (*fn)(meth->method, 0, &ret);
    return ret.s_enum;
}

Q_DECL_EXPORT bool IsDerivedFrom(const char *className, const char *baseClassName)
{
    return Smoke::isDerivedFrom(className, baseClassName);
}

}
