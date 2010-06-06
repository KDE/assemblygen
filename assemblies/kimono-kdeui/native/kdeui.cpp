/***************************************************************************
                        kdeui.cpp  -  description
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
#include <smoke/kdeui_smoke.h>

static const char *
resolve_classname_KDEUi(smokeqyoto_object * o)
{
	return qyoto_modules[o->smoke].binding->className(o->classId);
}

static bool
IsContainedInstanceKDEUi(smokeqyoto_object* /*o*/)
{
	// all cases are handled in the qyoto module
	return false;
}

extern TypeHandler Kimono_KDEUi_handlers[];

extern "C" Q_DECL_EXPORT
void Init_kimono_kdeui()
{
    init_kdeui_Smoke();

    QByteArray prefix("Kimono.");

    static QHash<int, QByteArray> kdeui_classname;
    for (int i = 1; i <= kdeui_Smoke->numClasses; i++) {
        QByteArray name(kdeui_Smoke->classes[i].className);
        name.replace("::", ".");
        if (!name.startsWith("KWallet")) {
            name = prefix + name;
        }
        kdeui_classname.insert(i, strdup(name.constData()));
    }
    static Qyoto::Binding kdeui_binding = Qyoto::Binding(kdeui_Smoke, kdeui_classname);
    QyotoModule module = { "Kimono_kdeui", resolve_classname_KDEUi, IsContainedInstanceKDEUi, &kdeui_binding };
    qyoto_modules[kdeui_Smoke] = module;

    qyoto_install_handlers(Kimono_KDEUi_handlers);
}
