# - Try to find the mono, mcs, dmcs and gacutil
#
# defines
#
# MONO_FOUND - system has mono, mcs, dmcs and gacutil
# MONO_PATH - where to find 'mono'
# GMCS_PATH - where to find 'dmcs'
# GACUTIL_PATH - where to find 'gacutil'
#
# copyright (c) 2007 Arno Rehn arno@arnorehn.de
#
# Redistribution and use is allowed according to the terms of the GPL license.

FIND_PROGRAM (MONO_EXECUTABLE mono)
FIND_PROGRAM (DMCS_EXECUTABLE dmcs)
FIND_PROGRAM (GACUTIL_EXECUTABLE gacutil)
FIND_PROGRAM (SN_EXECUTABLE sn)

SET (MONO_FOUND FALSE CACHE INTERNAL "")

IF (MONO_EXECUTABLE AND DMCS_EXECUTABLE AND GACUTIL_EXECUTABLE AND SN_EXACUTABLE)
	SET (MONO_FOUND TRUE CACHE INTERNAL "")
ENDIF (MONO_EXECUTABLE AND DMCS_EXECUTABLE AND GACUTIL_EXECUTABLE AND SN_EXACUTABLE)

IF (NOT Mono_FIND_QUIETLY)
    MESSAGE(STATUS "Path of mono: ${MONO_EXECUTABLE}")
    MESSAGE(STATUS "Path of dmcs: ${DMCS_EXECUTABLE}")
    MESSAGE(STATUS "Path of gacutil: ${GACUTIL_EXECUTABLE}")
    MESSAGE(STATUS "Path of sn: ${SN_EXACUTABLE}")
ENDIF (NOT Mono_FIND_QUIETLY)

IF (NOT MONO_FOUND)
	IF (Mono_FIND_REQUIRED)
		MESSAGE(FATAL_ERROR "Could not find one or more of the following programs: mono, dmcs, gacutil, sn")
	ENDIF (Mono_FIND_REQUIRED)
ENDIF (NOT MONO_FOUND)

MARK_AS_ADVANCED(MONO_EXECUTABLE DMCS_EXECUTABLE GACUTIL_EXECUTABLE SN_EXACUTABLE)
