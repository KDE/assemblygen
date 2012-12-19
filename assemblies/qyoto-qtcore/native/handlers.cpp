/***************************************************************************
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU Lesser General Public License as        *
 *   published by the Free Software Foundation; either version 2 of the    *
 *   License, or (at your option) any later version.                       *
 *                                                                         *
 ***************************************************************************/

#include <QtCore>

#include <stdlib.h>

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

#include "marshall.h"
#include "qyoto.h"
#include "qyoto_p.h"
#include "callbacks.h"
#include "smokeqyoto.h"
#include "marshall_macros.h"

#ifdef Q_OS_WIN
#include <windows.h>
#endif

extern "C" {

Q_DECL_EXPORT void* ConstructPointerList()
{
	void * list = (void*) new QList<void*>;
	return list;
}

Q_DECL_EXPORT void AddObjectToPointerList(void* ptr, void* obj)
{
	QList<void*> * list = (QList<void*>*) ptr;
	list->append(obj);
}

Q_DECL_EXPORT void* ConstructQListInt()
{
	void* list = (void*) new QList<int>;
	return list;
}

Q_DECL_EXPORT void AddIntToQList(void* ptr, int i)
{
	QList<int>* list = (QList<int>*) ptr;
	list->append(i);
}
Q_DECL_EXPORT void* ConstructQHash(int type)
{
	if (type == 0) {
		return (void*) new QHash<int, QVariant>();
	} else if (type == 1) {
		return (void*) new QHash<QString, QString>();
	} else if (type == 2) {
		return (void*) new QHash<QString, QVariant>();
	}
	return 0;
}

Q_DECL_EXPORT void AddIntQVariantToQHash(void* ptr, int i, void* qv)
{
	QHash<int, QVariant>* hash = (QHash<int, QVariant>*) ptr;
	QVariant* variant = (QVariant*) ((smokeqyoto_object*) (*GetSmokeObject)(qv))->ptr;
	hash->insert(i, *variant);
}

Q_DECL_EXPORT void AddQStringQStringToQHash(void* ptr, char* str1, char* str2)
{
	QHash<QString, QString>* hash = (QHash<QString, QString>*) ptr;
	hash->insert(QString(str1), QString(str2));
}

Q_DECL_EXPORT void AddQStringQVariantToQHash(void* ptr, char* str, void* qv)
{
	QHash<QString, QVariant>* hash = (QHash<QString, QVariant>*) ptr;
	QVariant* variant = (QVariant*) ((smokeqyoto_object*) (*GetSmokeObject)(qv))->ptr;
	hash->insert(QString(str), *variant);
}

Q_DECL_EXPORT void* ConstructQMap(int type)
{
	if (type == 0) {
		return (void*) new QMap<int, QVariant>();
	} else if (type == 1) {
		return (void*) new QMap<QString, QString>();
	} else if (type == 2) {
		return (void*) new QMap<QString, QVariant>();
	}
	return 0;
}

Q_DECL_EXPORT void AddIntQVariantToQMap(void* ptr, int i, void* qv)
{
	QMap<int, QVariant>* map = (QMap<int, QVariant>*) ptr;
	QVariant* variant = (QVariant*) ((smokeqyoto_object*) (*GetSmokeObject)(qv))->ptr;
	map->insert(i, *variant);
}

Q_DECL_EXPORT void AddQStringQStringToQMap(void* ptr, char* str1, char* str2)
{
	QMap<QString, QString>* map = (QMap<QString, QString>*) ptr;
	map->insert(QString(str1), QString(str2));
}

Q_DECL_EXPORT void AddQStringQVariantToQMap(void* ptr, char* str, void* qv)
{
	QMap<QString, QVariant>* map = (QMap<QString, QVariant>*) ptr;
	QVariant* variant = (QVariant*) ((smokeqyoto_object*) (*GetSmokeObject)(qv))->ptr;
	map->insert(QString(str), *variant);
}

}

bool
IsContainedInstanceQtCore(smokeqyoto_object *o)
{
    const char *className = o->smoke->classes[o->classId].className;

	if (o->smoke->isDerivedFrom(className, "QObject")) {
		QObject * qobject = (QObject *) o->smoke->cast(o->ptr, o->classId, o->smoke->idClass("QObject", true).index);
		if (qobject->parent() != 0) {
			return true;
		}
	}

    return false;
}

/*
 * Given an approximate classname and a qt instance, try to improve the resolution of the name
 * by using the various Qt rtti mechanisms for QObjects, QEvents and so on
 */
