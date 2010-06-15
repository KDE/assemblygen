/***************************************************************************
                        kfile.cpp  -  description
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
#include <smoke/kfile_smoke.h>

static const char *
resolve_classname_KFile(smokeqyoto_object * o)
{
	return qyoto_modules[o->smoke].binding->className(o->classId);
}

static bool
IsContainedInstanceKFile(smokeqyoto_object* /*o*/)
{
	// all cases are handled in the qyoto module
	return false;
}

extern "C" Q_DECL_EXPORT
void Init_kimono_kfile()
{
    init_kfile_Smoke();

    QByteArray prefix("Kimono.");

    static QHash<int, QByteArray> kfile_classname;
    for (int i = 1; i <= kfile_Smoke->numClasses; i++) {
        QByteArray name(kfile_Smoke->classes[i].className);
        name.replace("::", ".");
        name.prepend(prefix);
        kfile_classname.insert(i, strdup(name.constData()));
    }
    static Qyoto::Binding kfile_binding = Qyoto::Binding(kfile_Smoke, kfile_classname);
    QyotoModule module = { "Kimono_kfile", resolve_classname_KFile, IsContainedInstanceKFile, &kfile_binding };
    qyoto_modules[kfile_Smoke] = module;
}
