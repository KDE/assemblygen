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

public class KimonoTranslator : ICustomTranslator {

    Dictionary<string, Type> typeMap = new Dictionary<string, Type>()
    {
    };

    Dictionary<string, string> typeStringMap = new Dictionary<string, string>()
    {
    };

    Dictionary<string, Translator.TranslateFunc> typeCodeMap = new Dictionary<string, Translator.TranslateFunc>()
    {
        { "KSharedPtr", delegate(Translator.TypeInfo type) {
                            type.Name = type.TemplateParameters;
                            type.TemplateParameters = string.Empty;
                            return null;
                        }},
    };

    List<string> interfaceClasses = new List<string>()
    {
    };

    List<Regex> excludedMethods = new List<Regex>()
    {
        new Regex(@"KCmdLineArgs::init\(int.*"),
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
