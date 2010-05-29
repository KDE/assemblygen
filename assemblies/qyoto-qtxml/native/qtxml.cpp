/***************************************************************************
                          qtxml.cpp  -  description
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

#include <qtxml_smoke.h>

static bool IsContainedInstanceQtXml(smokeqyoto_object *o)
{
    return false;
}

static const char * qyoto_resolve_classname_qtxml(smokeqyoto_object * o)
{
    return qyoto_modules[o->smoke].binding->className(o->classId);
}


extern "C" Q_DECL_EXPORT void
Init_qyoto_qtxml()
{
    init_qtxml_Smoke();

    QByteArray prefix("Qyoto.");

    QHash<int,char *> qtxml_classname;
    for (int i = 1; i <= qtxml_Smoke->numClasses; i++) {
        QByteArray name(qtxml_Smoke->classes[i].className);
        name.replace("::", ".");
        name.prepend(prefix);
        qtxml_classname.insert(i, strdup(name.constData()));
    }
    static Qyoto::Binding binding = Qyoto::Binding(qtxml_Smoke, qtxml_classname);
    QyotoModule module = { "qyoto_qtxml", qyoto_resolve_classname_qtxml, IsContainedInstanceQtXml, &binding };
    qyoto_modules[qtxml_Smoke] = module;
}
