/***************************************************************************
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

#ifndef QYOTO_SMOKE_BINDING_H
#define QYOTO_SMOKE_BINDING_H

#include <smoke.h>
#include <QByteArray>
#include <QHash>

struct smokeqyoto_object;

namespace Qyoto {

class Q_DECL_EXPORT Binding : public SmokeBinding {
protected:
	QHash<int, QByteArray> _classname;
public:
	Binding();
	Binding(Smoke *s, const QHash<int, QByteArray>& classname);
	void deleted(Smoke::Index classId, void *ptr);
	bool callMethod(Smoke::Index method, void *ptr, Smoke::Stack args, bool isAbstract);
	virtual bool callMethod(void *obj, smokeqyoto_object *sqo, const QByteArray& signature, Smoke::Stack args, bool isAbstract) {
		Q_UNUSED(obj)
		Q_UNUSED(sqo)
		Q_UNUSED(signature)
		Q_UNUSED(args)
		Q_UNUSED(isAbstract)

		return false;
    }
	char *className(Smoke::Index classId);
};

}

#endif // QYOTO_SMOKE_BINDING_H
