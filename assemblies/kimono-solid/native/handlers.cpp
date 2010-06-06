/***************************************************************************
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU Lesser General Public License as        *
 *   published by the Free Software Foundation; either version 2 of the    *
 *   License, or (at your option) any later version.                       *
 *                                                                         *
 ***************************************************************************/

#include <solid/device.h>

#include <smoke.h>
#include <smoke/solid_smoke.h>

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
#include <callbacks.h>
#include <smokeqyoto.h>
#include <marshall_macros.h>

DEF_VALUELIST_MARSHALLER( SolidDeviceList, QList<Solid::Device>, Solid::Device )

Q_DECL_EXPORT TypeHandler Kimono_solid_handlers[] = {
    { "QList<Solid::Device>", marshall_SolidDeviceList },
    { "QList<Solid::Device>&", marshall_SolidDeviceList },
    { 0, 0 }
};

// kate: space-indent on; mixed-indent off;
