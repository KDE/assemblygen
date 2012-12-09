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
using System.Collections.Generic;
using System.Text;

// Extension methods would be nice, but 'Smoke' is a struct and would be
// copied every time we use an extension method.
public unsafe partial struct Smoke
{

	public short IDType(string type)
	{
		byte[] bytes = ByteArrayManager.GetCString(type);
		fixed (byte* t = bytes)
		{
			short imax = numTypes;
			short imin = 1;

			while (imax >= imin)
			{
				short icur = (short) ((imin + imax)/2);
				long icmp = ByteArrayManager.strcmp(types[icur].name, t);
				if (icmp == 0)
				{
					return icur;
				}

				if (icmp > 0)
				{
					imax = (short) (icur - 1);
				}
				else
				{
					imin = (short) (icur + 1);
				}
			}

			return 0;
		}
	}

	public short IDClass(string name)
	{
		byte[] bytes = ByteArrayManager.GetCString(name);
		fixed (byte* c = bytes)
		{
			short imax = numClasses;
			short imin = 1;

			while (imax >= imin)
			{
				short icur = (short) ((imin + imax)/2);
				long icmp = ByteArrayManager.strcmp(classes[icur].className, c);
				if (icmp == 0)
				{
					return icur;
				}
				if (icmp > 0)
				{
					imax = (short) (icur - 1);
				}
				else
				{
					imin = (short) (icur + 1);
				}
			}

			return 0;
		}
	}

	public short IDMethodName(string name)
	{
		byte[] bytes = ByteArrayManager.GetCString(name);
		fixed (byte* m = bytes)
		{
			short imax = numMethodNames;
			short imin = 1;

			while (imax >= imin)
			{
				short icur = (short) ((imin + imax)/2);
				long icmp = ByteArrayManager.strcmp(methodNames[icur], m);
				if (icmp == 0)
				{
					return icur;
				}
				if (icmp > 0)
				{
					imax = (short) (icur - 1);
				}
				else
				{
					imin = (short) (icur + 1);
				}
			}

			return 0;
		}
	}

	public short IDMethod(short c, short name)
	{
		short imax = numMethodMaps;
		short imin = 1;

		while (imax >= imin)
		{
			short icur = (short) ((imin + imax)/2);
			int icmp = methodMaps[icur].classId - c;
			if (icmp == 0)
			{
				icmp = methodMaps[icur].name - name;
				if (icmp == 0)
				{
					return icur;
				}
			}
			if (icmp > 0)
			{
				imax = (short) (icur - 1);
			}
			else
			{
				imin = (short) (icur + 1);
			}
		}

		return 0;
	}

	public short FindMungedName(Method* meth)
	{
		short imax = numMethodMaps;
		short imin = 1;

		short c = meth->classId;
		short searchId = (short) (meth - methods);

		while (imax >= imin)
		{
			short icur = (short) ((imin + imax)/2);
			int icmp = methodMaps[icur].classId - c;
			if (icmp == 0)
			{
				// move to the beginning of this class
				while (methodMaps[icur - 1].classId == c)
				{
					icur--;
				}
				// we found the class, let's go hunt for the munged name
				while (methodMaps[icur].classId == c)
				{
					if (methodMaps[icur].method == searchId)
					{
						return methodMaps[icur].name;
					}
					if (this.methodMaps[icur].method < 0)
					{
						for (short* id = this.ambiguousMethodList + (-this.methodMaps[icur].method); *id > 0; id++)
						{
							if (*id == searchId)
							{
								return this.methodMaps[icur].name;
							}
						}
					}
					icur++;
				}
			}
			if (icmp > 0)
			{
				imax = (short) (icur - 1);
			}
			else
			{
				imin = (short) (icur + 1);
			}
		}

		return 0;
	}

	// adapted from QtRuby's findAllMethods()
	public void FindAllMethods(short c, IDictionary<ModuleIndex, string> ret, bool searchSuperClasses)
	{
		Class* klass = classes + c;
		if (klass->external)
		{
			Smoke* smoke = (Smoke*) 0;
			short index = 0;
			if (!Util.GetModuleIndexFromClassName(klass->className, ref smoke, ref index))
			{
				Console.Error.WriteLine("  |--Failed resolving external class {0}", ByteArrayManager.GetString(klass->className));
				return;
			}
			smoke->FindAllMethods(index, ret, true);
			return;
		}

		Smoke* thisPtr;
		fixed (Smoke* smoke = &this)
		{
			thisPtr = smoke; // hackish, but this is the cleanest way that I know to get the 'this' pointer
		}

		short imax = numMethodMaps;
		short imin = 0;
		short methmin = -1, methmax = -1;
		int icmp = -1;
		while (imax >= imin)
		{
			short icur = (short) ((imin + imax)/2);
			icmp = methodMaps[icur].classId - c;
			if (icmp == 0)
			{
				short pos = icur;
				while (icur > 0 && methodMaps[icur - 1].classId == c)
					icur--;
				methmin = icur;
				icur = pos;
				while (icur < imax && methodMaps[icur + 1].classId == c)
					icur++;
				methmax = icur;
				break;
			}
			if (icmp > 0)
				imax = (short) (icur - 1);
			else
				imin = (short) (icur + 1);
		}

		if (icmp != 0)
		{
			Console.Error.WriteLine("WARNING: FindAllMethods failed for class {0}",
			                        ByteArrayManager.GetString(classes[c].className));
			return;
		}

		for (short i = methmin; i <= methmax; i++)
		{
			string mungedName = ByteArrayManager.GetString(methodNames[methodMaps[i].name]);
			short methId = methodMaps[i].method;
			if (methId > 0)
			{
				ret[new ModuleIndex(thisPtr, methId)] = mungedName;
			}
			else
			{
				for (short* overload = ambiguousMethodList + (-methId); *overload > 0; overload++)
				{
					ret[new ModuleIndex(thisPtr, *overload)] = mungedName;
				}
			}
		}
		if (searchSuperClasses)
		{
			for (short* parent = inheritanceList + classes[c].parents; *parent > 0; parent++)
			{
				FindAllMethods(*parent, ret, true);
			}
		}
	}

	// convenience overload
	public Dictionary<ModuleIndex, string> FindAllMethods(short classId, bool searchSuperClasses)
	{
		Dictionary<ModuleIndex, string> ret = new Dictionary<ModuleIndex, string>();
		FindAllMethods(classId, ret, searchSuperClasses);
		return ret;
	}

	public override string ToString()
	{
		return ByteArrayManager.GetString(module_name);
	}

	public string GetMethodSignature(short index)
	{
		Method* meth = methods + index;
		return GetMethodSignature(meth);
	}

	public string GetMethodSignature(Method* meth)
	{
		StringBuilder str = new StringBuilder();
		str.Append(ByteArrayManager.GetString(methodNames[meth->name]));
		str.Append('(');
		this.GetArgs(meth, str);
		str.Append(')');
		if ((meth->flags & (ushort) MethodFlags.mf_const) != 0)
			str.Append(" const");
		return str.ToString();
	}

	public string GetArgs(Method* meth)
	{
		StringBuilder argsBuilder = new StringBuilder();
		GetArgs(meth, argsBuilder);
		return argsBuilder.ToString();
	}

	private void GetArgs(Method* meth, StringBuilder str)
	{
		for (short* typeIndex = this.argumentList + meth->args; *typeIndex > 0;)
		{
			str.Append(ByteArrayManager.GetString(this.types[*typeIndex].name));
			if (*(++typeIndex) > 0)
			{
				str.Append(", ");
			}
		}
	}
}
