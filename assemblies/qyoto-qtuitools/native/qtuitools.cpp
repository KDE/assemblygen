/***************************************************************************
                          qtuitools.cpp  -  description
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

#include <qtuitools_smoke.h>

extern TypeHandler Qyoto_qtuitools_handlers[];

static bool IsContainedInstanceQtUiTools(smokeqyoto_object *o)
{
    return false;
}

static const char * qyoto_resolve_classname_qtuitools(smokeqyoto_object * o)
{
    return qyoto_modules[o->smoke].binding->className(o->classId);
}


extern "C" Q_DECL_EXPORT void
Init_qyoto_qtuitools()
{
    init_qtuitools_Smoke();

    QByteArray prefix("Qyoto.");

    QHash<int, QByteArray> qtuitools_classname;
    for (int i = 1; i <= qtuitools_Smoke->numClasses; i++) {
        QByteArray name(qtuitools_Smoke->classes[i].className);
        name.replace("::", ".");
        if (name != "QSsl") {
            name.prepend(prefix);
        }
        qtuitools_classname.insert(i, name);
    }
    static Qyoto::Binding binding = Qyoto::Binding(qtuitools_Smoke, qtuitools_classname);
    QyotoModule module = { "qyoto_qtuitools", qyoto_resolve_classname_qtuitools, IsContainedInstanceQtUiTools, &binding };
    qyoto_modules[qtuitools_Smoke] = module;
}
