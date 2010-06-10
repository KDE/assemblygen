/***************************************************************************
                        kio.cpp  -  description
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
#include <smoke/kio_smoke.h>

static const char *
resolve_classname_KIO(smokeqyoto_object * o)
{
	return qyoto_modules[o->smoke].binding->className(o->classId);
}

static bool
IsContainedInstanceKIO(smokeqyoto_object* /*o*/)
{
	// all cases are handled in the qyoto module
	return false;
}

extern TypeHandler Kimono_KIO_handlers[];

extern "C" Q_DECL_EXPORT
void Init_kimono_kio()
{
    init_kio_Smoke();

    QByteArray prefix("Kimono.");

    static QHash<int, QByteArray> kio_classname;
    for (int i = 1; i <= kio_Smoke->numClasses; i++) {
        QByteArray name(kio_Smoke->classes[i].className);
        name.replace("::", ".");
        if (!name.startsWith("KIO")) {
            name = prefix + name;
        }
        kio_classname.insert(i, strdup(name.constData()));
    }
    static Qyoto::Binding kio_binding = Qyoto::Binding(kio_Smoke, kio_classname);
    QyotoModule module = { "Kimono_kio", resolve_classname_KIO, IsContainedInstanceKIO, &kio_binding };
    qyoto_modules[kio_Smoke] = module;

    qyoto_install_handlers(Kimono_KIO_handlers);
}
