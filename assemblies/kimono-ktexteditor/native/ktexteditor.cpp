/***************************************************************************
                        ktexteditor.cpp  -  description
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
#include <smoke/ktexteditor_smoke.h>

static const char *
resolve_classname_KTextEditor(smokeqyoto_object * o)
{
	return qyoto_modules[o->smoke].binding->className(o->classId);
}

static bool
IsContainedInstanceKTextEditor(smokeqyoto_object* /*o*/)
{
	// all cases are handled in the qyoto module
	return false;
}

extern "C" Q_DECL_EXPORT
void Init_kimono_ktexteditor()
{
    init_ktexteditor_Smoke();

    QByteArray prefix("Kimono.");

    static QHash<int, QByteArray> ktexteditor_classname;
    for (int i = 1; i <= ktexteditor_Smoke->numClasses; i++) {
        QByteArray name(ktexteditor_Smoke->classes[i].className);
        name.replace("::", ".");
        if (!name.startsWith("KTextEditor")) {
            name.prepend(prefix);
        }
        ktexteditor_classname.insert(i, strdup(name.constData()));
    }
    static Qyoto::Binding ktexteditor_binding = Qyoto::Binding(ktexteditor_Smoke, ktexteditor_classname);
    QyotoModule module = { "Kimono_ktexteditor", resolve_classname_KTextEditor, IsContainedInstanceKTextEditor, &ktexteditor_binding };
    qyoto_modules[ktexteditor_Smoke] = module;
}
