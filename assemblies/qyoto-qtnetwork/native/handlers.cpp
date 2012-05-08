/***************************************************************************
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU Lesser General Public License as        *
 *   published by the Free Software Foundation; either version 2 of the    *
 *   License, or (at your option) any later version.                       *
 *                                                                         *
 ***************************************************************************/

#include <QtNetwork/qhostaddress.h>
#include <QtNetwork/qnetworkinterface.h>
#include <QtNetwork/qurlinfo.h>

#if QT_VERSION >= 0x40300
#include <QtNetwork/qsslcertificate.h>
#include <QtNetwork/qsslcipher.h>
#include <QtNetwork/qsslerror.h>
#endif

#if QT_VERSION >= 0x040400
#include <QtNetwork/qnetworkcookie.h>
#endif

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

#include <marshall.h>
#include <qyoto.h>
#include <callbacks.h>
#include <smokeqyoto.h>
#include <marshall_macros.h>

#ifdef Q_OS_WIN
#include <windows.h>
#endif

DEF_LIST_MARSHALLER( QHostAddressList, QList<QHostAddress*>, QHostAddress )
DEF_LIST_MARSHALLER( QNetworkAddressEntryList, QList<QNetworkAddressEntry*>, QNetworkAddressEntry )
DEF_LIST_MARSHALLER( QNetworkInterfaceList, QList<QNetworkInterface*>, QNetworkInterface )

#if QT_VERSION >= 0x40300
DEF_LIST_MARSHALLER( QSslCertificateList, QList<QSslCertificate*>, QSslCertificate )
DEF_LIST_MARSHALLER( QSslCipherList, QList<QSslCipher*>, QSslCipher )
DEF_LIST_MARSHALLER( QSslErrorList, QList<QSslError*>, QSslError )
#endif

#if QT_VERSION >= 0x40400
DEF_LIST_MARSHALLER( QNetworkCookieList, QList<QNetworkCookie*>, QNetworkCookie )
#endif

DEF_QPAIR_MARSHALLER( QPair_QHostAddress_int, QHostAddress, int, "Qyoto.QHostAddress", "System.Int32" )

Q_DECL_EXPORT TypeHandler Qyoto_qtnetwork_handlers[] = {
    { "QList<QHostAddress>", marshall_QHostAddressList },
    { "QList<QHostAddress>&", marshall_QHostAddressList },
    { "QList<QNetworkAddressEntry>", marshall_QNetworkAddressEntryList },
    { "QList<QNetworkInterface>", marshall_QNetworkInterfaceList },
    { "QPair<QHostAddress,int>", marshall_QPair_QHostAddress_int },
    { "QPair<QHostAddress,int>&", marshall_QPair_QHostAddress_int },
#if QT_VERSION >= 0x40300
    { "QList<QSslCertificate>", marshall_QSslCertificateList },
    { "QList<QSslCertificate>&", marshall_QSslCertificateList },
    { "QList<QSslCipher>", marshall_QSslCipherList },
    { "QList<QSslCipher>&", marshall_QSslCipherList },
    { "QList<QSslError>", marshall_QSslErrorList },
    { "QList<QSslError>&", marshall_QSslErrorList },
#endif
#if QT_VERSION >= 0x040400
    { "QList<QNetworkCookie>", marshall_QNetworkCookieList },
    { "QList<QNetworkCookie>&", marshall_QNetworkCookieList },
#endif
    { 0, 0 }
};

// kate: space-indent on; mixed-indent off;
