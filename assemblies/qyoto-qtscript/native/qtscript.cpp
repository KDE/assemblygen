/***************************************************************************
                          qtscript.cpp  -  description
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

#include <qtscript_smoke.h>

static bool IsContainedInstanceQtScript(smokeqyoto_object *o)
{
    return false;
}

static const char * qyoto_resolve_classname_qtscript(smokeqyoto_object * o)
{
    return qyoto_modules[o->smoke].binding->className(o->classId);
}

extern TypeHandler Qyoto_QtScript_handlers[];

extern "C" Q_DECL_EXPORT void
Init_qyoto_qtscript()
{
    init_qtscript_Smoke();

    QByteArray prefix("Qyoto.");

    QHash<int, QByteArray> qtscript_classname;
    for (int i = 1; i <= qtscript_Smoke->numClasses; i++) {
        QByteArray name(qtscript_Smoke->classes[i].className);
        name.replace("::", ".");
        name.prepend(prefix);
        qtscript_classname.insert(i, name);
    }
    static Qyoto::Binding binding = Qyoto::Binding(qtscript_Smoke, qtscript_classname);
    QyotoModule module = { "qyoto_qtscript", qyoto_resolve_classname_qtscript, IsContainedInstanceQtScript, &binding };
    qyoto_modules[qtscript_Smoke] = module;

    qyoto_install_handlers(Qyoto_QtScript_handlers);
}
