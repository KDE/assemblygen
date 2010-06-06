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

#include <kaboutdata.h>
#include <kautosavefile.h>
#include <kcoreconfigskeleton.h>
#include <kdatatool.h>
#include <kdeversion.h>
#include <kfile.h>
#include <kjob.h>
#include <klocalizedstring.h>
#include <kmimetype.h>
#include <kmountpoint.h>
#include <kplugininfo.h>
#include <kservicegroup.h>
#include <kservice.h>
#include <ksycocatype.h>
#include <ktimezone.h>
#include <ktrader.h>
#include <kurl.h>
#include <kuser.h>

#include "marshall_macros_kde.h"

void marshall_KServiceList(Marshall *m) {
	switch(m->action()) {
	case Marshall::FromObject:
	{
	}
	break;
	case Marshall::ToObject:
	{
		KService::List *offerList = (KService::List*)m->item().s_voidp;
		if (!offerList) {
			m->var().s_voidp = 0;
			break;
		}

		void *av = (*ConstructList)("Kimono.KService");

		for (	KService::List::Iterator it = offerList->begin();
				it != offerList->end();
				++it ) 
		{
			KSharedPtr<KService> *ptr = new KSharedPtr<KService>(*it);
			KService * currentOffer = ptr->data();

			void *obj = (*GetInstance)(currentOffer, true);
			if (obj == 0) {
				smokeqyoto_object * o = alloc_smokeqyoto_object(false, m->smoke(), m->smoke()->idClass("KService").index, currentOffer);
				obj = (*CreateInstance)("Kimono.KService", o);
			}
			(*AddIntPtrToList)(av, obj);
		}

		m->var().s_voidp = av;
	    
		if (m->type().isStack())
			delete offerList;
	}
	break;
	default:
		m->unsupported();
		break;
	}
}

DEF_KSHAREDPTR_MARSHALLER(KSharedConfig, KSharedConfig)
DEF_KSHAREDPTR_MARSHALLER(KService, KService)
DEF_KSHAREDPTR_MARSHALLER(KMimeType, KMimeType)

DEF_LIST_MARSHALLER( KAutoSaveFileList, QList<KAutoSaveFile*>, KAutoSaveFile )
DEF_LIST_MARSHALLER( KJobList, QList<KJob*>, KJob )

DEF_VALUELIST_MARSHALLER( KAboutLicenseList, QList<KAboutLicense>, KAboutLicense )
DEF_VALUELIST_MARSHALLER( KAboutPersonList, QList<KAboutPerson>, KAboutPerson )
DEF_VALUELIST_MARSHALLER( KCoreConfigSkeletonItemEnumChoiceList, QList<KCoreConfigSkeleton::ItemEnum::Choice>, KCoreConfigSkeleton::ItemEnum::Choice )
DEF_VALUELIST_MARSHALLER( KPluginInfoList, QList<KPluginInfo>, KPluginInfo )
DEF_VALUELIST_MARSHALLER( KServiceActionList, QList<KServiceAction>, KServiceAction )
DEF_VALUELIST_MARSHALLER( KServiceGroupPtrList, QList<KServiceGroup::Ptr>, KServiceGroup::Ptr )
DEF_VALUELIST_MARSHALLER( KTimeZoneLeapSecondsList, QList<KTimeZone::LeapSeconds>, KTimeZone::LeapSeconds )
DEF_VALUELIST_MARSHALLER( KTimeZonePhaseList, QList<KTimeZone::Phase>, KTimeZone::Phase )
DEF_VALUELIST_MARSHALLER( KTimeZoneTransitionList, QList<KTimeZone::Transition>, KTimeZone::Transition )
DEF_VALUELIST_MARSHALLER( KUrlList, QList<KUrl>, KUrl )
DEF_VALUELIST_MARSHALLER( KUserGroupList, QList<KUserGroup>, KUserGroup )
DEF_VALUELIST_MARSHALLER( KUserList, QList<KUser>, KUser )

TypeHandler Kimono_KDECore_handlers[] = {
    { "KPluginInfo::List", marshall_KPluginInfoList },
    { "KPluginInfo::List&", marshall_KPluginInfoList },
    { "KService::List", marshall_KServiceList },
    { "KService::List&", marshall_KServiceList },
    { "KService::Ptr", marshall_KSharedPtr_KService },
    { "KService::Ptr&", marshall_KSharedPtr_KService },
    { "KSharedPtr<KMimeType>", marshall_KSharedPtr_KMimeType },
    { "KSharedPtr<KMimeType>&", marshall_KSharedPtr_KMimeType },
    { "KSharedPtr<KService>", marshall_KSharedPtr_KService },
    { "KSharedPtr<KService>&", marshall_KSharedPtr_KService },
    { "KSharedConfig::Ptr", marshall_KSharedPtr_KSharedConfig },
    { "KSharedConfig::Ptr&", marshall_KSharedPtr_KSharedConfig },
    { "KSharedConfigPtr", marshall_KSharedPtr_KSharedConfig },
    { "KSharedConfigPtr&", marshall_KSharedPtr_KSharedConfig },
    { "KSharedPtr<KSharedConfig>", marshall_KSharedPtr_KSharedConfig },
    { "KSharedPtr<KSharedConfig>&", marshall_KSharedPtr_KSharedConfig },
    { "KUrl::List", marshall_KUrlList },
    { "KUrl::List&", marshall_KUrlList },
    { "KUrlList", marshall_KUrlList },
    { "KUrlList&", marshall_KUrlList },
    { "QList<KAboutLicense>", marshall_KAboutLicenseList },
    { "QList<KAboutPerson>", marshall_KAboutPersonList },
    { "QList<KAutoSaveFile*>", marshall_KAutoSaveFileList },
    { "QList<KAutoSaveFile*>&", marshall_KAutoSaveFileList },
    { "QList<KCoreConfigSkeleton::ItemEnum::Choice>", marshall_KCoreConfigSkeletonItemEnumChoiceList },
    { "QList<KCoreConfigSkeleton::ItemEnum::Choice>&", marshall_KCoreConfigSkeletonItemEnumChoiceList },
    { "QList<KJob*>&", marshall_KJobList },
    { "QList<KPluginInfo>", marshall_KPluginInfoList },
    { "QList<KPluginInfo>&", marshall_KPluginInfoList },
    { "QList<KServiceAction>", marshall_KServiceActionList },
    { "QList<KServiceGroup::Ptr>", marshall_KServiceGroupPtrList },
    { "QList<KService::Ptr>", marshall_KServiceList },
    { "QList<KSharedPtr<KService> >", marshall_KServiceList },
    { "QList<KTimeZone::LeapSeconds>", marshall_KTimeZoneLeapSecondsList },
    { "QList<KTimeZone::LeapSeconds>&", marshall_KTimeZoneLeapSecondsList },
    { "QList<KTimeZone::Phase>", marshall_KTimeZonePhaseList },
    { "QList<KTimeZone::Phase>&", marshall_KTimeZonePhaseList },
    { "QList<KTimeZone::Transition>", marshall_KTimeZoneTransitionList },
    { "QList<KTimeZone::Transition>&", marshall_KTimeZoneTransitionList },
    { "QList<KUrl>", marshall_KUrlList },
    { "QList<KUserGroup>", marshall_KUserGroupList },
    { "QList<KUser>", marshall_KUserList },
    { "QList<KUser>&", marshall_KUserList },

    { 0, 0 }
};