const char *
qyoto_resolve_classname_qtcore(smokeqyoto_object * o)
{
#define SET_SMOKEQYOTO_OBJECT(className) \
    { \
        Smoke::ModuleIndex mi = Smoke::findClass(className); \
        o->classId = mi.index; \
        o->smoke = mi.smoke; \
    }

	if (o->smoke->isDerivedFrom(o->smoke->classes[o->classId].className, "QEvent")) {
		QEvent * qevent = (QEvent *) o->smoke->cast(o->ptr, o->classId, o->smoke->idClass("QEvent", true).index);
		switch (qevent->type()) {
		case QEvent::Timer:
   			SET_SMOKEQYOTO_OBJECT("QTimerEvent")
			break;
		case QEvent::MouseButtonPress:
		case QEvent::MouseButtonRelease:
		case QEvent::MouseButtonDblClick:
		case QEvent::MouseMove:
			SET_SMOKEQYOTO_OBJECT("QMouseEvent")
			break;
		case QEvent::KeyPress:
		case QEvent::KeyRelease:
		case QEvent::ShortcutOverride:
   			SET_SMOKEQYOTO_OBJECT("QKeyEvent")
			break;
		case QEvent::FocusIn:
		case QEvent::FocusOut:
   			SET_SMOKEQYOTO_OBJECT("QFocusEvent")
			break;
		case QEvent::Enter:
		case QEvent::Leave:
			SET_SMOKEQYOTO_OBJECT("QEvent")
			break;
		case QEvent::Paint:
			SET_SMOKEQYOTO_OBJECT("QPaintEvent")
			break;
		case QEvent::Move:
			SET_SMOKEQYOTO_OBJECT("QMoveEvent")
			break;
		case QEvent::Resize:
			SET_SMOKEQYOTO_OBJECT("QResizeEvent")
			break;
		case QEvent::Create:
		case QEvent::Destroy:
			SET_SMOKEQYOTO_OBJECT("QEvent")
			break;
		case QEvent::Show:
			SET_SMOKEQYOTO_OBJECT("QShowEvent")
			break;
		case QEvent::Hide:
			SET_SMOKEQYOTO_OBJECT("QHideEvent")
			break;
		case QEvent::Close:
			SET_SMOKEQYOTO_OBJECT("QCloseEvent")
			break;
		case QEvent::Quit:
		case QEvent::ParentChange:
		case QEvent::ParentAboutToChange:
		case QEvent::ThreadChange:
		case QEvent::WindowActivate:
		case QEvent::WindowDeactivate:
		case QEvent::ShowToParent:
		case QEvent::HideToParent:
			SET_SMOKEQYOTO_OBJECT("QEvent")
			break;
		case QEvent::Wheel:
			SET_SMOKEQYOTO_OBJECT("QWheelEvent")
			break;
		case QEvent::WindowTitleChange:
		case QEvent::WindowIconChange:
		case QEvent::ApplicationWindowIconChange:
		case QEvent::ApplicationFontChange:
		case QEvent::ApplicationLayoutDirectionChange:
		case QEvent::ApplicationPaletteChange:
		case QEvent::PaletteChange:
			SET_SMOKEQYOTO_OBJECT("QEvent")
			break;
		case QEvent::Clipboard:
			SET_SMOKEQYOTO_OBJECT("QClipboardEvent")
			break;
		case QEvent::Speech:
		case QEvent::MetaCall:
		case QEvent::SockAct:
		case QEvent::WinEventAct:
		case QEvent::DeferredDelete:
			SET_SMOKEQYOTO_OBJECT("QEvent")
			break;
		case QEvent::DragEnter:
			SET_SMOKEQYOTO_OBJECT("QDragEnterEvent")
			break;
		case QEvent::DragLeave:
			SET_SMOKEQYOTO_OBJECT("QDragLeaveEvent")
			break;
		case QEvent::DragMove:
			SET_SMOKEQYOTO_OBJECT("QDragMoveEvent")
			break;
		case QEvent::Drop:
			SET_SMOKEQYOTO_OBJECT("QDropEvent")
			break;
		case QEvent::DragResponse:
			SET_SMOKEQYOTO_OBJECT("QDragResponseEvent")
			break;
		case QEvent::ChildAdded:
		case QEvent::ChildRemoved:
		case QEvent::ChildPolished:
			SET_SMOKEQYOTO_OBJECT("QChildEvent")
			break;
		case QEvent::ShowWindowRequest:
		case QEvent::PolishRequest:
		case QEvent::Polish:
		case QEvent::LayoutRequest:
		case QEvent::UpdateRequest:
		case QEvent::EmbeddingControl:
		case QEvent::ActivateControl:
		case QEvent::DeactivateControl:
			SET_SMOKEQYOTO_OBJECT("QEvent")
			break;
        case QEvent::ContextMenu:
			SET_SMOKEQYOTO_OBJECT("QContextMenuEvent")
			break;
  case QEvent::DynamicPropertyChange:
			SET_SMOKEQYOTO_OBJECT("QDynamicPropertyChangeEvent")
			break;
		case QEvent::InputMethod:
			SET_SMOKEQYOTO_OBJECT("QInputMethodEvent")
			break;
		case QEvent::AccessibilityPrepare:
			SET_SMOKEQYOTO_OBJECT("QEvent")
			break;
		case QEvent::TabletMove:
		case QEvent::TabletPress:
		case QEvent::TabletRelease:
			SET_SMOKEQYOTO_OBJECT("QTabletEvent")
			break;
		case QEvent::LocaleChange:
		case QEvent::LanguageChange:
		case QEvent::LayoutDirectionChange:
		case QEvent::Style:
		case QEvent::OkRequest:
		case QEvent::HelpRequest:
			SET_SMOKEQYOTO_OBJECT("QEvent")
			break;
		case QEvent::IconDrag:
			SET_SMOKEQYOTO_OBJECT("QIconDragEvent")
			break;
		case QEvent::FontChange:
		case QEvent::EnabledChange:
		case QEvent::ActivationChange:
		case QEvent::StyleChange:
		case QEvent::IconTextChange:
		case QEvent::ModifiedChange:
		case QEvent::MouseTrackingChange:
			SET_SMOKEQYOTO_OBJECT("QEvent")
			break;
		case QEvent::WindowBlocked:
		case QEvent::WindowUnblocked:
		case QEvent::WindowStateChange:
			SET_SMOKEQYOTO_OBJECT("QWindowStateChangeEvent")
			break;
		case QEvent::ToolTip:
		case QEvent::WhatsThis:
			SET_SMOKEQYOTO_OBJECT("QHelpEvent")
			break;
		case QEvent::StatusTip:
			SET_SMOKEQYOTO_OBJECT("QEvent")
			break;
		case QEvent::ActionChanged:
		case QEvent::ActionAdded:
		case QEvent::ActionRemoved:
			SET_SMOKEQYOTO_OBJECT("QActionEvent")
			break;
		case QEvent::FileOpen:
			SET_SMOKEQYOTO_OBJECT("QFileOpenEvent")
			break;
		case QEvent::Shortcut:
			SET_SMOKEQYOTO_OBJECT("QShortcutEvent")
			break;
		case QEvent::WhatsThisClicked:
			SET_SMOKEQYOTO_OBJECT("QWhatsThisClickedEvent")
			break;
		case QEvent::ToolBarChange:
			SET_SMOKEQYOTO_OBJECT("QToolBarChangeEvent")
			break;
		case QEvent::ApplicationActivated:
		case QEvent::ApplicationDeactivated:
		case QEvent::QueryWhatsThis:
		case QEvent::EnterWhatsThisMode:
		case QEvent::LeaveWhatsThisMode:
		case QEvent::ZOrderChange:
			SET_SMOKEQYOTO_OBJECT("QEvent")
			break;
		case QEvent::HoverEnter:
		case QEvent::HoverLeave:
		case QEvent::HoverMove:
			SET_SMOKEQYOTO_OBJECT("QHoverEvent")
			break;
		case QEvent::AccessibilityHelp:
		case QEvent::AccessibilityDescription:
			SET_SMOKEQYOTO_OBJECT("QEvent")
			break;
#if QT_VERSION >= 0x40200
		case QEvent::GraphicsSceneMouseMove:
		case QEvent::GraphicsSceneMousePress:
		case QEvent::GraphicsSceneMouseRelease:
		case QEvent::GraphicsSceneMouseDoubleClick:
			SET_SMOKEQYOTO_OBJECT("QGraphicsSceneMouseEvent")
			break;
		case QEvent::GraphicsSceneContextMenu:
			SET_SMOKEQYOTO_OBJECT("QGraphicsSceneContextMenuEvent")
			break;
		case QEvent::GraphicsSceneHoverEnter:
		case QEvent::GraphicsSceneHoverMove:
		case QEvent::GraphicsSceneHoverLeave:
			SET_SMOKEQYOTO_OBJECT("QGraphicsSceneHoverEvent")
			break;
		case QEvent::GraphicsSceneHelp:
			SET_SMOKEQYOTO_OBJECT("QGraphicsSceneHelpEvent")
			break;
		case QEvent::GraphicsSceneDragEnter:
		case QEvent::GraphicsSceneDragMove:
		case QEvent::GraphicsSceneDragLeave:
		case QEvent::GraphicsSceneDrop:
			SET_SMOKEQYOTO_OBJECT("QGraphicsSceneDragDropEvent")
			break;
		case QEvent::GraphicsSceneWheel:
			SET_SMOKEQYOTO_OBJECT("QGraphicsSceneWheelEvent")
			break;
		case QEvent::KeyboardLayoutChange:
			SET_SMOKEQYOTO_OBJECT("QEvent")
			break;
#endif
		default:
			break;
		}
	}

#undef SET_SMOKEQYOTO_OBJECT

	return qyoto_modules[o->smoke].binding->className(o->classId);
}

