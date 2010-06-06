/***************************************************************************
                          solid.cpp  -  description
                             -------------------
    begin                : Sun Jun 06 2010
    copyright            : (C) 2010 by Arno Rehn
    email                : arno@arnorehn.de
 ***************************************************************************/

/***************************************************************************
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU Lesser General Public License as        *
 *   published by the Free Software Foundation; either version 2 of the    *
 *   License, or (at your option) any later version.                       *
 *                                                                         *
 ***************************************************************************/

#include <QByteArray>
#include <QHash>

#include <qyoto.h>
#include <qyotosmokebinding.h>

#include <smoke/solid_smoke.h>

extern TypeHandler Kimono_solid_handlers[];

static bool IsContainedInstanceQtNetwork(smokeqyoto_object *o)
{
    Q_UNUSED(o)
    return false;
}

static const char * qyoto_resolve_classname_solid(smokeqyoto_object * o)
{
    return qyoto_modules[o->smoke].binding->className(o->classId);
}


extern "C" Q_DECL_EXPORT void
Init_kimono_solid()
{
    init_solid_Smoke();

    qyoto_install_handlers(Kimono_solid_handlers);

    QHash<int, QByteArray> solid_classname;
    for (int i = 1; i <= solid_Smoke->numClasses; i++) {
        QByteArray name(solid_Smoke->classes[i].className);
        solid_classname.insert(i, name);
    }
    static Qyoto::Binding binding = Qyoto::Binding(solid_Smoke, solid_classname);
    QyotoModule module = { "kimono_solid", qyoto_resolve_classname_solid, IsContainedInstanceQtNetwork, &binding };
    qyoto_modules[solid_Smoke] = module;
}
