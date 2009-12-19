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
using System.Runtime.InteropServices;

unsafe partial struct Smoke {
    public byte *module_name;

    public enum EnumOperation {
        EnumNew,
        EnumDelete,
        EnumFromLong,
        EnumToLong
    }

    /**
     * Describe one index in a given module.
     */
    public struct ModuleIndex {
        public Smoke* smoke;
        public short index;
    };
    
    /**
     * A ModuleIndex with both fields set to 0.
     */
    public ModuleIndex NullModuleIndex;

    public enum ClassFlags {
        cf_constructor = 0x01,  // has a constructor
        cf_deepcopy = 0x02,     // has copy constructor
        cf_virtual = 0x04,      // has virtual destructor
        cf_undefined = 0x10     // defined elsewhere
    }
    
    /**
     * Describe one class.
     */
    public struct Class {
        public byte *className;  // Name of the class
        [MarshalAs(UnmanagedType.U1)]
        public bool external;      // Whether the class is in another module
        public short parents;      // Index into inheritanceList
        public IntPtr classFn;    // Calls any method in the class
        public IntPtr enumFn;      // Handles enum pointers
        public ushort flags;   // ClassFlags
        public uint size;
    };

    public enum MethodFlags {
        mf_static = 0x01,
        mf_const = 0x02,
        mf_copyctor = 0x04,  // Copy constructor
        mf_internal = 0x08,   // For internal use only
        mf_enum = 0x10,   // An enum value
        mf_ctor = 0x20,
        mf_dtor = 0x40,
        mf_protected = 0x80,
        mf_attribute = 0x100,
        mf_property = 0x200,
        mf_virtual = 0x400,
        mf_purevirtual = 0x800,
        mf_signal = 0x1000, // method is a signal
        mf_slot = 0x2000   // method is a slot
    }
    
    /**
     * Describe one method of one class.
     */
    public struct Method {
        public short classId;      // Index into classes
        public short name;     // Index into methodNames; real name
        public short args;     // Index into argumentList
        public byte numArgs;  // Number of arguments
        public ushort flags;   // MethodFlags (const/static/etc...)
        public short ret;      // Index into types for the return type
        public short method;       // Passed to Class.classFn, to call method
    };

    /**
     * One MethodMap entry maps the munged method prototype
     * to the Method entry.
     *
     * The munging works this way:
     * $ is a plain scalar
     * # is an object
     * ? is a non-scalar (reference to array or hash, undef)
     *
     * e.g. QApplication(int &, char **) becomes QApplication$?
     */
    public struct MethodMap {
        public short classId;      // Index into classes
        public short name;     // Index into methodNames; munged name
        public short method;       // Index into methods
    }

    public enum TypeFlags {
        // The first 4 bits indicate the TypeId value, i.e. which field
        // of the StackItem union is used.
        tf_elem = 0x0F,

        // Always only one of the next three flags should be set
        tf_stack = 0x10,    // Stored on the stack, 'type'
        tf_ptr = 0x20,      // Pointer, 'type*'
        tf_ref = 0x30,      // Reference, 'type&'
        // Can | whatever ones of these apply
        tf_const = 0x40     // const argument
    }
    /**
     * One Type entry is one argument type needed by a method.
     * Type entries are shared, there is only one entry for "int" etc.
     */
    public struct Type {
        public byte *name;   // Stringified type name
        public short classId;      // Index into classes. -1 for none
        public ushort flags;   // TypeFlags
    }

    public enum TypeId {
        t_voidp,
        t_bool,
        t_char,
        t_uchar,
        t_short,
        t_ushort,
        t_int,
        t_uint,
        t_long,
        t_ulong,
        t_float,
        t_double,
        t_enum,
        t_class,
        t_last      // number of pre-defined types
    }

    // Passed to constructor
    /**
     * The classes array defines every class for this module
     */
    public Class *classes;
    public short numClasses;

    /**
     * The methods array defines every method in every class for this module
     */
    public Method *methods;
    public short numMethods;

    /**
     * methodMaps maps the munged method prototypes
     * to the methods entries.
     */
    public MethodMap *methodMaps;
    public short numMethodMaps;

    /**
     * Array of method names, for Method.name and MethodMap.name
     */
    public byte **methodNames;
    public short numMethodNames;

    /**
     * List of all types needed by the methods (arguments and return values)
     */
    public Type *types;
    public short numTypes;

    /**
     * Groups of Indexes (0 separated) used as super class lists.
     * For classes with super classes: Class.parents = index into this array.
     */
    public short *inheritanceList;
    /**
     * Groups of type IDs (0 separated), describing the types of argument for a method.
     * Method.args = index into this array.
     */
    public short *argumentList;
    /**
     * Groups of method prototypes with the same number of arguments, but different types.
     * Used to resolve overloading.
     */
    public short *ambiguousMethodList;
    /**
     * Function used for casting from/to the classes defined by this module.
     */
    public IntPtr castFn;
}