extern "C" {

Q_DECL_EXPORT void *
StringArrayToCharStarStar(int length, char ** strArray)
{
	char ** result = (char **) calloc(length, sizeof(char *));
	int i;
	for (i = 0; i < length; i++) {
		result[i] = strdup(strArray[i]);
	}
	return (void *) result;
}

Q_DECL_EXPORT void *
StringToQString(const ushort *str)
{
	QString * result = new QString(QString::fromUtf16(str));
	return (void *) result;
}

Q_DECL_EXPORT const ushort *
StringFromQString(void *ptr)
{
	QString* str = (QString*) ptr;
	int len = str->length() + 1; // include the terminating \0
#ifdef Q_OS_WIN
	ushort *copy = (ushort*) GlobalAlloc(GMEM_FIXED, sizeof(ushort) * len);
#else
	ushort *copy = (ushort*) malloc(sizeof(ushort) * len);
#endif
	memcpy(copy, str->utf16(), sizeof(ushort) * len);
	// return a copy of the string - the runtime will take ownership of it and care about deletion
	return copy;
}

Q_DECL_EXPORT void *
StringArrayToQStringList(int length, char ** strArray)
{
	QStringList * result = new QStringList();
	
	for (int i = 0; i < length; i++) {
		(*result) << QString::fromUtf16((ushort*) strArray[i]);
	}
	return (void*) result;
}

}

