/***************************************************************************
                        plasma.cpp  -  description
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

#ifndef _GNU_SOURCE
#define _GNU_SOURCE
#endif

#include <stdio.h>

#include <QByteArray>
#include <QHash>

#include <qyoto.h>
#include <callbacks.h>
#include <qyotosmokebinding.h>

#include <smoke.h>
#include <smoke/plasma_smoke.h>

static const char *
resolve_classname_Plasma(smokeqyoto_object * o)
{
    return qyoto_modules[o->smoke].binding->className(o->classId);
}

static Smoke::ModuleIndex plasmaExtenderIndex;

static bool
IsContainedInstancePlasma(smokeqyoto_object* o)
{
    // plasma extenders are always contained instances
    if (o->smoke->isDerivedFrom(o->smoke, o->classId, plasmaExtenderIndex.smoke, plasmaExtenderIndex.index)) {
            return true;
    }

    return false;
}

extern TypeHandler Kimono_Plasma_handlers[];

extern "C" Q_DECL_EXPORT
void Init_kimono_plasma()
{
    init_plasma_Smoke();

    plasmaExtenderIndex = plasma_Smoke->idClass("Plasma::Extender");

    QByteArray prefix("Kimono.");

    static QHash<int, QByteArray> plasma_classname;
    for (int i = 1; i <= plasma_Smoke->numClasses; i++) {
        QByteArray name(plasma_Smoke->classes[i].className);
        name.replace("::", ".");
        if (!name.startsWith("Plasma")) {
            name.prepend(prefix);
        }
        plasma_classname.insert(i, strdup(name.constData()));
    }

    static Qyoto::Binding plasma_binding = Qyoto::Binding(plasma_Smoke, plasma_classname);
    QyotoModule module = { "Kimono_plasma", resolve_classname_Plasma, IsContainedInstancePlasma, &plasma_binding };
    qyoto_modules[plasma_Smoke] = module;

    qyoto_install_handlers(Kimono_Plasma_handlers);
}
