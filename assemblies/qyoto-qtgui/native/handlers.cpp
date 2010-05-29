/***************************************************************************
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU Lesser General Public License as        *
 *   published by the Free Software Foundation; either version 2 of the    *
 *   License, or (at your option) any later version.                       *
 *                                                                         *
 ***************************************************************************/

#include <QtCore/qbytearray.h>
#include <QtCore/qdir.h>
#include <QtCore/qhash.h>
#include <QtCore/qlinkedlist.h>
#include <QtCore/qmetaobject.h>
#include <QtCore/qobject.h>
#include <QtCore/qpair.h>
#include <QtCore/qprocess.h>
#include <QtCore/qrect.h>
#include <QtCore/qregexp.h>
#include <QtCore/qstring.h>
#include <QtCore/qtextcodec.h>
#include <QtCore/qurl.h>
#include <QtGui/qabstractbutton.h>
#include <QtGui/qaction.h>
#include <QtGui/qapplication.h>
#include <QtGui/qdockwidget.h>
#include <QtGui/qevent.h>
#include <QtGui/qlayout.h>
#include <QtGui/qlistwidget.h>
#include <QtGui/qpainter.h>
#include <QtGui/qpalette.h>
#include <QtGui/qpixmap.h>
#include <QtGui/qpolygon.h>
#include <QtGui/qtabbar.h>
#include <QtGui/qtablewidget.h>
#include <QtGui/qtextlayout.h>
#include <QtGui/qtextobject.h>
#include <QtGui/qtoolbar.h>
#include <QtGui/qtreewidget.h>
#include <QtGui/qwidget.h>

#if QT_VERSION >= 0x40200
#include <QtGui/qgraphicsitem.h>
#include <QtGui/qgraphicsscene.h>
#include <QtGui/qstandarditemmodel.h>
#include <QtGui/qundostack.h>
#endif

#if QT_VERSION >= 0x40300
#include <QtGui/qwizard.h>
#include <QtGui/qmdisubwindow.h>
#endif

#if QT_VERSION >= 0x040400
#include <QtGui/qprinterinfo.h>
#endif

#include "smoke.h"

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
#include <qyoto_p.h>
#include <callbacks.h>
#include <smokeqyoto.h>
#include <marshall_macros.h>

#ifdef Q_OS_WIN
#include <windows.h>
#endif

extern "C" {

#if QT_VERSION >= 0x40300
Q_DECL_EXPORT void* ConstructQListWizardButton()
{
	return (void*) new QList<QWizard::WizardButton>();
}

Q_DECL_EXPORT void AddWizardButtonToQList(void* ptr, int i)
{
	QList<QWizard::WizardButton>* list = (QList<QWizard::WizardButton>*) ptr;
	list->append((QWizard::WizardButton) i);
}
#endif

}

