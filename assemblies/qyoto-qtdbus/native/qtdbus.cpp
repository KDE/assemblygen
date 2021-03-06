/***************************************************************************
                          qtdbus.cpp  -  description
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

#include <qtdbus_smoke.h>

extern TypeHandler Qyoto_qtdbus_handlers[];

static bool IsContainedInstanceQtDBus(smokeqyoto_object *o)
{
    return false;
}

static const char * qyoto_resolve_classname_qtdbus(smokeqyoto_object * o)
{
    return qyoto_modules[o->smoke].binding->className(o->classId);
}


extern "C" Q_DECL_EXPORT void
Init_qyoto_qtdbus()
{
    init_qtdbus_Smoke();

    qyoto_install_handlers(Qyoto_qtdbus_handlers);
    QByteArray prefix("QtDBus.");

    QHash<int, QByteArray> qtdbus_classname;
    for (int i = 1; i <= qtdbus_Smoke->numClasses; i++) {
        QByteArray name(qtdbus_Smoke->classes[i].className);
        name.replace("::", ".");
        name.prepend(prefix);
        qtdbus_classname.insert(i, name);
    }
    static Qyoto::Binding binding = Qyoto::Binding(qtdbus_Smoke, qtdbus_classname);
    QyotoModule module = { "qyoto_qtdbus", qyoto_resolve_classname_qtdbus, IsContainedInstanceQtDBus, &binding };
    qyoto_modules[qtdbus_Smoke] = module;
}
