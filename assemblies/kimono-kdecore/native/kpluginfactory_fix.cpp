/***************************************************************************
                        kimono.cpp  -  description
                             -------------------
    begin                : Mon Sep 10 2007
    copyright            : (C) 2007 by Arno Rehn
    email                : arno@arnorehn.de
 ***************************************************************************/

/***************************************************************************
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License as published by  *
 *   the Free Software Foundation; either version 2 of the License, or     *
 *   (at your option) any later version.                                   *
 *                                                                         *
 ***************************************************************************/

#include <kpluginfactory.h>

#include <qyoto.h>
#include <callbacks.h>

#include <smoke.h>
#include <smoke/kdecore_smoke.h>
#include <smoke/qtcore_smoke.h>

class KPluginFactory_Create_Caller : public KPluginFactory
{
public:
    QObject *call_create(const char *iface, QWidget *parentWidget,
                         QObject *parent, const QVariantList &args,
                         const QString &keyword)
    {
        return create(iface, parentWidget, parent, args, keyword);
    }
};

extern "C" Q_DECL_EXPORT
void* KPluginFactory_Create(void *self, const char *classname, void *parentWidget, void *parent, void **args, int numArgs, const char *keyword)
{
    smokeqyoto_object *o = (smokeqyoto_object*) (*GetSmokeObject)(self);
    (*FreeGCHandle)(self);
    KPluginFactory_Create_Caller *factory = (KPluginFactory_Create_Caller*) o->ptr;

    QWidget *pw = 0;
    if (parentWidget) {
        o = (smokeqyoto_object*) (*GetSmokeObject)(parentWidget);
        (*FreeGCHandle)(parentWidget);
        pw = (QWidget*) o->smoke->cast(o->ptr, o->classId, o->smoke->idClass("QWidget").index);
    }

    QObject *p = 0;
    if (parent) {
        o = (smokeqyoto_object*) (*GetSmokeObject)(parent);
        (*FreeGCHandle)(parent);
        p = (QObject*) o->smoke->cast(o->ptr, o->classId, o->smoke->idClass("QObject").index);
    }

    QVariantList list;
    for (int i = 0; i < numArgs; i++) {
        o = (smokeqyoto_object*) (*GetSmokeObject)(args[i]);
        (*FreeGCHandle)(args[i]);
        list << *((QVariant*) o->ptr);
    }

    QObject *ret = factory->call_create(classname, pw, p, list, keyword);
    smokeqyoto_object *obj = alloc_smokeqyoto_object(false, qtcore_Smoke, qtcore_Smoke->idClass("QObject").index, ret);
    const char *name = qyoto_resolve_classname(obj);
    return (*CreateInstance)(name, obj);
}