bool
IsContainedInstanceQtGui(smokeqyoto_object *o)
{
    const char *className = o->smoke->classes[o->classId].className;
		
	if (	qstrcmp(className, "QListBoxItem") == 0
			|| qstrcmp(className, "QStyleSheetItem") == 0
			|| qstrcmp(className, "QSqlCursor") == 0
			|| qstrcmp(className, "QModelIndex") == 0 )
	{
		return true;
	} else if (o->smoke->isDerivedFrom(className, "QLayoutItem")) {
		QLayoutItem * item = (QLayoutItem *) o->smoke->cast(o->ptr, o->classId, o->smoke->idClass("QLayoutItem", true).index);
		if (item->layout() != 0 || item->widget() != 0 || item->spacerItem() != 0) {
			return true;
		}
	} else if (qstrcmp(className, "QListWidgetItem") == 0) {
		QListWidgetItem * item = (QListWidgetItem *) o->ptr;
		if (item->listWidget() != 0) {
			return true;
		}
	} else if (o->smoke->isDerivedFrom(className, "QTableWidgetItem")) {
		QTableWidgetItem * item = (QTableWidgetItem *) o->smoke->cast(o->ptr, o->classId, o->smoke->idClass("QTableWidgetItem", true).index);
		if (item->tableWidget() != 0) {
			return true;
		}
	} else if (o->smoke->isDerivedFrom(className, "QTreeWidgetItem")) {
		QTreeWidgetItem * item = (QTreeWidgetItem *) o->smoke->cast(o->ptr, o->classId, o->smoke->idClass("QTreeWidgetItem", true).index);
		if (item->treeWidget() != 0) {
			return true;
		}
	} else if (o->smoke->isDerivedFrom(className, "QGraphicsScene")) {
		QGraphicsScene * scene = (QGraphicsScene *) o->smoke->cast(o->ptr, o->classId, o->smoke->idClass("QGraphicsScene", true).index);
		if (scene->views().count() > 0 || scene->parent() != 0) {
			return true;
		}
	} else if (o->smoke->isDerivedFrom(className, "QWidget")) {
		// Only garbage collect the widget if it's hidden, doesn't have any parents and if there are no more 
		// references to it in the code. This should produce a more 'natural' behaviour for top-level widgets.
		QWidget * qwidget = (QWidget *) o->smoke->cast(o->ptr, o->classId, o->smoke->idClass("QWidget", true).index);
		if (qwidget->isVisible() || qwidget->parent() != 0) {
			return true;
		}
	} else if (o->smoke->isDerivedFrom(className, "QTextBlockUserData")) {
		return true;
	} else if (o->smoke->isDerivedFrom(className, "QGraphicsItem")) {
		QGraphicsItem * item = (QGraphicsItem *) o->smoke->cast(o->ptr, o->classId, o->smoke->idClass("QGraphicsItem", true).index);
		if (item->scene() != 0 || item->parentItem() != 0) {
			return true;
		}
	}
	
    return false;
}

/*
 * Given an approximate classname and a qt instance, try to improve the resolution of the name
 * by using the various Qt rtti mechanisms for QObjects, QEvents and so on
 */
Q_DECL_EXPORT const char *
qyoto_resolve_classname_qtgui(smokeqyoto_object * o)
{
#define SET_SMOKEQYOTO_OBJECT(className) \
    { \
        Smoke::ModuleIndex mi = Smoke::findClass(className); \
        o->classId = mi.index; \
        o->smoke = mi.smoke; \
    }

	if (o->smoke->isDerivedFrom(o->smoke->classes[o->classId].className, "QGraphicsItem")) {
		QGraphicsItem * item = (QGraphicsItem *) o->smoke->cast(o->ptr, o->classId, o->smoke->idClass("QGraphicsItem", true).index);
		switch (item->type()) {
		case 1:
			SET_SMOKEQYOTO_OBJECT("QGraphicsItem")
			break;
		case 2:
			SET_SMOKEQYOTO_OBJECT("QGraphicsPathItem")
			break;
		case 3:
			SET_SMOKEQYOTO_OBJECT("QGraphicsRectItem")
		case 4:
			SET_SMOKEQYOTO_OBJECT("QGraphicsEllipseItem")
			break;
		case 5:
			SET_SMOKEQYOTO_OBJECT("QGraphicsPolygonItem")
			break;
		case 6:
			SET_SMOKEQYOTO_OBJECT("QGraphicsLineItem")
			break;
		case 7:
			SET_SMOKEQYOTO_OBJECT("QGraphicsItem")
			break;
		case 8:
			SET_SMOKEQYOTO_OBJECT("QGraphicsTextItem")
			break;
		case 9:
			SET_SMOKEQYOTO_OBJECT("QGraphicsSimpleTextItem")
			break;
		case 10:
			SET_SMOKEQYOTO_OBJECT("QGraphicsItemGroup")
			break;
		}
	} else if (o->smoke->isDerivedFrom(o->smoke->classes[o->classId].className, "QLayoutItem")) {
		QLayoutItem * item = (QLayoutItem *) o->smoke->cast(o->ptr, o->classId, o->smoke->idClass("QLayoutItem", true).index);
		if (item->widget() != 0) {
			SET_SMOKEQYOTO_OBJECT("QWidgetItem")
		} else if (item->spacerItem() != 0) {
			SET_SMOKEQYOTO_OBJECT("QSpacerItem")
		}
	}
	
	return qyoto_modules[o->smoke].binding->className(o->classId);

#undef SET_SMOKEQYOTO_OBJECT
}

