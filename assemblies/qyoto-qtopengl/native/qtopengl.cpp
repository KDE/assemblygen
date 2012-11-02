/***************************************************************************
                          qtopengl.cpp  -  description
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

#include <qtopengl_smoke.h>

static bool IsContainedInstanceQtOpenGL(smokeqyoto_object *o)
{
    return false;
}

static const char * qyoto_resolve_classname_qtopengl(smokeqyoto_object * o)
{
    return qyoto_modules[o->smoke].binding->className(o->classId);
}


extern "C" Q_DECL_EXPORT void
Init_qyoto_qtopengl()
{
    init_qtopengl_Smoke();

    QByteArray prefix("QtOpenGL.");

    QHash<int, QByteArray> qtopengl_classname;
    for (int i = 1; i <= qtopengl_Smoke->numClasses; i++) {
        QByteArray name(qtopengl_Smoke->classes[i].className);
        name.replace("::", ".");
        if (name != "QGL") {
            name.prepend(prefix);
        }
        qtopengl_classname.insert(i, name);
    }
    static Qyoto::Binding binding = Qyoto::Binding(qtopengl_Smoke, qtopengl_classname);
    QyotoModule module = { "qyoto_qtopengl", qyoto_resolve_classname_qtopengl, IsContainedInstanceQtOpenGL, &binding };
    qyoto_modules[qtopengl_Smoke] = module;
}