void
marshall_basetype(Marshall *m)
{
    switch(m->type().elem()) {
	
    
      case Smoke::t_bool:
	switch(m->action()) {
	  case Marshall::FromObject:
	    m->item().s_bool = m->var().s_bool;
	    break;
	  case Marshall::ToObject:
	    m->var().s_bool = m->item().s_bool;
	    break;
	  default:
	    m->unsupported();
	    break;
	}
	break;
      case Smoke::t_char:
	switch(m->action()) {
	  case Marshall::FromObject:
	    m->item().s_char = m->var().s_char;
	    break;
	  case Marshall::ToObject:
	    m->var().s_char = m->item().s_char;
	    break;
	  default:
	    m->unsupported();
	    break;
	}
	break;
      case Smoke::t_uchar:
	switch(m->action()) {
	  case Marshall::FromObject:
	    m->item().s_uchar = m->var().s_uchar;
	    break;
	  case Marshall::ToObject:
	    m->var().s_uchar = m->item().s_uchar;
	    break;
	  default:
	    m->unsupported();
	    break;
	}
	break;
      case Smoke::t_short:
	switch(m->action()) {
	  case Marshall::FromObject:
	    m->item().s_short = m->var().s_short;
	    break;
	  case Marshall::ToObject:
	    m->var().s_short = m->item().s_short;
	    break;
	  default:
	    m->unsupported();
	    break;
	}
	break;
      case Smoke::t_ushort:
	switch(m->action()) {
	  case Marshall::FromObject:
	    m->item().s_ushort = m->var().s_ushort;
	    break;
	  case Marshall::ToObject:
	    m->var().s_ushort = m->item().s_ushort;
	    break;
	  default:
	    m->unsupported();
	    break;
	}
	break;
      case Smoke::t_int:
	switch(m->action()) {
	  case Marshall::FromObject:
	    m->item().s_int = m->var().s_int;
	    break;
	  case Marshall::ToObject:
	    m->var().s_int = m->item().s_int;
	    break;
	  default:
	    m->unsupported();
	    break;
	}
	break;
      case Smoke::t_uint:
	switch(m->action()) {
	  case Marshall::FromObject:
	    m->item().s_uint = m->var().s_uint;
	    break;
	  case Marshall::ToObject:
	    m->var().s_uint = m->item().s_uint;
	    break;
	  default:
	    m->unsupported();
	    break;
	}
	break;
      case Smoke::t_long:
	switch(m->action()) {
	  case Marshall::FromObject:
	    m->item().s_long = m->var().s_long;
	    break;
	  case Marshall::ToObject:
	    m->var().s_long = m->item().s_long;
	    break;
	  default:
	    m->unsupported();
	    break;
	}
	break;
      case Smoke::t_ulong:
	switch(m->action()) {
	  case Marshall::FromObject:
	    m->item().s_ulong = m->var().s_ulong;
	    break;
	  case Marshall::ToObject:
	    m->var().s_ulong = m->item().s_ulong;
	    break;
	  default:
	    m->unsupported();
	    break;
	}
	break;
      case Smoke::t_float:
	switch(m->action()) {
	  case Marshall::FromObject:
	    m->item().s_float = m->var().s_float;
	    break;
	  case Marshall::ToObject:
	    m->var().s_float = m->item().s_float;
	    break;
	  default:
	    m->unsupported();
	    break;
	}
	break;
      case Smoke::t_double:
	switch(m->action()) {
	  case Marshall::FromObject:
	    m->item().s_double = m->var().s_double;
	    break;
	  case Marshall::ToObject:
	    m->var().s_double = m->item().s_double;
	    break;
	  default:
	    m->unsupported();
	    break;
	}
	break;
      case Smoke::t_enum:
	switch(m->action()) {
	  case Marshall::FromObject:
	    m->item().s_enum = m->var().s_enum;
	    break;
	  case Marshall::ToObject:
	    m->var().s_enum = m->item().s_enum;
	    break;
	  default:
	    m->unsupported();
	    break;
	}
	break;
	case Smoke::t_class:
	switch(m->action()) {
	case Marshall::FromObject:
	{
		void * obj = m->var().s_voidp;
		if (obj == 0) {
			m->item().s_class = 0;
			return;
		}

		smokeqyoto_object *o = (smokeqyoto_object*) (*GetSmokeObject)(obj);
		if (!o || !o->ptr) {
			if (m->type().isRef()) {
				m->unsupported();
			}
		    m->item().s_class = 0;
		    break;
		}

		void *ptr = o->ptr;
		if (!m->cleanup() && m->type().isStack()) {
		    ptr = construct_copy(o);
		}
		const Smoke::Class &c = m->smoke()->classes[m->type().classId()];
		ptr = o->smoke->cast(
		    ptr,				// pointer
		    o->classId,				// from
		    o->smoke->idClass(c.className, true).index	// to
		);
		m->item().s_class = ptr;
		(*FreeGCHandle)(obj);
		break;
	}
	break;
	case Marshall::ToObject:
	{
		if(m->item().s_voidp == 0) {
			m->var().s_voidp = 0;
		    break;
		}

		void *p = m->item().s_voidp;
		void * obj = (*GetInstance)(p, true);
		if(obj != 0) {
			m->var().s_voidp = obj;
		    break;
		}

		smokeqyoto_object  * o = alloc_smokeqyoto_object(false, m->smoke(), m->type().classId(), p);
		QByteArray className(qyoto_resolve_classname(o));
		const char * classname = className.append(", qyoto-").append(o->smoke->moduleName()).data();

		if((m->type().isConst() && m->type().isRef()) || (m->type().isStack() && m->cleanup())) {
			p = construct_copy( o );
			if (p != 0) {
				o->ptr = p;
				o->allocated = true;
		    }
		}

		obj = (*CreateInstance)(classname, o);
		if (do_debug & qtdb_calls) {
			printf("allocating %s %p -> %p\n", classname, o->ptr, (void*)obj);
		}

		if(m->type().isStack()) {
		    o->allocated = true;
		}
		// Keep a mapping of the pointer so that it is only wrapped once
        if (m->shouldMapPointer()) {
            if (o->smoke->isDerivedFrom(o->smoke->className(o->classId), "QObject")) {
                QObject::connect((QObject*) p, SIGNAL(destroyed()), &objectUnmapper, SLOT(objectDestroyed()));
            }
            mapPointer(obj, o, o->classId, 0);
		}
		
		m->var().s_class = obj;
	}
	break;
	default:
		m->unsupported();
		break;
	}
	break;
      default:
	m->unsupported();
	break;
    }

}

static void marshall_void(Marshall * /*m*/) {}
static void marshall_unknown(Marshall *m) {
    m->unsupported();
}

// In C#, we store the 'long long' directly in the Smoke::StackItem. Therefore, treat
// the Smoke::StackItem as the value itself, even though there's no 'long long' field
// defined in Smoke::StackItem. The union is 64 bits on most platforms anyway, because
// of the s_double field.
static void marshall_int64(Marshall *m) {
	switch(m->action()) {
	case Marshall::FromObject:
	{
		qint64 *copy = new qint64(*reinterpret_cast<qint64*>(&m->var()));
		m->item().s_voidp = copy;

		m->next();

		if (m->cleanup()) {
			delete copy;
		}
	}
	break;

	case Marshall::ToObject:
	{
		qint64 *ptr = static_cast<qint64*>(m->item().s_voidp);
		*reinterpret_cast<qint64*>(&m->var()) = *ptr;

		if (m->type().isStack()) {
			delete ptr;
		}
	}
	break;

	default:
		m->unsupported();
		break;
	}
}

static void marshall_uint64(Marshall *m) {
	switch(m->action()) {
	case Marshall::FromObject:
	{
		quint64 *copy = new quint64(*reinterpret_cast<quint64*>(&m->var()));
		m->item().s_voidp = copy;

		m->next();

		if (m->cleanup()) {
			delete copy;
		}
	}
	break;

	case Marshall::ToObject:
	{
		quint64 *ptr = static_cast<quint64*>(m->item().s_voidp);
		*reinterpret_cast<quint64*>(&m->var()) = *ptr;

		if (m->type().isStack()) {
			delete ptr;
		}
	}
	break;

	default:
		m->unsupported();
		break;
	}
}

static void marshall_charP(Marshall *m) {
	switch(m->action()) {
	case Marshall::FromObject:
	{
		if (!m->type().isConst()) {
			m->item().s_voidp = (*GenericPointerGetIntPtr)(m->var().s_class);
            (*FreeString)(m->var().s_voidp);
			return;
		}
		if (m->var().s_class == 0) {
			m->item().s_voidp = 0;
		} else {
			m->item().s_voidp = (*IntPtrToCharStar)(m->var().s_class);
            (*FreeString)(m->var().s_voidp);
		}
	}
	break;

	case Marshall::ToObject:
	{
		char *p = (char*) m->item().s_voidp;
		if (!m->type().isConst()) {
			m->var().s_class = (*CreateGenericPointer)("System.SByte", p);
			return;
		}
	    if (p != 0) {
			m->var().s_class = (*IntPtrFromCharStar)(p);
	    } else {
			m->var().s_class = 0;
		}
	}
	break;

	default:
		m->unsupported();
		break;
	}
}