#if QT_VERSION >= 0x40300
void marshall_QListWizardButton(Marshall *m) {
    switch(m->action()) {
      case Marshall::FromObject:
	{
	    if (m->var().s_class == 0) {
		m->item().s_class = 0;
		return;
	    }
	    void* list = m->var().s_voidp;
	    void* valuelist = (*ListWizardButtonToQListWizardButton)(list);
	    m->item().s_voidp = valuelist;
	    m->next();

	    (*FreeGCHandle)(m->var().s_voidp);

		/*if (m->cleanup()) {
			delete valuelist;
	    }*/
	}
	break;
      case Marshall::ToObject:
	{
		// not needed yet
		printf("Marshalling QList<QWizard::WizardButton> not yet implemented\n");
	}
	break;
      default:
	m->unsupported();
	break;
    }
}
#endif

DEF_LIST_MARSHALLER( QAbstractButtonList, QList<QAbstractButton*>, QAbstractButton )
DEF_LIST_MARSHALLER( QActionGroupList, QList<QActionGroup*>, QActionGroup )
DEF_LIST_MARSHALLER( QActionList, QList<QAction*>, QAction )
DEF_LIST_MARSHALLER( QListWidgetItemList, QList<QListWidgetItem*>, QListWidgetItem )
DEF_LIST_MARSHALLER( QTableWidgetList, QList<QTableWidget*>, QTableWidget )
DEF_LIST_MARSHALLER( QTableWidgetItemList, QList<QTableWidgetItem*>, QTableWidgetItem )
DEF_LIST_MARSHALLER( QTextFrameList, QList<QTextFrame*>, QTextFrame )
DEF_LIST_MARSHALLER( QTreeWidgetItemList, QList<QTreeWidgetItem*>, QTreeWidgetItem )
DEF_LIST_MARSHALLER( QTreeWidgetList, QList<QTreeWidget*>, QTreeWidget )
DEF_LIST_MARSHALLER( QWidgetList, QList<QWidget*>, QWidget )

#if QT_VERSION >= 0x40200
DEF_LIST_MARSHALLER( QGraphicsItemList, QList<QGraphicsItem*>, QGraphicsItem )
DEF_LIST_MARSHALLER( QStandardItemList, QList<QStandardItem*>, QStandardItem )
DEF_LIST_MARSHALLER( QUndoStackList, QList<QUndoStack*>, QUndoStack )
#endif

#if QT_VERSION >= 0x40300
DEF_LIST_MARSHALLER( QMdiSubWindowList, QList<QMdiSubWindow*>, QMdiSubWindow )
#endif

DEF_VALUELIST_MARSHALLER( QColorVector, QVector<QColor>, QColor )
DEF_VALUELIST_MARSHALLER( QImageTextKeyLangList, QList<QImageTextKeyLang>, QImageTextKeyLang )
DEF_VALUELIST_MARSHALLER( QKeySequenceList, QList<QKeySequence>, QKeySequence )
DEF_VALUELIST_MARSHALLER( QModelIndexList, QList<QModelIndex>, QModelIndex )
DEF_VALUELIST_MARSHALLER( QPixmapList, QList<QPixmap>, QPixmap )
DEF_VALUELIST_MARSHALLER( QPolygonFList, QList<QPolygonF>, QPolygonF )
DEF_VALUELIST_MARSHALLER( QTableWidgetSelectionRangeList, QList<QTableWidgetSelectionRange>, QTableWidgetSelectionRange )
DEF_VALUELIST_MARSHALLER( QTextBlockList, QList<QTextBlock>, QTextBlock )
DEF_VALUELIST_MARSHALLER( QTextFormatVector, QVector<QTextFormat>, QTextFormat )
DEF_VALUELIST_MARSHALLER( QTextLayoutFormatRangeList, QList<QTextLayout::FormatRange>, QTextLayout::FormatRange)
DEF_VALUELIST_MARSHALLER( QTextLengthVector, QVector<QTextLength>, QTextLength )

