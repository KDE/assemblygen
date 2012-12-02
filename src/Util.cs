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
using System.CodeDom;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;
using System.Web.Util;

public static class Util
{
	[DllImport("assemblygen-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern unsafe bool GetModuleIndexFromClassName(byte* name, ref Smoke* smoke, ref short index);

	[DllImport("assemblygen-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern bool IsDerivedFrom(string className, string baseClassName);

	[DllImport("assemblygen-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.U1)]
	public static extern unsafe bool IsDerivedFrom(byte* className, byte* baseClassName);

	public static bool IsQObject(string className)
	{
		return IsDerivedFrom(className, "QObject");
	}

	public static unsafe bool IsQObject(Smoke.Class* klass)
	{
		return IsDerivedFrom(ByteArrayManager.GetString(klass->className), "QObject");
	}

	public static bool IsPrimitiveType(string type)
	{
		type = type.Replace("unsigned ", "u");
		return type == "char" || type == "uchar" || type == "short" || type == "ushort" || type == "int" || type == "uint"
		       || type == "long" || type == "long long" || type == "ulong" || type == "ulong long" || type == "float" ||
		       type == "double"
		       || type == "bool" || type == "void" || type == "qreal" || type == "QString";
	}

	public static string StackItemFieldFromType(Type type)
	{
		if (type == typeof(bool))
			return "s_bool";
		if (type == typeof(char))
			return "s_char";
		if (type == typeof(byte))
			return "s_uchar";
		if (type == typeof(short))
			return "s_short";
		if (type == typeof(ushort))
			return "s_ushort";
		if (type == typeof(int))
			return "s_int";
		if (type == typeof(uint))
			return "s_uint";
		if (type == typeof(long))
			return "s_long";
		if (type == typeof(ulong))
			return "s_ulong";
		if (type == typeof(float))
			return "s_float";
		if (type == typeof(double))
			return "s_double";
		return "s_class";
	}

	public static List<string> SplitUnenclosed(string input, char delimeter, char open, char close)
	{
		int enclosed = 0;
		int lastDelimeter = -1;
		List<string> ret = new List<string>();
		for (int i = 0; i < input.Length; i++)
		{
			char c = input[i];
			if (c == open)
			{
				enclosed++;
			}
			else if (c == close)
			{
				enclosed--;
			}
			else if (c == delimeter && enclosed == 0)
			{
				ret.Add(input.Substring(lastDelimeter + 1, i - lastDelimeter - 1));
				lastDelimeter = i;
			}
		}
		ret.Add(input.Substring(lastDelimeter + 1));
		return ret;
	}

	public static unsafe Stack<KeyValuePair<Smoke.ModuleIndex, string>> GetAbstractMethods(Smoke* smoke, short classId)
	{
		Dictionary<Smoke.ModuleIndex, string> methods =
			new Dictionary<Smoke.ModuleIndex, string>(SmokeMethodEqualityComparer.AbstractRespectingComparer);
		SmokeMethodEqualityComparer defaultComparer = SmokeMethodEqualityComparer.DefaultEqualityComparer;

		smoke->FindAllMethods(classId, methods, true);
		var abstractMethods = new Stack<KeyValuePair<Smoke.ModuleIndex, string>>();

		foreach (KeyValuePair<Smoke.ModuleIndex, string> pair in methods)
		{
			Smoke.Method* meth = pair.Key.smoke->methods + pair.Key.index;
			if ((meth->flags & (ushort) Smoke.MethodFlags.mf_purevirtual) == 0)
			{
				// only compare pure-virtuals
				continue;
			}
			abstractMethods.Push(pair);

			foreach (KeyValuePair<Smoke.ModuleIndex, string> other in methods)
			{
				// Break if we encounter our original Index. Anything after this one will be further up in the
				// hierarchy and thus can't override anything.
				if (pair.Key == other.Key)
					break;

				Smoke.Method* otherMeth = other.Key.smoke->methods + other.Key.index;
				if (defaultComparer.Equals(pair.Key, other.Key))
				{
					if ((otherMeth->flags & (ushort) Smoke.MethodFlags.mf_purevirtual) == 0)
					{
						// overriden with implementation
						abstractMethods.Pop();
					}
					break;
				}
			}
		}

		return abstractMethods;
	}

	public static unsafe bool IsClassAbstract(Smoke* smoke, short classId)
	{
		return GetAbstractMethods(smoke, classId).Count > 0;
	}

	public static void FormatComment(string docs, CodeTypeMember cmp, bool obsolete = false, string tag = "summary")
	{
		StringBuilder obsoleteMessageBuilder = new StringBuilder();
		cmp.Comments.Add(new CodeCommentStatement(string.Format("<{0}>", tag), true));
		foreach (string line in HtmlEncoder.HtmlEncode(docs).Split(Environment.NewLine.ToCharArray(), StringSplitOptions.None))
		{
			cmp.Comments.Add(new CodeCommentStatement(string.Format("<para>{0}</para>", line), true));
			if (obsolete && (line.Contains("instead") || line.Contains("deprecated")))
			{
				obsoleteMessageBuilder.Append(HtmlEncoder.HtmlDecode(line));
				obsoleteMessageBuilder.Append(' ');
			}
		}
		cmp.Comments.Add(new CodeCommentStatement(string.Format("</{0}>", tag), true));
		if (obsolete)
		{
			if (obsoleteMessageBuilder.Length > 0)
			{
				obsoleteMessageBuilder.Remove(obsoleteMessageBuilder.Length - 1, 1);				
			}
			CodeTypeReference obsoleteAttribute = new CodeTypeReference(typeof(ObsoleteAttribute));
			CodePrimitiveExpression obsoleteMessage = new CodePrimitiveExpression(obsoleteMessageBuilder.ToString());
			cmp.CustomAttributes.Add(new CodeAttributeDeclaration(obsoleteAttribute, new CodeAttributeArgument(obsoleteMessage)));
		}
	}
}
