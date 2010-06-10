/***************************************************************************
                        kparts.cpp  -  description
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
#include <smoke/kparts_smoke.h>

static const char *
resolve_classname_KParts(smokeqyoto_object * o)
{
	return qyoto_modules[o->smoke].binding->className(o->classId);
}

static bool
IsContainedInstanceKParts(smokeqyoto_object* /*o*/)
{
	// all cases are handled in the qyoto module
	return false;
}

extern TypeHandler Kimono_KParts_handlers[];

extern "C" Q_DECL_EXPORT
void Init_kimono_kparts()
{
    init_kparts_Smoke();

    QByteArray prefix("Kimono.");

    static QHash<int, QByteArray> kparts_classname;
    for (int i = 1; i <= kparts_Smoke->numClasses; i++) {
        QByteArray name(kparts_Smoke->classes[i].className);
        name.replace("::", ".");
        if (!name.startsWith("KParts")) {
            name = prefix + name;
        }
        kparts_classname.insert(i, strdup(name.constData()));
    }
    static Qyoto::Binding kparts_binding = Qyoto::Binding(kparts_Smoke, kparts_classname);
    QyotoModule module = { "Kimono_kparts", resolve_classname_KParts, IsContainedInstanceKParts, &kparts_binding };
    qyoto_modules[kparts_Smoke] = module;

    qyoto_install_handlers(Kimono_KParts_handlers);
}
