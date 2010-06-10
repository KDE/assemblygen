/***************************************************************************
                          kdehandlers.cpp  -  KDE specific marshallers
                             -------------------
    begin                : Tuesday Jun 16 2008
    copyright            : (C) 2008 by Richard Dale
    email                : richard.j.dale@gmail.org
 ***************************************************************************/

/***************************************************************************
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License as published by  *
 *   the Free Software Foundation; either version 2 of the License, or     *
 *   (at your option) any later version.                                   *
 *                                                                         *
 ***************************************************************************/

#include <qyoto.h>
#include <smokeqyoto.h>
#include <marshall_macros.h>

#include <marshall_macros_kde.h>

#include <kparts/part.h>
#include <kparts/plugin.h>

DEF_LIST_MARSHALLER( KPartsPartList, QList<KParts::Part*>, KParts::Part )
DEF_LIST_MARSHALLER( KPartsPluginList, QList<KParts::Plugin*>, KParts::Plugin )
DEF_LIST_MARSHALLER( KPartsReadOnlyPartList, QList<KParts::ReadOnlyPart*>, KParts::ReadOnlyPart )

DEF_VALUELIST_MARSHALLER( KPartsPluginPluginInfoList, QList<KParts::Plugin::PluginInfo>, KParts::Plugin::PluginInfo )

TypeHandler Kimono_KParts_handlers[] = {
    { "QList<KParts::Part*>", marshall_KPartsPartList },
    { "QList<KParts::Part*>&", marshall_KPartsPartList },
    { "QList<KParts::Plugin*>", marshall_KPartsPluginList },
    { "QList<KParts::Plugin*>&", marshall_KPartsPluginList },
    { "QList<KParts::Plugin::PluginInfo>", marshall_KPartsPluginPluginInfoList },
    { "QList<KParts::Plugin::PluginInfo>&", marshall_KPartsPluginPluginInfoList },
    { "QList<KParts::ReadOnlyPart*>", marshall_KPartsReadOnlyPartList },
    { "QList<KParts::ReadOnlyPart*>&", marshall_KPartsReadOnlyPartList },

    { 0, 0 }
};
