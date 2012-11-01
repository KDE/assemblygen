/***************************************************************************
                          qyoto.cpp  -  description
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

#ifndef _GNU_SOURCE
#define _GNU_SOURCE
#endif
#include <stdio.h>
#include <stdlib.h>

#include <QMetaMethod>
#include <QByteArray>
#include <QHash>
#include <QObject>
#include <QRegExp>
#include <QString>
#include <QStringList>
#include <QEvent>

#undef DEBUG
#ifndef __USE_POSIX
#define __USE_POSIX
#endif
#ifndef __USE_XOPEN
#define __USE_XOPEN
#endif
#ifdef _BOOL
#define HAS_BOOL
#endif                                                          

#ifndef QT_VERSION_STR
#define QT_VERSION_STR "Unknown"
#endif

#include "marshall.h"
#include "qyoto.h"
#include "callbacks.h"
#include "smokeqyoto.h"
#include "smoke.h"

#include "qyotosmokebinding.h"
#include "methodcall.h"
#include "emitsignal.h"

#define QYOTO_VERSION "0.0.1"
// #define DEBUG

#include <qtcore_smoke.h>

void ObjectUnmapper::objectDestroyed()
{
    void* qyotoObj = (*GetInstance)(sender(), true);
    if (qyotoObj) {
        smokeqyoto_object* o = (smokeqyoto_object*) (*GetSmokeObject)(qyotoObj);
        if (o != 0 && o->ptr != 0) {
            unmapPointer(o, o->classId, 0);
            (*SetSmokeObject)(qyotoObj, 0);
            free_smokeqyoto_object(o);
        }
    }
}

extern bool qRegisterResourceData(int, const unsigned char *, const unsigned char *, const unsigned char *);
extern bool qUnregisterResourceData(int, const unsigned char *, const unsigned char *, const unsigned char *);

extern TypeHandler Qyoto_handlers[];

extern bool IsContainedInstanceQtCore(smokeqyoto_object *o);
extern const char * qyoto_resolve_classname_qtcore(smokeqyoto_object * o);

static bool 
qyoto_event_notify(void **data)
{
	// don't do anything if the application has already terminated
	if (application_terminated) return false;
	QObject *receiver = reinterpret_cast<QObject*>(data[0]);
	QEvent *event = reinterpret_cast<QEvent*>(data[1]);

	// If a child has been given a parent then make a global ref to it, to prevent
	// garbage collection. If a parent has been removed, then remove to global ref
	// to the child also.
	if (event->type() == QEvent::ChildAdded || event->type() == QEvent::ChildRemoved) {
		QChildEvent *e = static_cast<QChildEvent *>(event);
		void * childObj = (*GetInstance)(e->child(), true);
		if (childObj != 0) {
			smokeqyoto_object *o = (smokeqyoto_object*) (*GetSmokeObject)(childObj);
			// Maybe add a check whether the childObj is still a QObject here
			if (e->added()) {
				(*AddGlobalRef)(childObj, e->child());
				o->allocated = false;  // we don't need to care about deleting stuff anymore
			} else {
				(*RemoveGlobalRef)(childObj, e->child());
				o->allocated = true;  // now we need to care about deletion again
			}

			(*FreeGCHandle)(childObj);
		}
	} else if (event->type() == QEvent::Show || event->type() == QEvent::Hide) {
		if (!receiver->isWidgetType() || receiver->parent() != 0)
			return false;

		void *obj = (*GetInstance)(receiver, true);
		if (!obj)
			return false;

		if (event->type() == QEvent::Show) {
			(*AddGlobalRef)(obj, receiver);    // Keep top-level widgets alive as long as they're visible.
		} else {
			(*RemoveGlobalRef)(obj, receiver);    // Make them eligible for collection as soon as they're hidden (and if there are no more references to them, obviously).
		}

		(*FreeGCHandle)(obj);
	}

	return false;
}

extern "C" {

Q_DECL_EXPORT Smoke::ModuleIndex 
FindMethodId(const char * classname, const char * mungedname, const char * signature)
{
	static Smoke::ModuleIndex negativeIndex(0, -1);
#ifdef DEBUG
	printf("FindMethodId(classname: %s mungedname: %s signature: %s)\n", classname, mungedname, signature);
	fflush(stdout);
#endif

	Smoke::ModuleIndex meth = qtcore_Smoke->findMethod(classname, mungedname);
#ifdef DEBUG
	if (do_debug & qtdb_calls) printf("DAMNIT on %s::%s => %d\n", classname, mungedname, meth.index);
#endif
	if (meth.index == 0) {
		foreach (Smoke *smoke, qyoto_modules.keys()) {
			meth = smoke->findMethod("QGlobalSpace", mungedname);
			if (meth.index)
				break;
		}
#ifdef DEBUG
		if (do_debug & qtdb_calls) printf("DAMNIT on QGlobalSpace::%s => %d\n", mungedname, meth.index);
#endif
	}
	
    if (meth.index == 0) {
    	return negativeIndex;
		// empty list
	} else if(meth.index > 0) {
		Smoke::Index i = meth.smoke->methodMaps[meth.index].method;
		if (i == 0) {		// shouldn't happen
	    	return negativeIndex;
		} else if (i > 0) {	// single match
	    	Smoke::Method &methodRef = meth.smoke->methods[i];
			if ((methodRef.flags & Smoke::mf_internal) == 0) {
				Smoke::ModuleIndex ret(meth.smoke, i);
				return ret;
			}
		} else {		// multiple match
	    	int ambiguousId = -i;		// turn into ambiguousMethodList index
			while (meth.smoke->ambiguousMethodList[ambiguousId] != 0) {
				Smoke::Method &methodRef = meth.smoke->methods[meth.smoke->ambiguousMethodList[ambiguousId]];
				if ((methodRef.flags & Smoke::mf_internal) == 0) {
static QByteArray * currentSignature = 0;
					if (currentSignature == 0) {
						currentSignature = new QByteArray("");
					}

					signature = strchr(signature, '(');
					*currentSignature = "(";
		
					for (int i = 0; i < methodRef.numArgs; i++) {
						if (i != 0) *currentSignature += ", ";
						*currentSignature += meth.smoke->types[meth.smoke->argumentList[methodRef.args + i]].name;
					}
		
					*currentSignature += ")";
					if (methodRef.flags & Smoke::mf_const) {
						*currentSignature += " const";
					}
		
#ifdef DEBUG
					printf(	"\t\tIn FindAmbiguousMethodId(%d, %s) => %d (%s)\n", 
							ambiguousId,
							signature,
							meth.smoke->ambiguousMethodList[ambiguousId],
							(const char *) *currentSignature );
					fflush(stdout);
#endif
		
					if (*currentSignature == signature) {
						Smoke::ModuleIndex ret(meth.smoke,  meth.smoke->ambiguousMethodList[ambiguousId]);
						return ret;
					}
				}
				ambiguousId++;
			}
		}
	}
	
	return negativeIndex;
}

Q_DECL_EXPORT bool QyotoRegisterResourceData(int flag, const unsigned char * s, const unsigned char *n, const unsigned char *d)
{
	return qRegisterResourceData(flag, s, n, d);
}

Q_DECL_EXPORT bool QyotoUnregisterResourceData(int flag, const unsigned char * s, const unsigned char *n, const unsigned char *d)
{
	return qUnregisterResourceData(flag, s, n, d);
}

Q_DECL_EXPORT int
SizeOfLong()
{
	return sizeof(long);
}

/* 
	Based on this function from QtCore/qhash.h:

	inline uint qHash(ulong key)
	{
		if (sizeof(ulong) > sizeof(uint)) {
			return uint((key >> (8 * sizeof(uint) - 1)) ^ key);
		} else {
			return uint(key);
		}
	}
*/

