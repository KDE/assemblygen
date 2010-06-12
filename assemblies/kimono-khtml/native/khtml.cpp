/***************************************************************************
                        khtml.cpp  -  description
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
#include <smoke/khtml_smoke.h>

static const char *
resolve_classname_khtml(smokeqyoto_object * o)
{
	return qyoto_modules[o->smoke].binding->className(o->classId);
}

static bool
IsContainedInstancekhtml(smokeqyoto_object* /*o*/)
{
	// all cases are handled in the qyoto module
	return false;
}

extern "C" Q_DECL_EXPORT
void Init_kimono_khtml()
{
    init_khtml_Smoke();

    QByteArray prefix("Kimono.");

    static QHash<int, QByteArray> khtml_classname;
    for (int i = 1; i <= khtml_Smoke->numClasses; i++) {
        QByteArray name(khtml_Smoke->classes[i].className);
        name.replace("::", ".");
        if (!name.startsWith("DOM")) {
            name.prepend(prefix);
        }
        khtml_classname.insert(i, strdup(name.constData()));
    }
    static Qyoto::Binding khtml_binding = Qyoto::Binding(khtml_Smoke, khtml_classname);
    QyotoModule module = { "Kimono_khtml", resolve_classname_khtml, IsContainedInstancekhtml, &khtml_binding };
    qyoto_modules[khtml_Smoke] = module;
}
