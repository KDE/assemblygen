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

#ifndef _GNU_SOURCE
#define _GNU_SOURCE
#endif

#include <stdio.h>

#include <qyoto.h>
#include <callbacks.h>
#include <qyotosmokebinding.h>

#include <smoke.h>
#include <smoke/kdecore_smoke.h>

#include <QMimeData>
#include <QStringList>

#include <KPluginFactory>
#include <KUrl>

const char *
resolve_classname_KDE(smokeqyoto_object * o)
{
	if (o->smoke->isDerivedFrom(o->smoke->classes[o->classId].className, "QObject")) {
		if (strcmp(o->smoke->classes[o->classId].className, "KParts::ReadOnlyPart") == 0)
			return "KParts.ReadOnlyPartInternal";
		if (strcmp(o->smoke->classes[o->classId].className, "KParts::ReadWritePart") == 0)
			return "KParts.ReadWritePartInternal";
	}
	return qyoto_modules[o->smoke].binding->className(o->classId);
}

bool
IsContainedInstanceKDE(smokeqyoto_object* /*o*/)
{
	// all cases are handled in the qyoto module
	return false;
}

extern TypeHandler Kimono_KDECore_handlers[];

extern "C" Q_DECL_EXPORT
void Init_kimono()
{
    init_kdecore_Smoke();

    QByteArray prefix("Kimono.");

    static QHash<int, QByteArray> kdecore_classname;
    for (int i = 1; i <= kdecore_Smoke->numClasses; i++) {
        QByteArray name(kdecore_Smoke->classes[i].className);
        name.replace("::", ".");
        if (!name.startsWith("KWallet")) {
            name = prefix + name;
        }
        kdecore_classname.insert(i, strdup(name.constData()));
    }
    static Qyoto::Binding kdecore_binding = Qyoto::Binding(kdecore_Smoke, kdecore_classname);
    QyotoModule module = { "Kimono_kdecore", resolve_classname_KDE, IsContainedInstanceKDE, &kdecore_binding };
    qyoto_modules[kdecore_Smoke] = module;

    qyoto_install_handlers(Kimono_KDECore_handlers);
}
