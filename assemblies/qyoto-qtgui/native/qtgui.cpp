/***************************************************************************
                          qtgui.cpp  -  description
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

#include <qtgui_smoke.h>

extern TypeHandler qtgui_handlers[];
extern bool IsContainedInstanceQtGui(smokeqyoto_object *o);
extern const char * qyoto_resolve_classname_qtgui(smokeqyoto_object * o);

extern "C" Q_DECL_EXPORT void
Init_qyoto_qtgui()
{
    init_qtgui_Smoke();

    qyoto_install_handlers(qtgui_handlers);
    QByteArray prefix("Qyoto.");

    QHash<int,char *> qtgui_classname;
    for (int i = 1; i <= qtgui_Smoke->numClasses; i++) {
        QByteArray name(qtgui_Smoke->classes[i].className);
        name.replace("::", ".");
        if (name != "QAccessible2") {
            name.prepend(prefix);
        }
        qtgui_classname.insert(i, strdup(name.constData()));
    }
    static Qyoto::Binding binding = Qyoto::Binding(qtgui_Smoke, qtgui_classname);
    QyotoModule module = { "qyoto_qtgui", qyoto_resolve_classname_qtgui, IsContainedInstanceQtGui, &binding };
    qyoto_modules[qtgui_Smoke] = module;
}
