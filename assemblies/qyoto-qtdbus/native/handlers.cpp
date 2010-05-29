/***************************************************************************
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU Lesser General Public License as        *
 *   published by the Free Software Foundation; either version 2 of the    *
 *   License, or (at your option) any later version.                       *
 *                                                                         *
 ***************************************************************************/

#include <QDBusVariant>

#include <smoke.h>

#undef DEBUG
#ifndef _GNU_SOURCE
#define _GNU_SOURCE
#endif
#ifndef __USE_POSIX
#define __USE_POSIX
#endif
#ifndef __USE_XOPEN
#define __USE_XOPEN
#endif

#include <marshall.h>
#include <qyoto.h>
#include <callbacks.h>
#include <smokeqyoto.h>

void marshall_QDBusVariant(Marshall *m) {
	switch(m->action()) {
	case Marshall::FromObject: 
	{
		if (m->var().s_class == 0) {
			m->item().s_class = 0;
			(*FreeGCHandle)(m->var().s_class);
			return;
		}

		smokeqyoto_object *o = (smokeqyoto_object*) (*GetSmokeObject)(m->var().s_class);
		if (!o || !o->ptr) {
			if (m->type().isRef()) {
				m->unsupported();
			}
		    m->item().s_class = 0;
		    break;
		}
		m->item().s_class = o->ptr;
		(*FreeGCHandle)(m->var().s_class);
		break;
	}

	case Marshall::ToObject: 
	{
		if (m->item().s_voidp == 0) {
			m->var().s_voidp = 0;
		    break;
		}

		void *p = m->item().s_voidp;
		void * obj = (*GetInstance)(p, true);
		if(obj != 0) {
			m->var().s_voidp = obj;
		    break;
		}
		
		Smoke::ModuleIndex id = m->smoke()->findClass("QVariant");
		smokeqyoto_object  * o = alloc_smokeqyoto_object(false, id.smoke, id.index, p);

		if((m->type().isConst() && m->type().isRef()) || (m->type().isStack() && m->cleanup())) {
			p = construct_copy( o );
			if (p != 0) {
				o->ptr = p;
				o->allocated = true;
		    }
		}

		obj = (*CreateInstance)("Qyoto.QDBusVariant", o);
		if (do_debug & qtdb_calls) {
			qDebug("allocating %s %p -> %p\n", "QDBusVariant", o->ptr, (void*)obj);
		}

		if (m->type().isStack()) {
		    o->allocated = true;
		}
		// Keep a mapping of the pointer so that it is only wrapped once
		mapPointer(obj, o, o->classId, 0);
		
		m->var().s_class = obj;
	}
	
	default:
		m->unsupported();
		break;
    }
}

Q_DECL_EXPORT TypeHandler Qyoto_qtdbus_handlers[] = {
    { "QDBusVariant", marshall_QDBusVariant },
    { "QDBusVariant&", marshall_QDBusVariant },
    { 0, 0 }
};

// kate: space-indent off; mixed-indent off;
