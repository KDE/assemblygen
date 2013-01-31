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

#include "cswritedeclaration.h"
#include "cswriteicondeclaration.h"
#include "cswriteinitialization.h"
#include "cswriteiconinitialization.h"
#include "driver.h"
#include "ui4.h"
#include "uic.h"
#include "databaseinfo.h"
#include "customwidgetsinfo.h"

#include <QFileInfo>
#include <QTextStream>

namespace CS {

WriteDeclaration::WriteDeclaration(Uic *uic)
    : driver(uic->driver()), output(uic->output()), option(uic->option())
{
    this->uic = uic;
}

void WriteDeclaration::acceptUI(DomUI *node)
{
    QString qualifiedClassName = node->elementClass() + option.postfix;
    QString className = qualifiedClassName;
    QString access = QString("public").trimmed();

    QString varName = driver->findOrInsertWidget(node->elementWidget());
    QString widgetClassName = node->elementWidget()->attributeClass();

    QString exportMacro = node->elementExportMacro();
    if (!exportMacro.isEmpty())
        exportMacro.append(QLatin1Char(' '));

    QStringList nsList = qualifiedClassName.split(QLatin1String("::"));
    if (nsList.count()) {
        className = nsList.last();
        nsList.removeLast();
    }
    QString prefix = option.prefix;
    if (!option.name_space.isEmpty()) {
        nsList.append(option.name_space);
        prefix = "";
    }
    if (!option.klass.isEmpty()) {
        className = option.klass;
    }
    if (!option.access.isEmpty()) {
        access = option.access;
    }

    QListIterator<QString> it(nsList);
    while (it.hasNext()) {
        QString ns = it.next();
        if (ns.isEmpty())
            continue;

        output << "namespace " << ns << " {\n";
    }

    if (nsList.count())
        output << "\n";
    if (option.execCode) {
        output << access << " partial class " << exportMacro << prefix << className << "\n"
               << "{\n";
    } else {
        output << access << " partial class " << exportMacro << prefix << className << "\n"
               << "{\n";
    }

    QStringList connections = uic->databaseInfo()->connections();
    for (int i=0; i<connections.size(); ++i) {
        QString connection = connections.at(i);

        if (connection == QLatin1String("(default)"))
            continue;

        output << option.indent << "QSqlDatabase " << connection << "Connection;\n";
    }

    TreeWalker::acceptWidget(node->elementWidget());

    output << "\n";

    WriteInitialization(uic).acceptUI(node);

    if (node->elementImages()) {
        output << "\n"
            // << "protected:\n"
            << option.indent << "protected enum IconID\n"
            << option.indent << "{\n";
        WriteIconDeclaration(uic).acceptUI(node);

        output << option.indent << option.indent << "unknown_ID\n"
            << option.indent << "}\n";

        WriteIconInitialization(uic).acceptUI(node);
    }

    output << "}\n\n";

    it.toBack();
    while (it.hasPrevious()) {
        QString ns = it.previous();
        if (ns.isEmpty())
            continue;

        output << "} // namespace " << ns << "\n";
    }

    if (nsList.count())
        output << "\n";

    if (option.generateNamespace && !option.prefix.isEmpty() && option.name_space.isEmpty()) {
        nsList.append(QLatin1String("Ui"));

        QListIterator<QString> it(nsList);
        while (it.hasNext()) {
            QString ns = it.next();
            if (ns.isEmpty())
                continue;

            output << "namespace " << ns << " {\n";
        }

        output << option.indent << access << " class " << exportMacro << className << " : " << option.prefix << className << " {}\n";

        it.toBack();
        while (it.hasPrevious()) {
            QString ns = it.previous();
            if (ns.isEmpty())
                continue;

            output << "} // namespace " << ns << "\n";
        }

        if (nsList.count())
            output << "\n";
    }
}

void WriteDeclaration::acceptWidget(DomWidget *node)
{
    QString className = QLatin1String("QWidget");
    QString memberAccess = QString("public").trimmed();
    if (node->hasAttributeClass())
        className = node->attributeClass();
    if (!option.memberAccess.isEmpty())
        memberAccess = option.memberAccess;

    output << option.indent << memberAccess << " " << uic->customWidgetsInfo()->realClassName(className) << " " << driver->findOrInsertWidget(node) << ";\n";

    TreeWalker::acceptWidget(node);
}

void WriteDeclaration::acceptLayout(DomLayout *node)
{
    QString className = QLatin1String("QLayout");
    QString memberAccess = QString("public").trimmed();
    if (node->hasAttributeClass())
        className = node->attributeClass();
    if (!option.memberAccess.isEmpty())
        memberAccess = option.memberAccess;

    output << option.indent << memberAccess << " " << className << " " << driver->findOrInsertLayout(node) << ";\n";

    TreeWalker::acceptLayout(node);
}

void WriteDeclaration::acceptSpacer(DomSpacer *node)
{
    QString memberAccess = QString("public").trimmed();
    if (!option.memberAccess.isEmpty())
        memberAccess = option.memberAccess;

    output << option.indent << memberAccess << " QSpacerItem " << driver->findOrInsertSpacer(node) << ";\n";

    TreeWalker::acceptSpacer(node);
}

void WriteDeclaration::acceptActionGroup(DomActionGroup *node)
{
    QString memberAccess = QString("public").trimmed();
    if (!option.memberAccess.isEmpty())
        memberAccess = option.memberAccess;

    output << option.indent << memberAccess << " QActionGroup " << driver->findOrInsertActionGroup(node) << ";\n";

    TreeWalker::acceptActionGroup(node);
}

void WriteDeclaration::acceptAction(DomAction *node)
{
    QString memberAccess = QString("public").trimmed();
    if (!option.memberAccess.isEmpty())
        memberAccess = option.memberAccess;

    output << option.indent << memberAccess << " QAction " << driver->findOrInsertAction(node) << ";\n";

    TreeWalker::acceptAction(node);
}

} // namespace CS
