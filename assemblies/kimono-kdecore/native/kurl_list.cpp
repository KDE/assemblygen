/***************************************************************************
                        kimono.cpp  -  description
                             -------------------
    begin                : Mon Sep 10 2007
    copyright            : (C) 2007 by Arno Rehn
    email                : arno@arnorehn.de
 ***************************************************************************/

/***************************************************************************
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License as published by  *
 *   the Free Software Foundation; either version 2 of the License, or     *
 *   (at your option) any later version.                                   *
 *                                                                         *
 ***************************************************************************/

#include <QString>
#include <QStringList>

#include <kurl.h>

#include <qyoto.h>
#include <callbacks.h>

#include <smoke.h>
#include <smoke/kdecore_smoke.h>

extern "C" {

typedef bool (*GetNextDictionaryEntryFn)(ushort** key, ushort** value);

Q_DECL_EXPORT void
KUrlListPopulateMimeData(NoArgs getNextItem, void* mimeData,
    GetNextDictionaryEntryFn getNextDictionaryEntry, uint flags)
{
    QMimeData* md = (QMimeData*) ((smokeqyoto_object*) (*GetSmokeObject)(mimeData))->ptr;
    (*FreeGCHandle)(mimeData);
    KUrl::List list;
    for (void* handle = (*getNextItem)(); handle; handle = (*getNextItem)()) {
        list.append(*(KUrl*) ((smokeqyoto_object*) (*GetSmokeObject)(mimeData))->ptr);
        (*FreeGCHandle)(handle);
    }
    QMap<QString, QString> map;
    for (ushort *key = 0, *value = 0; getNextDictionaryEntry(&key, &value); ) {
        QString k = QString::fromUtf16(key);
        QString v = QString::fromUtf16(value);
        map.insert(k, v);
    }
    list.populateMimeData(md, map, (KUrl::MimeDataFlags) flags);
}

Q_DECL_EXPORT void
KUrlListMimeDataTypes(FromIntPtr addfn)
{
    foreach(QString str, KUrl::List::mimeDataTypes())
        (*addfn)((*IntPtrFromQString)(&str));
}

Q_DECL_EXPORT bool
KUrlListCanDecode(void* mimeData)
{
    QMimeData* md = (QMimeData*) ((smokeqyoto_object*) (*GetSmokeObject)(mimeData))->ptr;
    (*FreeGCHandle)(mimeData);
    return KUrl::List::canDecode(md);
}

Q_DECL_EXPORT void
KUrlListFromMimeData(FromIntPtr addfn, void* mimeData, GetNextDictionaryEntryFn getNextDictionaryEntry)
{
    QMimeData* md = (QMimeData*) ((smokeqyoto_object*) (*GetSmokeObject)(mimeData))->ptr;
    (*FreeGCHandle)(mimeData);
    QMap<QString, QString> map;
    for (ushort *key = 0, *value = 0; getNextDictionaryEntry(&key, &value); ) {
        QString k = QString::fromUtf16(key);
        QString v = QString::fromUtf16(value);
        map.insert(k, v);
    }
    Smoke::Index id = kdecore_Smoke->idClass("KUrl").index;
    foreach(KUrl url, KUrl::List::fromMimeData(md, (map.size() > 0)? &map : 0)) {
        smokeqyoto_object *o = alloc_smokeqyoto_object(true, kdecore_Smoke, id, new KUrl(url));
        (*addfn)((*CreateInstance)("Kimono.KUrl", o));
    }
}

}