static void marshall_ucharP(Marshall *m) {
	switch(m->action()) {
	case Marshall::FromObject:
	{
		m->item().s_voidp = (*GenericPointerGetIntPtr)(m->var().s_class);
        (*FreeString)(m->var().s_voidp);
	}
	break;

	case Marshall::ToObject:
	{
		uchar *p = (uchar*) m->item().s_voidp;
		m->var().s_class = (*CreateGenericPointer)("System.Byte", p);
	}
	break;

	default:
		m->unsupported();
		break;
	}
}

static void marshall_QString(Marshall *m) {
	switch(m->action()) {
	case Marshall::FromObject:
	{
		void* s = StringToQString((ushort*) m->var().s_voidp);
		m->item().s_voidp = s;
	    m->next();
		
//		if (!m->type().isConst() && m->var().s_voidp != 0 && s != 0) {
//			(*StringBuilderFromQString)(m->var().s_voidp, (const char *) s->toUtf8());
//		}
	    
		if (s && m->cleanup()) {
			delete (QString*) s;
		}
	}
	break;
	case Marshall::ToObject:
	{
	    QString *s = (QString*)m->item().s_voidp;
	    if (s) {
			if (s->isNull()) {
				m->var().s_voidp = 0;
			} else {
				m->var().s_class = (void*) StringFromQString(m->item().s_voidp);
			}

			if (m->type().isStack())
				delete s;
			} else {
				m->var().s_voidp = 0;
			}
	}
	break;
		default:
		m->unsupported();
	break;
    }
}

static void marshall_intR(Marshall *m) {
	switch(m->action()) {
	case Marshall::FromObject:
	{
		m->item().s_voidp = &(m->var().s_int);
	}
	break;

	case Marshall::ToObject:
	{
		int *ip = (int*)m->item().s_voidp;
		m->var().s_int = *ip;
	}
	break;

	default:
		m->unsupported();
		break;
	}
}

static void marshall_uintR(Marshall *m) {
	switch(m->action()) {
	case Marshall::FromObject:
	{
		m->item().s_voidp = &(m->var().s_uint);
	}
	break;

	case Marshall::ToObject:
	{
		uint *ip = (uint*)m->item().s_voidp;
		m->var().s_uint = *ip;
	}
	break;

	default:
		m->unsupported();
		break;
	}
}

static void marshall_longR(Marshall *m) {
	switch(m->action()) {
	case Marshall::FromObject:
	{
		m->item().s_voidp = &(m->var().s_long);
	}
	break;

	case Marshall::ToObject:
	{
		long *ip = (long*)m->item().s_voidp;
		m->var().s_long = *ip;
	}
	break;

	default:
		m->unsupported();
		break;
	}
}

static void marshall_ulongR(Marshall *m) {
	switch(m->action()) {
	case Marshall::FromObject:
	{
		m->item().s_voidp = &(m->var().s_ulong);
	}
	break;

	case Marshall::ToObject:
	{
		unsigned long *ip = (unsigned long*)m->item().s_voidp;
		m->var().s_ulong = *ip;
	}
	break;

	default:
		m->unsupported();
		break;
	}
}

static void marshall_shortR(Marshall *m) {
	switch(m->action()) {
	case Marshall::FromObject:
	{
		m->item().s_voidp = &(m->var().s_short);
	}
	break;

	case Marshall::ToObject:
	{
		short *ip = (short*)m->item().s_voidp;
		m->var().s_short = *ip;
	}
	break;

	default:
		m->unsupported();
		break;
	}
}

static void marshall_ushortR(Marshall *m) {
	switch(m->action()) {
	case Marshall::FromObject:
	{
		m->item().s_voidp = &(m->var().s_ushort);
	}
	break;

	case Marshall::ToObject:
	{
		unsigned short *ip = (unsigned short*)m->item().s_voidp;
		m->var().s_ushort = *ip;
	}
	break;

	default:
		m->unsupported();
		break;
	}
}

static void marshall_floatR(Marshall *m) {
    switch(m->action()) {
	case Marshall::FromObject:
	{
		m->item().s_voidp = &(m->var().s_float);
	}
	break;

	case Marshall::ToObject:
	{
		float *dp = (float*)m->item().s_voidp;
		m->var().s_float = *dp;
	}
	break;

	default:
		m->unsupported();
		break;
	}
}

static void marshall_doubleR(Marshall *m) {
    switch(m->action()) {
	case Marshall::FromObject:
	{
		m->item().s_voidp = &(m->var().s_double);
	}
	break;

	case Marshall::ToObject:
	{
		double *dp = (double*)m->item().s_voidp;
		m->var().s_double = *dp;
	}
	break;

	default:
		m->unsupported();
		break;
	}
}

static void marshall_boolR(Marshall *m) {
    switch(m->action()) {
	case Marshall::FromObject:
	{
		m->item().s_voidp = &(m->var().s_bool);
	}
	break;

	case Marshall::ToObject:
	{
		bool *dp = (bool*)m->item().s_voidp;
		m->var().s_bool = *dp;
	}
	break;

	default:
		m->unsupported();
		break;
	}
}

static void marshall_charP_array(Marshall *m) {

    switch(m->action()) {
        case Marshall::FromObject:
            {
            m->item().s_voidp = (*IntPtrToCharStarStar)(m->var().s_voidp);
            }
            break;

        default:
            m->unsupported();
            break;
    }

}

static void marshall_function_pointer(Marshall *m) {
    switch (m->action()) {
        case Marshall::FromObject:
            m->item().s_voidp = m->var().s_voidp;
            break;
        case Marshall::ToObject:
            m->var().s_voidp = m->item().s_voidp;
            break;
    }
}