Q_DECL_EXPORT void
CallSmokeMethod(Smoke * smoke, int methodId, void * obj, Smoke::StackItem * sp, int items, Smoke::TypeId * typeIDs);

typedef QHash<Smoke::ModuleIndex, Smoke::ModuleIndex> qHashCacheType;
Q_GLOBAL_STATIC(qHashCacheType, qHashFunctionCache);

static uint qHash(const Smoke::ModuleIndex& mi)
{
	return qHash(mi.smoke) ^ qHash(mi.index);
}

Q_DECL_EXPORT int 
QyotoHash(void * obj)
{
	smokeqyoto_object *o = (smokeqyoto_object*) (*GetSmokeObject)(obj);
	Smoke::ModuleIndex klassModuleIndex = Smoke::ModuleIndex(o->smoke, o->classId);

	Smoke::ModuleIndex hashIndex = qHashFunctionCache()->value(klassModuleIndex, Smoke::ModuleIndex(0, -1));

	if (hashIndex.index == -1) {
		const char *className = o->smoke->className(o->classId);
		QByteArray signature("qHash(const ");
		signature.append(className).append("&)");
		hashIndex = FindMethodId("QGlobalSpace", "qHash#", signature.constData());
		qHashFunctionCache()->insert(klassModuleIndex, hashIndex);
	}

	if (hashIndex.smoke) {
		Smoke::StackItem stack[2];
		stack[1].s_class = obj;
		Smoke::TypeId typeIDs[1] = { Smoke::t_class };
		CallSmokeMethod(hashIndex.smoke, hashIndex.index, 0, stack, 2, typeIDs);
		return (int) stack[0].s_uint;
	}

	(*FreeGCHandle)(obj);

	if (sizeof(void*) > sizeof(int)) {
		qint64 key = (qint64) o->ptr;
		return (int) ((key >> (8 * sizeof(int) - 1)) ^ key);
	} else {
		return (int) (qint64) o->ptr;
	}
}

