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

#include <QGraphicsItem>
#include <QGraphicsScene>

#include <callbacks.h>
#include <qyoto.h>
#include <qyotosmokebinding.h>

#include <qtgui_smoke.h>

extern TypeHandler qtgui_handlers[];
extern bool IsContainedInstanceQtGui(smokeqyoto_object *o);
extern const char * qyoto_resolve_classname_qtgui(smokeqyoto_object * o);

namespace Qyoto {

class QtGuiBinding : public Qyoto::Binding
{
public:
    QtGuiBinding(Smoke *s, const QHash<int, QByteArray>& classname) : Qyoto::Binding(s, classname) {}

    virtual bool callMethod(void *obj, smokeqyoto_object *sqo, const QByteArray& signature, Smoke::Stack args, bool isAbstract) {
#if QT_VERSION >= 0x40200
        static Smoke::Index qgraphicsitem_class = Smoke::classMap["QGraphicsItem"].index;

        if (strcmp(signature, "itemChange(QGraphicsItem::GraphicsItemChange, const QVariant&)") == 0
            && smoke->isDerivedFrom(smoke, sqo->classId, qtgui_Smoke, qgraphicsitem_class))
        {
            int change = args[1].s_int;
            if (change == QGraphicsItem::ItemSceneChange) {
                QGraphicsScene *scene = ((QVariant*) args[2].s_voidp)->value<QGraphicsScene*>();
                if (scene) {
                    (*AddGlobalRef)(obj, sqo->ptr);
                } else {
                    QGraphicsItem *item = (QGraphicsItem*) sqo->smoke->cast(sqo->ptr, sqo->classId, qgraphicsitem_class);
                    if (!item->group()) {  // only remove the global ref if the item doesn't belong to a group
                        (*RemoveGlobalRef)(obj, sqo->ptr);
                    }
                }
            }
        }
#endif
    return Qyoto::Binding::callMethod(obj, sqo, signature, args, isAbstract);
    }
};

}

extern "C" Q_DECL_EXPORT void
Init_qyoto_qtgui()
{
    init_qtgui_Smoke();

    qyoto_install_handlers(qtgui_handlers);
    QByteArray prefix("QtGui.");

    QHash<int, QByteArray> qtgui_classname;
    for (int i = 1; i <= qtgui_Smoke->numClasses; i++) {
        QByteArray name(qtgui_Smoke->classes[i].className);
        name.replace("::", ".");
        if (name != "QAccessible2") {
            name.prepend(prefix);
        }
        qtgui_classname.insert(i, name);
    }
    static Qyoto::QtGuiBinding binding = Qyoto::QtGuiBinding(qtgui_Smoke, qtgui_classname);
    QyotoModule module = { "qyoto_qtgui", qyoto_resolve_classname_qtgui, IsContainedInstanceQtGui, &binding };
    qyoto_modules[qtgui_Smoke] = module;
}
