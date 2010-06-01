/***************************************************************************
                          qtsvg.cpp  -  description
                             -------------------
    begin                : Wed Jun 16 2004
    copyright            : (C) 2004 by Richard Dale
    email                : Richard_Dale@tipitina.demon.co.uk
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

#include <qtsvg_smoke.h>

static bool IsContainedInstanceQtSvg(smokeqyoto_object *o)
{
    return false;
}

static const char * qyoto_resolve_classname_qtsvg(smokeqyoto_object * o)
{
    return qyoto_modules[o->smoke].binding->className(o->classId);
}


extern "C" Q_DECL_EXPORT void
Init_qyoto_qtsvg()
{
    init_qtsvg_Smoke();

    QByteArray prefix("Qyoto.");

    QHash<int, QByteArray> qtsvg_classname;
    for (int i = 1; i <= qtsvg_Smoke->numClasses; i++) {
        QByteArray name(qtsvg_Smoke->classes[i].className);
        name.replace("::", ".");
        name.prepend(prefix);
        qtsvg_classname.insert(i, name);
    }
    static Qyoto::Binding binding = Qyoto::Binding(qtsvg_Smoke, qtsvg_classname);
    QyotoModule module = { "qyoto_qtsvg", qyoto_resolve_classname_qtsvg, IsContainedInstanceQtSvg, &binding };
    qyoto_modules[qtsvg_Smoke] = module;
}