Q_DECL_EXPORT void
CallSmokeMethod(Smoke * smoke, int methodId, void * obj, Smoke::StackItem * sp, int items, Smoke::TypeId * typeIDs)
{
	Smoke::Method meth = smoke->methods[methodId];
	const char *methname = smoke->methodNames[meth.name];
#ifdef DEBUG
	printf("ENTER CallSmokeMethod(methodId: %d methodName: %s target: 0x%8.8x class: %s items: %d module: %s)\n", methodId, methname, obj, smoke->className(meth.classId), items, smoke->moduleName());
#endif

	// C# operator methods must be static, and so some C++ instance methods with one argument
	// are mapped onto C# static methods with two arguments in the Qyoto classes. So look for
	// examples of these and changes the args passed to the QyotoMethodCall() constructor. Note
	// that 'operator>>' and 'operator<<' methods in C# must have a second arg of type int,
	// and so they are mapped onto the instance methods Read() and Write() in C#.
	if (	meth.numArgs == 1
			&& qstrncmp("operator", methname, sizeof("operator")) == 0
			&& qstrncmp("operator<<", methname, sizeof("operator<<")) != 0
			&& qstrncmp("operator>>", methname, sizeof("operator>>")) != 0 )
	{ // instance operator
		obj = sp[1].s_class;
		sp[1] = sp[2];
		items = 1;
	} else if (meth.numArgs == 0 && (qstrcmp("operator++", methname) == 0 || qstrcmp("operator--", methname) == 0)) {
		// instance operator++ / operator-- method that maps onto a static C# operator method
		obj = sp[1].s_class;
		items = 0;
	}

	Qyoto::MethodCall c(smoke, methodId, obj, sp, items, typeIDs);
	c.next();

#ifdef DEBUG
	printf("LEAVE CallSmokeMethod()\n");
#endif

	return;
}

Q_DECL_EXPORT bool
SignalEmit(char * signature, char * type, void * obj, Smoke::StackItem * sp, int items)
{
	smokeqyoto_object *o = (smokeqyoto_object*) (*GetSmokeObject)(obj);
	QObject *qobj = (QObject*)o->smoke->cast(o->ptr, o->classId, o->smoke->idClass("QObject").index);

	if (qobj->signalsBlocked()) {
		(*FreeGCHandle)(obj);
		return false;
	}

	QString sig(signature);
	QString replyType(type);

	const QMetaObject* meta = qobj->metaObject();
	int i;
	for (i = 0; i < meta->methodCount(); i++) {
		QMetaMethod m = meta->method(i);
		if (	m.methodType() == QMetaMethod::Signal 
				&& strcmp(m.signature(), signature) == 0 )
		{
			break;
		}
	}

	QList<MocArgument*> args = GetMocArguments(o->smoke, meta->method(i).typeName(), meta->method(i).parameterTypes());
	
	Qyoto::EmitSignal signal(qobj, i, items, args, sp);
	signal.next();

	(*FreeGCHandle)(obj);
	return true;
}

Q_DECL_EXPORT void
Init_qyoto_qtcore()
{
    init_qtcore_Smoke();

    qyoto_install_handlers(Qyoto_handlers);
    QByteArray prefix("QtCore.");

    QHash<int, QByteArray> qtcore_classname;
    for (int i = 1; i <= qtcore_Smoke->numClasses; i++) {
        QByteArray name(qtcore_Smoke->classes[i].className);
        name.replace("::", ".");
        name.prepend(prefix);
        qtcore_classname.insert(i, name);
    }
    static Qyoto::Binding binding = Qyoto::Binding(qtcore_Smoke, qtcore_classname);
    QyotoModule module = { "qyoto_qtcore", qyoto_resolve_classname_qtcore, IsContainedInstanceQtCore, &binding };
    qyoto_modules[qtcore_Smoke] = module;

#if QT_VERSION >= 0x40300
    QInternal::registerCallback(QInternal::EventNotifyCallback, qyoto_event_notify);
#endif
}

}

