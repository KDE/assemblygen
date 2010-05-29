/***************************************************************************
                          qtnetwork.cpp  -  description
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

#include <qtnetwork_smoke.h>

extern TypeHandler Qyoto_qtnetwork_handlers[];

static bool IsContainedInstanceQtNetwork(smokeqyoto_object *o)
{
    return false;
}

static const char * qyoto_resolve_classname_qtnetwork(smokeqyoto_object * o)
{
    return qyoto_modules[o->smoke].binding->className(o->classId);
}


extern "C" Q_DECL_EXPORT void
Init_qyoto_qtnetwork()
{
    init_qtnetwork_Smoke();

    qyoto_install_handlers(Qyoto_qtnetwork_handlers);
    QByteArray prefix("Qyoto.");

    QHash<int,char *> qtnetwork_classname;
    for (int i = 1; i <= qtnetwork_Smoke->numClasses; i++) {
        QByteArray name(qtnetwork_Smoke->classes[i].className);
        name.replace("::", ".");
        if (name != "QSsl") {
            name.prepend(prefix);
        }
        qtnetwork_classname.insert(i, strdup(name.constData()));
    }
    static Qyoto::Binding binding = Qyoto::Binding(qtnetwork_Smoke, qtnetwork_classname);
    QyotoModule module = { "qyoto_qtnetwork", qyoto_resolve_classname_qtnetwork, IsContainedInstanceQtNetwork, &binding };
    qyoto_modules[qtnetwork_Smoke] = module;
}