#if QT_VERSION >= 0x40400
DEF_VALUELIST_MARSHALLER( QPrinterInfoList, QList<QPrinterInfo>, QPrinterInfo )
#endif

TypeHandler qtgui_handlers[] = {
    { "QList<QAbstractButton*>", marshall_QAbstractButtonList },
    { "QList<QActionGroup*>", marshall_QActionGroupList },
    { "QList<QAction*>", marshall_QActionList },
    { "QList<QAction*>&", marshall_QActionList },
    { "QList<QImageTextKeyLang>", marshall_QImageTextKeyLangList },
    { "QList<QKeySequence>", marshall_QKeySequenceList },
    { "QList<QKeySequence>&", marshall_QKeySequenceList },
    { "QList<QListWidgetItem*>", marshall_QListWidgetItemList },
    { "QList<QListWidgetItem*>&", marshall_QListWidgetItemList },
    { "QList<QModelIndex>", marshall_QModelIndexList },
    { "QList<QModelIndex>&", marshall_QModelIndexList },
    { "QList<QPixmap>", marshall_QPixmapList },
    { "QList<QPolygonF>", marshall_QPolygonFList },
    { "QList<QStandardItem*>", marshall_QStandardItemList },
    { "QList<QStandardItem*>&", marshall_QStandardItemList },
    { "QList<QTableWidgetItem*>", marshall_QTableWidgetItemList },
    { "QList<QTableWidgetItem*>&", marshall_QTableWidgetItemList },
    { "QList<QTableWidgetSelectionRange>", marshall_QTableWidgetSelectionRangeList },
    { "QList<QTextBlock>", marshall_QTextBlockList },
    { "QList<QTextFrame*>", marshall_QTextFrameList },
    { "QList<QTextLayout::FormatRange>", marshall_QTextLayoutFormatRangeList },
    { "QList<QTextLayout::FormatRange>&", marshall_QTextLayoutFormatRangeList },
    { "QList<QTreeWidgetItem*>", marshall_QTreeWidgetItemList },
    { "QList<QTreeWidgetItem*>&", marshall_QTreeWidgetItemList },
    { "QList<QTreeWidget*>&", marshall_QTreeWidgetList },
    { "QList<QUndoStack*>", marshall_QUndoStackList },
    { "QList<QUndoStack*>&", marshall_QUndoStackList },
    { "QList<QWidget*>", marshall_QWidgetList },
    { "QList<QWidget*>&", marshall_QWidgetList },
    { "QModelIndexList", marshall_QModelIndexList },
    { "QModelIndexList&", marshall_QModelIndexList },
    { "QVector<QColor>", marshall_QColorVector },
    { "QVector<QColor>&", marshall_QColorVector },
    { "QVector<QTextFormat>", marshall_QTextFormatVector },
    { "QVector<QTextFormat>&", marshall_QTextFormatVector },
    { "QVector<QTextLength>", marshall_QTextLengthVector },
    { "QVector<QTextLength>&", marshall_QTextLengthVector },
    { "QWidgetList", marshall_QWidgetList },
    { "QWidgetList&", marshall_QWidgetList },
#if QT_VERSION >= 0x40200
    { "QList<QGraphicsItem*>", marshall_QGraphicsItemList },
    { "QList<QGraphicsItem*>&", marshall_QGraphicsItemList },
    { "QList<QStandardItem*>", marshall_QStandardItemList },
    { "QList<QStandardItem*>&", marshall_QStandardItemList },
    { "QList<QUndoStack*>", marshall_QUndoStackList },
    { "QList<QUndoStack*>&", marshall_QUndoStackList },
#endif
#if QT_VERSION >= 0x40300
    { "QList<QMdiSubWindow*>", marshall_QMdiSubWindowList },
    { "QList<QWizard::WizardButton>", marshall_QListWizardButton },
    { "QList<QWizard::WizardButton>&", marshall_QListWizardButton },
#endif
    { 0, 0 }
};

// kate: space-indent off; mixed-indent off;
