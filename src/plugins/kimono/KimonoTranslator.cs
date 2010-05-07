/*
    Generator for .NET assemblies utilizing SMOKE libraries
    Copyright (C) 2009 Arno Rehn <arno@arnorehn.de>

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along
    with this program; if not, write to the Free Software Foundation, Inc.,
    51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/

using System;
using System.Collections.Generic;
using System.CodeDom;
using System.Text.RegularExpressions;

public unsafe class KimonoTranslator : ICustomTranslator {

    Dictionary<string, Type> typeMap = new Dictionary<string, Type>()
    {
        { "mode_t", typeof(uint) },
        { "qulonglong", typeof(ulong) },

        { "Controls", typeof(uint) },
        { "KConfigGroup.WriteConfigFlags", typeof(uint) },
        { "KParts.BrowserExtension.PopupFlags", typeof(uint) },
        { "ShortcutTypes", typeof(uint) },
    };

    Dictionary<string, string> typeStringMap = new Dictionary<string, string>()
    {
        { "CommandType", "FileUndoManager.CommandType" },
        { "EncryptionMode", "KTcpSocket.EncryptionMode" },
        { "InformationList", "System.Collections.Generic.List<Information>" },
        { "KIO::UDSEntryList", "System.Collections.Generic.List<KIO.UDSEntry>" },
        { "KParts::BrowserExtension::ActionGroupMap", "System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<QAction>>" },
        { "KParts::LiveConnectExtension::ArgList", "System.Collections.Generic.List<QPair<Type, string>>" },
        { "QVariantList", "System.Collections.Generic.List<QVariant>" },
        { "QVariantMap", "System.Collections.Generic.Dictionary<string, QVariant>" },
        { "Plasma::DataEngine::Data", "System.Collections.Generic.Dictionary<string, QVariant>" },
    };

    Dictionary<string, Translator.TranslateFunc> typeCodeMap = new Dictionary<string, Translator.TranslateFunc>()
    {
        { "_IceConn", delegate { throw new NotSupportedException(); } },
        { "K3Icon", delegate { throw new NotSupportedException(); } },
        { "KAbstractViewAdapter", delegate { throw new NotSupportedException(); } },
        { "KCalendarSystemPrivate", delegate { throw new NotSupportedException(); } },
        { "KCatalogName", delegate { throw new NotSupportedException(); } },
        { "KCompletionMatchesWrapper", delegate { throw new NotSupportedException(); } },
        { "KCompositeJobPrivate", delegate { throw new NotSupportedException(); } },
        { "KConfigPrivate", delegate { throw new NotSupportedException(); } },
        { "KDialogPrivate", delegate { throw new NotSupportedException(); } },

        { "KIO::ChmodJobPrivate", delegate { throw new NotSupportedException(); } },
        { "KIO::CopyJobPrivate", delegate { throw new NotSupportedException(); } },
        { "KIO::DavJobPrivate", delegate { throw new NotSupportedException(); } },
        { "KIO::DeleteJobPrivate", delegate { throw new NotSupportedException(); } },
        { "KIO::DirectorySizeJobPrivate", delegate { throw new NotSupportedException(); } },
        { "KIO::FileCopyJobPrivate", delegate { throw new NotSupportedException(); } },
        { "KIO::FileJobPrivate", delegate { throw new NotSupportedException(); } },
        { "KIO::JobPrivate", delegate { throw new NotSupportedException(); } },
        { "KIO::ListJobPrivate", delegate { throw new NotSupportedException(); } },
        { "KIO::MimetypeJobPrivate", delegate { throw new NotSupportedException(); } },
        { "KIO::MultiGetJobPrivate", delegate { throw new NotSupportedException(); } },
        { "KIO::SimpleJobPrivate", delegate { throw new NotSupportedException(); } },
        { "KIO::SlaveInterfacePrivate", delegate { throw new NotSupportedException(); } },
        { "KIO::StatJobPrivate", delegate { throw new NotSupportedException(); } },
        { "KIO::StoredTransferJobPrivate", delegate { throw new NotSupportedException(); } },
        { "KIO::TransferJobPrivate", delegate { throw new NotSupportedException(); } },

        { "KJobPrivate", delegate { throw new NotSupportedException(); } },
        { "KMainWindowPrivate", delegate { throw new NotSupportedException(); } },
        { "KMimeTypePrivate", delegate { throw new NotSupportedException(); } },
        { "KPageDialogPrivate", delegate { throw new NotSupportedException(); } },
        { "KPageModelPrivate", delegate { throw new NotSupportedException(); } },
        { "KPageViewPrivate", delegate { throw new NotSupportedException(); } },
        { "KPageWidgetPrivate", delegate { throw new NotSupportedException(); } },

        { "KParts::PartBasePrivate", delegate { throw new NotSupportedException(); } },
        { "KParts::PartPrivate", delegate { throw new NotSupportedException(); } },
        { "KParts::ReadOnlyPartPrivate", delegate { throw new NotSupportedException(); } },

        { "KPluginFactoryPrivate", delegate { throw new NotSupportedException(); } },
        { "KProcessPrivate", delegate { throw new NotSupportedException(); } },
        { "KSelectActionPrivate", delegate { throw new NotSupportedException(); } },
        { "KService::ServiceTypeAndPreference", delegate { throw new NotSupportedException(); } },
        { "KServiceTypePrivate", delegate { throw new NotSupportedException(); } },
        { "KSycocaEntryPrivate", delegate { throw new NotSupportedException(); } },
        { "KSycocaFactory", delegate { throw new NotSupportedException(); } },
        { "KSycocaFactoryList", delegate { throw new NotSupportedException(); } },

        { "Solid::DeviceInterfacePrivate", delegate { throw new NotSupportedException(); } },
        { "Solid::Networking::Notifier", delegate { throw new NotSupportedException(); } },
        { "Solid::PowerManagement::Notifier", delegate { throw new NotSupportedException(); } },
        { "Solid::StorageAccessPrivate", delegate { throw new NotSupportedException(); } },
        { "Solid::StorageDrivePrivate", delegate { throw new NotSupportedException(); } },
        { "Solid::StorageVolumePrivate", delegate { throw new NotSupportedException(); } },

        { "XEvent", delegate { throw new NotSupportedException(); } },

        { "passwd", delegate { throw new NotSupportedException(); } },
        { "group", delegate { throw new NotSupportedException(); } },

        { "KSharedPtr", (type, data, translator) => translator.CppToCSharp(type.TemplateParameters) },
        { "KFileFilterCombo", delegate(Translator.TypeInfo type, GeneratorData data, Translator translator) {
                            if (data.Smoke->ToString() == "kio") {
                                // we can't reference kimono-kfile, because that itself depends on kio again
                                return "KComboBox";
                            } else {
                                // default behaviour
                                return null;
                            }
                        }},
    };

    List<string> interfaceClasses = new List<string>()
    {
    };

    List<Regex> excludedMethods = new List<Regex>()
    {
        new Regex(@"KCmdLineArgs::init\(int.*"),
        new Regex(@"KPluginFactory::createPartObject\(.*"),
        new Regex(@"KParts::Factory::createPartObject\(.*"),
    };

    List<string> namespacesAsClasses = new List<string>()
    {
        "KDE",
    };

    public IDictionary<string, Type> TypeMap { get { return typeMap; } }
    public IDictionary<string, string> TypeStringMap { get { return typeStringMap; } }
    public IDictionary<string, Translator.TranslateFunc> TypeCodeMap { get { return typeCodeMap; } }

    public IList<string> InterfaceClasses { get { return interfaceClasses; } }

    public IList<Regex> ExcludedMethods { get { return excludedMethods; } }
    public IList<string> NamespacesAsClasses { get { return namespacesAsClasses; } }
}
