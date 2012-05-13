/****************************************************************************
**
** Copyright (C) 1992-2006 Trolltech ASA. All rights reserved.
**
** This file is part of the tools applications of the Qt Toolkit.
**
** Licensees holding valid Qt Preview licenses may use this file in
** accordance with the Qt Preview License Agreement provided with the
** Software.
**
** See http://www.trolltech.com/pricing.html or email sales@trolltech.com for
** information about Qt Commercial License Agreements.
**
** Contact info@trolltech.com if any conditions of this licensing are
** not clear to you.
**
** This file is provided AS IS with NO WARRANTY OF ANY KIND, INCLUDING THE
** WARRANTY OF DESIGN, MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE.
**
****************************************************************************/

#ifndef CSWRITEINITIALIZATION_H
#define CSWRITEINITIALIZATION_H

#include "treewalker.h"
#include <QPair>
#include <QHash>
#include <QMap>
#include <QStack>
#include <QTextStream>

class Driver;
class Uic;
class DomBrush;
class DomFont;
class DomSizePolicy;
struct Option;

namespace CS {
    // Handle for a flat DOM font to get comparison functionality required for maps
    class FontHandle {
    public:
        FontHandle(const DomFont *domFont);
        int compare(const FontHandle &) const;
    private:
        const DomFont *m_domFont;
#if defined(Q_OS_MAC) && defined(Q_CC_GNU) && (__GNUC__ == 3 && __GNUC_MINOR__ == 3)
        friend uint qHash(const FontHandle &);
#endif
    };
    inline bool operator ==(const FontHandle &f1, const FontHandle &f2) { return f1.compare(f2) == 0; }
    inline bool operator  <(const FontHandle &f1, const FontHandle &f2) { return f1.compare(f2) < 0; }

    // Handle for a flat DOM size policy to get comparison functionality required for maps
    class SizePolicyHandle {
    public:
        SizePolicyHandle(const DomSizePolicy *domSizePolicy);
        int compare(const SizePolicyHandle &) const;
    private:
        const DomSizePolicy *m_domSizePolicy;
#if defined(Q_OS_MAC) && defined(Q_CC_GNU) && (__GNUC__ == 3 && __GNUC_MINOR__ == 3)
        friend uint qHash(const SizePolicyHandle &);
#endif
    };
    inline bool operator ==(const SizePolicyHandle &f1, const SizePolicyHandle &f2) { return f1.compare(f2) == 0; }
#if !(defined(Q_OS_MAC) && defined(Q_CC_GNU) && (__GNUC__ == 3 && __GNUC_MINOR__ == 3))
    inline bool operator  <(const SizePolicyHandle &f1, const SizePolicyHandle &f2) { return f1.compare(f2) < 0; }
#endif



struct WriteInitialization : public TreeWalker
{
    typedef QList<DomProperty*> DomPropertyList;
    typedef QHash<QString, DomProperty*> DomPropertyMap;

    WriteInitialization(Uic *uic);

//
// widgets
//
    void acceptUI(DomUI *node);
    void acceptWidget(DomWidget *node);
//    void acceptWidgetScripts(const DomScripts &, DomWidget *node, const  DomWidgets &childWidgets);

    void acceptLayout(DomLayout *node);
    void acceptSpacer(DomSpacer *node);
    void acceptLayoutItem(DomLayoutItem *node);

//
// actions
//
    void acceptActionGroup(DomActionGroup *node);
    void acceptAction(DomAction *node);
    void acceptActionRef(DomActionRef *node);

//
// tab stops
//
    void acceptTabStops(DomTabStops *tabStops);

//
// custom widgets
//
    void acceptCustomWidgets(DomCustomWidgets *node);
    void acceptCustomWidget(DomCustomWidget *node);

//
// layout defaults/functions
//
    void acceptLayoutDefault(DomLayoutDefault *node)   { m_LayoutDefaultHandler.acceptLayoutDefault(node); }
    void acceptLayoutFunction(DomLayoutFunction *node) { m_LayoutDefaultHandler.acceptLayoutFunction(node); }

//
// signal/slot connections
//
    void acceptConnection(DomConnection *connection);

//
// images
//
    void acceptImage(DomImage *image);
    enum {
        Use43UiFile = 0,
        TopLevelMargin,
        ChildMargin,
        SubLayoutMargin
    };

private:
    static QString domColor2QString(const DomColor *c);

    QString pixCall(const DomProperty *prop) const;
    QString trCall(const QString &str, const QString &comment = QString()) const;
    QString trCall(DomString *str) const;

