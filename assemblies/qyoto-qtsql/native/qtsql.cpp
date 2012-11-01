/***************************************************************************
                          qtsql.cpp  -  description
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

#include <qtsql_smoke.h>

static bool IsContainedInstanceQtSql(smokeqyoto_object *o)
{
    return false;
}

static const char * qyoto_resolve_classname_qtsql(smokeqyoto_object * o)
{
    return qyoto_modules[o->smoke].binding->className(o->classId);
}


extern "C" Q_DECL_EXPORT void
Init_qyoto_qtsql()
{
    init_qtsql_Smoke();

    QByteArray prefix("QtSql.");

    QHash<int, QByteArray> qtsql_classname;
    for (int i = 1; i <= qtsql_Smoke->numClasses; i++) {
        QByteArray name(qtsql_Smoke->classes[i].className);
        name.replace("::", ".");
        if (name != "QSql") {
            name.prepend(prefix);
        }
        qtsql_classname.insert(i, name);
    }
    static Qyoto::Binding binding = Qyoto::Binding(qtsql_Smoke, qtsql_classname);
    QyotoModule module = { "qyoto_qtsql", qyoto_resolve_classname_qtsql, IsContainedInstanceQtSql, &binding };
    qyoto_modules[qtsql_Smoke] = module;
}
