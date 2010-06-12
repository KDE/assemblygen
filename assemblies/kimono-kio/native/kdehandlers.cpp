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

#include <kfileitem.h>
#include <kio/copyjob.h>

#include <marshall_macros_kde.h>

DEF_VALUELIST_MARSHALLER( KFileItemList, QList<KFileItem>, KFileItem )
DEF_VALUELIST_MARSHALLER( KIOCopyInfoList, QList<KIO::CopyInfo>, KIO::CopyInfo )

/// TODO: Add marshallers for KFileItemList and KIO::MetaData

TypeHandler Kimono_KIO_handlers[] = {
    { "QList<KFileItem>", marshall_KFileItemList },
    { "QList<KFileItem>&", marshall_KFileItemList },
    { "QList<KIO::CopyInfo>", marshall_KIOCopyInfoList },
    { "QList<KIO::CopyInfo>&", marshall_KIOCopyInfoList },

    { 0, 0 }
};