    enum { WritePropertyIgnoreMargin = 1, WritePropertyIgnoreSpacing = 2 };
    void writeProperties(const QString &varName, const QString &className, const DomPropertyList &lst, unsigned flags = 0);
    void writeColorGroup(DomColorGroup *colorGroup, const QString &group, const QString &paletteName);
    void writeBrush(DomBrush *brush, const QString &brushName);

//
// special initialization
//
    void initializeMenu(DomWidget *w, const QString &parentWidget);
    void initializeComboBox(DomWidget *w);
    void initializeListWidget(DomWidget *w);
    void initializeTreeWidget(DomWidget *w);
    void initializeTreeWidgetItems(const QString &className, const QString &varName, const QList<DomItem *> &items);
    void initializeTableWidget(DomWidget *w);

//
// special initialization for the Q3 support classes
//
    void initializeQ3ListBox(DomWidget *w);
    void initializeQ3IconView(DomWidget *w);
    void initializeQ3ListView(DomWidget *w);
    void initializeQ3ListViewItems(const QString &className, const QString &varName, const QList<DomItem*> &items);
    void initializeQ3Table(DomWidget *w);
    void initializeQ3TableItems(const QString &className, const QString &varName, const QList<DomItem*> &items);

//
// Sql
//
    void initializeQ3SqlDataTable(DomWidget *w);
    void initializeQ3SqlDataBrowser(DomWidget *w);

    QString findDeclaration(const QString &name);
    DomWidget *findWidget(const QString &widgetClass);
    DomImage *findImage(const QString &name) const;

    bool isValidObject(const QString &name) const;

private:
    QString writeFontProperties(const DomFont *f);
    QString writeSizePolicy(const DomSizePolicy *sp);

    const Uic *m_uic;
    Driver *m_driver;
    QTextStream &m_output;
    const Option &m_option;
    bool m_stdsetdef;

    struct Buddy
    {
        Buddy(const QString &oN, const QString &b)
            : objName(oN), buddy(b) {}
        QString objName;
        QString buddy;
    };

    QStack<DomWidget*> m_widgetChain;
    QStack<DomLayout*> m_layoutChain;
    QStack<DomActionGroup*> m_actionGroupChain;
    QList<Buddy> m_buddies;

    QHash<QString, QString> m_buttonGroups;
    QHash<QString, DomWidget*> m_registeredWidgets;
    QHash<QString, DomImage*> m_registeredImages;
    QHash<QString, DomAction*> m_registeredActions;
    QHash<uint, QString> m_colorBrushHash;
    // Map from font properties to  font variable name for reuse
    // Map from size policy to  variable for reuse
#if defined(Q_OS_MAC) && defined(Q_CC_GNU) && (__GNUC__ == 3 && __GNUC_MINOR__ == 3)
    typedef QHash<FontHandle, QString> FontPropertiesNameMap;
    typedef QHash<SizePolicyHandle, QString> SizePolicyNameMap;
#else
    typedef QMap<FontHandle, QString> FontPropertiesNameMap;
    typedef QMap<SizePolicyHandle, QString> SizePolicyNameMap;
#endif
    FontPropertiesNameMap m_FontPropertiesNameMap;
    SizePolicyNameMap m_SizePolicyNameMap;

    class LayoutDefaultHandler {
    public:
        LayoutDefaultHandler();
        void acceptLayoutDefault(DomLayoutDefault *node);
        void acceptLayoutFunction(DomLayoutFunction *node);

        // Write out the layout margin and spacing properties applying the defaults.
        void writeProperties(const QString &indent, const QString &varName,
                             const DomPropertyMap &pm, int marginType,
                             bool suppressMarginDefault, QTextStream &str) const;
    private:
        void writeProperty(int p, const QString &indent, const QString &objectName, const DomPropertyMap &pm,
                           const QString &propertyName, const QString &setter, int defaultStyleValue,
                           bool suppressDefault, QTextStream &str) const;

        enum Properties { Margin, Spacing, NumProperties };
        enum StateFlags { HasDefaultValue = 1, HasDefaultFunction = 2};
        unsigned m_state[NumProperties];
        int m_defaultValues[NumProperties];
        QString m_functions[NumProperties];
    };

    // layout defaults
    LayoutDefaultHandler m_LayoutDefaultHandler;
    int m_layoutMarginType;

    QString m_generatedClass;

    QString m_delayedInitialization;
    QTextStream m_delayedOut;

    QString m_refreshInitialization;
    QTextStream m_refreshOut;

    QString m_delayedActionInitialization;
    QTextStream m_actionOut;

    bool m_layoutWidget;
};

} // namespace CS

#endif // CSWRITEINITIALIZATION_H
