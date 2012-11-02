/***************************************************************************
                          qtwebkit.cpp  -  QtWebKit ruby extension
                             -------------------
    begin                : 14-07-2008
    copyright            : (C) 2008 by Richard Dale
    email                : richard.j.dale@gmail.com
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

#include <stdio.h>

#include <qyoto.h>
#include <qyotosmokebinding.h>

#include <smoke.h>
#include <smoke/qtwebkit_smoke.h>

const char *
resolve_classname_qtwebkit(smokeqyoto_object * o)
{
	return qyoto_modules[o->smoke].binding->className(o->classId);
}

bool
IsContainedInstanceQtWebKit(smokeqyoto_object* /*o*/)
{
	// all cases are handled in the qyoto module
	return false;
}

extern TypeHandler Qyoto_QtWebKit_handlers[];

extern "C" Q_DECL_EXPORT
void Init_qtwebkit()
{
	init_qtwebkit_Smoke();

    QByteArray prefix("QtWebKit.");

	static QHash<int, QByteArray> classNames;
	for (int i = 1; i <= qtwebkit_Smoke->numClasses; i++) {
		QByteArray name(qtwebkit_Smoke->classes[i].className);
		name.replace("::", ".");
		name.prepend(prefix);
		classNames.insert(i, name);
	}
	
	static Qyoto::Binding binding = Qyoto::Binding(qtwebkit_Smoke, classNames);
	QyotoModule module = { "QtWebKit", resolve_classname_qtwebkit, IsContainedInstanceQtWebKit, &binding };
	qyoto_modules.insert(qtwebkit_Smoke, module);

    qyoto_install_handlers(Qyoto_QtWebKit_handlers);
}

// kate: space-indent off;
