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

#include <QByteArray>
#include <QMetaObject>
#include <QMetaMethod>

#include <smoke.h>

extern "C" {

const QMetaObject *GetMetaObject(Smoke *smoke, Smoke::Index classId) {
    const Smoke::Class& klass = smoke->classes[classId];

    Smoke::Index methodNameId = smoke->idMethodName("staticMetaObject").index;
    if (!methodNameId) {
        return false;
    }
    Smoke::Index methodMapId = smoke->idMethod(classId, methodNameId).index;
    if (!methodMapId) {
        return false;
    }

    const Smoke::Method& meth = smoke->methods[smoke->methodMaps[methodMapId].method];
    Smoke::ClassFn fn = klass.classFn;
    Smoke::StackItem ret;
    (*fn)(meth.method, 0, &ret);

    return (QMetaObject*) ret.s_class;
}

Q_DECL_EXPORT bool GetProperties(Smoke* smoke, Smoke::Index classId, void (*addProp)(const char*, const char*, bool, bool))
{
    const QMetaObject *mo = GetMetaObject(smoke, classId);
    if (!mo) {
        return false;
    }

    for (int i = mo->propertyOffset(); i < mo->propertyCount(); ++i) {
        const QMetaProperty& prop = mo->property(i);
        (*addProp)(prop.name(), prop.isFlagType() ? "uint" : prop.typeName(), prop.isWritable(), prop.isEnumType());
    }
    return true;
}

typedef void (*AddSignal)(const char *signature, const char *name, const char *returnType, const QMetaMethod *method);

Q_DECL_EXPORT void GetSignals(Smoke *smoke, const Smoke::Class *klass, AddSignal addSignalFn) {
    Smoke::Index classId = klass - smoke->classes;
    const QMetaObject *mo = GetMetaObject(smoke, classId);

    if (!mo) {
        qWarning("GetSignals: invalid meta-object for class %s", smoke->className(classId));
        return;
    }

    for (int i = mo->methodOffset(); i < mo->methodCount(); ++i) {
        const QMetaMethod &method = mo->method(i);
        if (method.methodType() == QMetaMethod::Signal) {
            QByteArray methodSig(method.signature());
            (*addSignalFn)(methodSig, methodSig.left(methodSig.indexOf('(')), method.typeName(), &method);
        }
    }
}

typedef void (*AddParameter)(const char *type, const char *name);

Q_DECL_EXPORT void GetMetaMethodParameters(const QMetaMethod *method, AddParameter addParamFn) {
    for (int i = 0; i < method->parameterTypes().length(); i++) {
        (*addParamFn)(method->parameterTypes().at(i), method->parameterNames().at(i));
    }
}

}