static void marshall_QVariant(Marshall *m) {
	QVariant* variant;
    switch (m->action()) {
        case Marshall::FromObject:
			switch (m->typeID()) {
				case Smoke::t_bool:
                    variant = new QVariant(m->var().s_bool);
                    break;
                case Smoke::t_char:
                    variant = new QVariant(m->var().s_char);
                    break;
                case Smoke::t_double:
                    variant = new QVariant(m->var().s_double);
                    break;
                case Smoke::t_enum:
                    variant = new QVariant((qlonglong) m->var().s_enum);
                    break;
                case Smoke::t_float:
                    variant = new QVariant(m->var().s_float);
                    break;
				case Smoke::t_int:
					variant = new QVariant(m->var().s_int);
                    break;
                case Smoke::t_long:
                    variant = new QVariant((qlonglong) m->var().s_long);
                    break;
                case Smoke::t_short:
                    variant = new QVariant(m->var().s_short);
                    break;
                case Smoke::t_uchar:
                    variant = new QVariant(m->var().s_uchar);
                    break;
                case Smoke::t_uint:
                    variant = new QVariant(m->var().s_uint);
                    break;
                case Smoke::t_ulong:
                    variant = new QVariant((qulonglong) m->var().s_ulong);
                    break;
                case Smoke::t_ushort:
                    variant = new QVariant(m->var().s_ushort);
					break;
				case 15: // string, added to TypeId in Qyoto only
				{
					QString* string = (QString*) StringToQString((ushort*) m->var().s_voidp);
					variant = new QVariant(*string);
					delete string;
					break;
				}
				default:
					if (m->var().s_voidp == 0) {
						variant = new QVariant();
					} else {
						variant = new QVariant(QMetaType::type("System.Object"), m->var().s_voidp);
					}
					break;
			}
			m->item().s_voidp = variant;
			m->next();
			if (m->cleanup() && variant) {
				delete variant;
			}
            break;
		case Marshall::ToObject:
			variant = (QVariant*) m->item().s_voidp;
			switch (variant->type()) {
				case QVariant::Bool:
					m->var().s_bool = variant->value<bool>();
					m->setTypeID(Smoke::t_bool);
					break;
				case QVariant::Char:
					m->var().s_char = variant->value<char>();
					m->setTypeID(Smoke::t_char);
					break;
				case QVariant::Double:
					m->var().s_double = variant->value<double>();
					m->setTypeID(Smoke::t_double);
					break;
				case QVariant::Int:
					m->var().s_int = variant->value<int>();
					m->setTypeID(Smoke::t_int);
					break;
				case QVariant::LongLong:
					m->var().s_long = variant->value<qlonglong>();
					m->setTypeID(Smoke::t_long);
					break;
				case QVariant::UInt:
					m->var().s_uint = variant->value<uint>();
					m->setTypeID(Smoke::t_uint);
					break;
				case QVariant::ULongLong:
					m->var().s_ulong = variant->value<qulonglong>();
					m->setTypeID(Smoke::t_ulong);
					break;
				case QMetaType::Float:
					m->var().s_float = variant->value<float>();
					m->setTypeID(Smoke::t_float);
					break;
				case QVariant::String:
				{
					QString* s = new QString(variant->value<QString>());
					m->var().s_voidp = (void*) StringFromQString((void*) s);
					delete s;
					m->setTypeID((Smoke::TypeId) 15);
					break;
				}
				case QVariant::Invalid:
					m->var().s_voidp = 0;
					m->setTypeID(Smoke::t_voidp);
					break;
				default:
					if (variant->data() == 0) {
						m->var().s_voidp = 0;
					} else {
						m->var().s_voidp = variant->data();
					}
					m->setTypeID(Smoke::t_voidp);
					break;
			}
            break;
    }
}

void marshall_QMapintQVariant(Marshall *m) {
	switch(m->action()) {
		case Marshall::FromObject: 
		{
			if (m->var().s_class == 0) {
				m->item().s_class = 0;
				return;
			}
			QMap<int, QVariant>* map = (QMap<int, QVariant>*) (*DictionaryToQMap)(m->var().s_voidp, 0);
			m->item().s_voidp = (void*) map;
			m->next();
			
			if (m->cleanup()) {
				delete map;
			}
			(*FreeGCHandle)(m->var().s_voidp);
			break;
		}

		case Marshall::ToObject: 
		{
			QMap<int, QVariant>* map = (QMap<int, QVariant>*) m->item().s_voidp;
			void* dict = (*ConstructDictionary)("System.Int32", "Qyoto.QVariant");
			
			Smoke::ModuleIndex id = m->smoke()->findClass("QVariant");
			
			for (QMap<int, QVariant>::iterator i = map->begin(); i != map->end(); ++i) {
				void* v = (void*) &(i.value());
				smokeqyoto_object * vo = alloc_smokeqyoto_object(false, id.smoke, id.index, v);
                void* value = (*CreateInstance)("QtCore.QVariant", vo);
				(*AddIntObjectToDictionary)(dict, i.key(), value);
				(*FreeGCHandle)(value);
			}
			
			m->var().s_voidp = dict;
			m->next();

			if (m->type().isStack()) {
				delete map;
			}
			
			break;
		}
	
		default:
			m->unsupported();
			break;
    }
}

void marshall_QMapQStringQString(Marshall *m) {
	switch(m->action()) {
		case Marshall::FromObject: 
		{
			if (m->var().s_class == 0) {
				m->item().s_class = 0;
				return;
			}
			QMap<QString, QString>* map = (QMap<QString, QString>*) (*DictionaryToQMap)(m->var().s_voidp, 1);
			m->item().s_voidp = (void*) map;
			m->next();
			
			if (m->cleanup()) {
				delete map;
			}
			(*FreeGCHandle)(m->var().s_voidp);
			break;
		}

		case Marshall::ToObject: 
		{
			QMap<QString, QString>* map = (QMap<QString, QString>*) m->item().s_voidp;
			void* dict = (*ConstructDictionary)("System.String", "System.String");
			
			for (QMap<QString, QString>::iterator i = map->begin(); i != map->end(); ++i) {
				void* string1 = (void*) (*IntPtrFromQString)((void*) &(i.key()));
				void* string2 = (void*) (*IntPtrFromQString)((void*) &(i.value()));
				(*AddObjectObjectToDictionary)(	dict,
								string1,
								string2);
				(*FreeGCHandle)(string1);
				(*FreeGCHandle)(string2);
			}
			
			m->var().s_voidp = dict;
			m->next();

			if (m->type().isStack()) {
				delete map;
			}

			
			break;
		}
	
		default:
			m->unsupported();
			break;
    }
}

