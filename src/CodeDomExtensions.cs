/*
    Generator for .NET assemblies utilizing SMOKE libraries
    Copyright (C) 2009, 2010 Arno Rehn <arno@arnorehn.de>

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
using System.Text;
using System.CodeDom;

public static class CodeDomExtensions {

    public static bool TypeEquals(this CodeTypeReference self, CodeTypeReference other) {
        if (self.BaseType != other.BaseType || self.TypeArguments.Count != other.TypeArguments.Count)
            return false;

        for (int i = 0; i < self.TypeArguments.Count; i++) {
            if (!self.TypeArguments[i].TypeEquals(other.TypeArguments[i]))
                return false;
        }
        return true;
    }

    public static string GetStringRepresentation(this CodeTypeReference self) {
        StringBuilder ret = new StringBuilder(self.BaseType);
        if (self.TypeArguments.Count > 0) {
            ret.Append('<');
            for (int i = 0; i < self.TypeArguments.Count; i++) {
                if (i > 0) ret.Append(", ");
                ret.Append(self.TypeArguments[i].GetStringRepresentation());
            }
            ret.Append('>');
        }
        return ret.ToString();
    }

    public static bool HasMethod(this CodeTypeDeclaration self, CodeMemberMethod method) {
        foreach (CodeTypeMember member in self.Members) {
            if (!(member is CodeMemberMethod) || member.Name != method.Name)
                continue;

            // now check the parameters
            CodeMemberMethod currentMeth = (CodeMemberMethod) member;
            if (currentMeth.Parameters.Count != method.Parameters.Count)
                continue;
            bool continueOuter = false;
            for (int i = 0; i < method.Parameters.Count; i++) {
                if (!method.Parameters[i].Type.TypeEquals(currentMeth.Parameters[i].Type) || method.Parameters[i].Direction != currentMeth.Parameters[i].Direction) {
                    continueOuter = true;
                    break;
                }
            }
            if (continueOuter)
                continue;
            return true;
        }
        return false;
    }

    public static bool ParametersEqual(this CodeMemberMethod self, CodeMemberMethod method) {
        if (method.Parameters.Count != self.Parameters.Count)
            return false;
        for (int i = 0; i < self.Parameters.Count; i++) {
            if (!self.Parameters[i].Type.TypeEquals(method.Parameters[i].Type) || self.Parameters[i].Direction != method.Parameters[i].Direction) {
                return false;
            }
        }
        return true;
    }
}
