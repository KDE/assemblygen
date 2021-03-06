/***************************************************************************
                          qtxmlpatterns.cpp  -  description
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

#include <qtxmlpatterns_smoke.h>

static bool IsContainedInstanceQtXmlPatterns(smokeqyoto_object *o)
{
    return false;
}

static const char * qyoto_resolve_classname_qtxmlpatterns(smokeqyoto_object * o)
{
    return qyoto_modules[o->smoke].binding->className(o->classId);
}


extern "C" Q_DECL_EXPORT void
Init_qyoto_qtxmlpatterns()
{
    init_qtxmlpatterns_Smoke();

    QByteArray prefix("QtXmlPatterns.");

    QHash<int, QByteArray> qtxmlpatterns_classname;
    for (int i = 1; i <= qtxmlpatterns_Smoke->numClasses; i++) {
        QByteArray name(qtxmlpatterns_Smoke->classes[i].className);
        name.replace("::", ".");
        name.prepend(prefix);
        qtxmlpatterns_classname.insert(i, name);
    }
    static Qyoto::Binding binding = Qyoto::Binding(qtxmlpatterns_Smoke, qtxmlpatterns_classname);
    QyotoModule module = { "qyoto_qtxmlpatterns", qyoto_resolve_classname_qtxmlpatterns, IsContainedInstanceQtXmlPatterns, &binding };
    qyoto_modules[qtxmlpatterns_Smoke] = module;
}