void marshall_QMapQStringQVariant(Marshall *m) {
	switch(m->action()) {
		case Marshall::FromObject: 
		{
			if (m->var().s_class == 0) {
				m->item().s_class = 0;
				return;
			}
			QMap<QString, QVariant>* map = (QMap<QString, QVariant>*) (*DictionaryToQMap)(m->var().s_voidp, 2);
			m->item().s_voidp = (void*) map;
			m->next();
			
			if (m->cleanup()) {
				delete map;
			}
			(*FreeGCHandle)(m->var().s_voidp);
			break;
		}

		case Marshall::ToObject: 
		{
			QMap<QString, QVariant>* map = (QMap<QString, QVariant>*) m->item().s_voidp;
			void* dict = (*ConstructDictionary)("System.String", "Qyoto.QVariant");
			
			Smoke::ModuleIndex id = m->smoke()->findClass("QVariant");
			
			for (QMap<QString, QVariant>::iterator i = map->begin(); i != map->end(); ++i) {
				void* v = new QVariant(i.value());
				smokeqyoto_object * vo = alloc_smokeqyoto_object(false, id.smoke, id.index, v);
                void* value = (*CreateInstance)("QtCore.QVariant", vo);
				void* string = (void*) (*IntPtrFromQString)((void*) &(i.key()));
				(*AddObjectObjectToDictionary)(	dict,
								string,
								value);
				(*FreeGCHandle)(string);
				(*FreeGCHandle)(value);
			}
			
			m->var().s_voidp = dict;
			m->next();

			if (m->type().isStack()) {
				delete map;
			}

			
			break;
		}
	
		default:
			m->unsupported();
			break;
    }
}

void marshall_QStringList(Marshall *m) {
	switch(m->action()) {
		case Marshall::FromObject: 
		{
			if (m->var().s_class == 0) {
				m->item().s_class = 0;
				return;
			}
			QStringList *stringlist = (QStringList*) (*StringListToQStringList)(m->var().s_voidp);
			
			m->item().s_voidp = (void*) stringlist;
			m->next();
			
			if (m->cleanup()) {
				delete stringlist;
			}
			(*FreeGCHandle)(m->var().s_voidp);
	   
			break;
		}

      case Marshall::ToObject: 
	{
		QStringList *stringlist = static_cast<QStringList *>(m->item().s_voidp);
		if (!stringlist) {
// 			m->var().s_voidp = 0;
			break;
		}

		void* al = (*ConstructList)("System.String");
        for (int i = 0; i < stringlist->count(); i++) {
            (*AddStringToList)(al, (void*) StringFromQString((void*) &(*stringlist)[i]));
		}
		m->var().s_voidp = al;
		m->next();

		if (m->type().isStack()) {
			delete stringlist;
		}

	}
	break;
      default:
	m->unsupported();
	break;
    }
}

void marshall_QListInt(Marshall *m) {
    switch(m->action()) {
      case Marshall::FromObject:
	{
	    if (m->var().s_class == 0) {
		m->item().s_class = 0;
		return;
	    }
	    void* list = m->var().s_voidp;
	    void* valuelist = (*ListIntToQListInt)(list);
	    m->item().s_voidp = valuelist;
	    m->next();

	    (*FreeGCHandle)(m->var().s_voidp);
	}
	break;
      case Marshall::ToObject:
	{
	    QList<int> *valuelist = (QList<int>*)m->item().s_voidp;
	    if(!valuelist) {
		m->var().s_voidp = 0;
		break;
	    }

	    void* av = (*ConstructList)("System.Int32");

		for (QList<int>::iterator i = valuelist->begin(); i != valuelist->end(); ++i )
		{
		    (*AddIntToListInt)(av, (int) *i);
		}
		
	    m->var().s_voidp = av;
		m->next();

		if (m->type().isStack()) {
			delete valuelist;
		}
	}
	break;
      default:
	m->unsupported();
	break;
    }
}

void marshall_QListConstCharP(Marshall *m) {
	switch (m->action()) {
    case Marshall::FromObject:
	{
		m->unsupported();
	}
	break;
	case Marshall::ToObject:
	{
		QList<const char*> *list = static_cast<QList<const char*>*>(m->item().s_voidp);
		void* al = (*ConstructList)("System.String");
		for (int i = 0; i < list->size(); i++) {
			(*AddIntPtrToList)(al, (*IntPtrFromCharStar)(const_cast<char*>(list->at(i))));
		}
		m->var().s_voidp = al;
		m->next();
		if (m->type().isStack()) {
			delete list;
		}
	}
	break;
	default:
		m->unsupported();
		break;
	}
}


DEF_LIST_MARSHALLER( QObjectList, QList<QObject*>, QObject )

DEF_LIST_MARSHALLER( QByteArrayList, QList<QByteArray*>, QByteArray )
DEF_LIST_MARSHALLER( QFileInfoList, QList<QFileInfo*>, QFileInfo )
DEF_LIST_MARSHALLER( QLineFVector, QVector<QLineF*>, QLineF )
DEF_LIST_MARSHALLER( QLineVector, QVector<QLine*>, QLine )
DEF_LIST_MARSHALLER( QPointFVector, QVector<QPointF*>, QPointF )
DEF_LIST_MARSHALLER( QPointVector, QVector<QPoint*>, QPoint )
DEF_LIST_MARSHALLER( QRectFList, QList<QRectF*>, QRectF )
DEF_LIST_MARSHALLER( QRectFVector, QVector<QRectF*>, QRectF )
DEF_LIST_MARSHALLER( QRectVector, QVector<QRect*>, QRect )
DEF_LIST_MARSHALLER( QUrlList, QList<QUrl*>, QUrl )
DEF_LIST_MARSHALLER( QVariantList, QList<QVariant*>, QVariant )
DEF_LIST_MARSHALLER( QVariantVector, QVector<QVariant*>, QVariant )

Q_DECL_EXPORT TypeHandler Qyoto_handlers[] = {
    { "bool*", marshall_boolR },
    { "bool&", marshall_boolR },
    { "char*", marshall_charP },
    { "char**", marshall_charP_array },
    { "double*", marshall_doubleR },
    { "double&", marshall_doubleR },
    { "float*", marshall_floatR },
    { "float&", marshall_floatR },
    { "int*", marshall_intR },
    { "int&", marshall_intR },
    { "long*", marshall_longR },
    { "long&", marshall_longR },
    { "long long", marshall_int64 },
    { "long long&", marshall_int64 },
    { "long long*", marshall_int64 },
    { "long long int", marshall_int64 },
    { "long long int&", marshall_int64 },
    { "long long int*", marshall_int64 },
    { "qint64", marshall_int64 },
    { "qint64&", marshall_int64 },
    { "qint64*", marshall_int64 },
    { "qlonglong", marshall_int64 },
    { "qlonglong&", marshall_int64 },
    { "qlonglong*", marshall_int64 },
    { "quint64", marshall_uint64 },
    { "quint64&", marshall_uint64 },
    { "quint64*", marshall_uint64 },
    { "qulonglong", marshall_uint64 },
    { "qulonglong&", marshall_uint64 },
    { "qulonglong*", marshall_uint64 },
    { "QFileInfoList", marshall_QFileInfoList },
    { "QList<const char*>", marshall_QListConstCharP },
    { "QList<const char*>&", marshall_QListConstCharP },
    { "QList<int>", marshall_QListInt },
    { "QList<int>&", marshall_QListInt },
    { "QList<QByteArray>", marshall_QByteArrayList },
    { "QList<QByteArray>*", marshall_QByteArrayList },
    { "QList<QByteArray>&", marshall_QByteArrayList },
    { "QList<QObject*>", marshall_QObjectList },
    { "QList<QObject*>&", marshall_QObjectList },
    { "QList<QRectF>", marshall_QRectFList },
    { "QList<QRectF>&", marshall_QRectFList },
    { "QList<QUrl>", marshall_QUrlList },
    { "QList<QUrl>&", marshall_QUrlList },
    { "QList<QVariant>", marshall_QVariantList },
    { "QList<QVariant>&", marshall_QVariantList },
    { "QMap<int,QVariant>", marshall_QMapintQVariant },
    { "QMap<QString,QString>", marshall_QMapQStringQString },
    { "QMap<QString,QString>&", marshall_QMapQStringQString },
    { "QMap<QString,QVariant>", marshall_QMapQStringQVariant },
    { "QMap<QString,QVariant>&", marshall_QMapQStringQVariant },
    { "QVariantMap", marshall_QMapQStringQVariant },
    { "QVariantMap&", marshall_QMapQStringQVariant },
    { "QObjectList", marshall_QObjectList },
    { "QObjectList&", marshall_QObjectList },
    { "qreal*", marshall_doubleR },
    { "qreal&", marshall_doubleR },
    { "QStringList", marshall_QStringList },
    { "QStringList*", marshall_QStringList },
    { "QStringList&", marshall_QStringList },
    { "QString", marshall_QString },
    { "QString*", marshall_QString },
    { "QString&", marshall_QString },
	{ "QVariant", marshall_QVariant },
	{ "QVariant&", marshall_QVariant },
    { "QVariantList&", marshall_QVariantList },
    { "QVector<QLineF>", marshall_QLineFVector },
    { "QVector<QLineF>&", marshall_QLineFVector },
    { "QVector<QLine>", marshall_QLineVector },
    { "QVector<QLine>&", marshall_QLineVector },
    { "QVector<QPointF>", marshall_QPointFVector },
    { "QVector<QPointF>&", marshall_QPointFVector },
    { "QVector<QPoint>", marshall_QPointVector },
    { "QVector<QPoint>&", marshall_QPointVector },
    { "QVector<QRectF>", marshall_QRectFVector },
    { "QVector<QRectF>&", marshall_QRectFVector },
    { "QVector<QRect>", marshall_QRectVector },
    { "QVector<QRect>&", marshall_QRectVector },
    { "QVector<QVariant>", marshall_QVariantVector },
    { "QVector<QVariant>&", marshall_QVariantVector },
    { "short*", marshall_shortR },
    { "short&", marshall_shortR },
    { "signed int*", marshall_intR },
    { "sigend int&", marshall_intR },
    { "signed long*", marshall_longR },
    { "signed long&", marshall_longR },
    { "uchar*", marshall_ucharP},
    { "uint*", marshall_uintR },
    { "uint&", marshall_uintR },
    { "unsigned char*", marshall_ucharP},
    { "unsigned int*", marshall_uintR },
    { "unsigned int&", marshall_uintR },
    { "unsigned long*", marshall_ulongR },
    { "unsigned long&", marshall_ulongR },
    { "unsigned long long", marshall_uint64 },
    { "unsigned long long&", marshall_uint64 },
    { "unsigned long long*", marshall_uint64 },
    { "unsigned long long int", marshall_uint64 },
    { "unsigned long long int&", marshall_uint64 },
    { "unsigned long long int*", marshall_uint64 },
    { "unsigned short*", marshall_ushortR },
    { "unsigned short&", marshall_ushortR },
    { 0, 0 }
};

QHash<QString,TypeHandler *> qyoto_type_handlers;

void qyoto_install_handlers(TypeHandler *h) {
	while(h->name) {
		qyoto_type_handlers.insert(h->name, h);
		h++;
	}
}

Marshall::HandlerFn getMarshallFn(const SmokeType &type) {
	if (!type.name())
		return marshall_void;
	TypeHandler *h = qyoto_type_handlers[type.name()];
	if (h == 0 && type.isConst() && strlen(type.name()) > strlen("const ")) {
    	h = qyoto_type_handlers[type.name() + strlen("const ")];
	}
	
	if(h != 0) {
		return h->fn;
	}
	if (type.elem())
		return marshall_basetype;

    QRegExp regexFunction("^[^(]+\\(\\*\\)\\([^)]*\\)$");
    int pos = regexFunction.indexIn(type.name());
    if (pos > -1) {
        return marshall_function_pointer;
    }

	return marshall_unknown;
}

// kate: space-indent off; mixed-indent off;
