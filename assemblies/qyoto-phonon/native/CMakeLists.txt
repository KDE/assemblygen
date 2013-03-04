find_package(Qt4 REQUIRED COMPONENTS QtCore)
find_package(Phonon REQUIRED)
find_package(Smoke REQUIRED COMPONENTS Phonon)

# source files
SET(SRC_QYOTO_PHONON_NATIVE
    phononhandlers.cpp
    phonon.cpp
)

INCLUDE_DIRECTORIES (${QT_INCLUDES} ${PHONON_INCLUDES} ${SMOKE_INCLUDE_DIR} ${SMOKE_PHONON_INCLUDE_DIR}/smoke ../../qyoto-qtcore/native)

ADD_LIBRARY (qyoto-phonon-native SHARED ${SRC_QYOTO_PHONON_NATIVE})
TARGET_LINK_LIBRARIES (qyoto-phonon-native qyoto-qtcore-native ${SMOKE_BASE_LIBRARY} ${SMOKE_PHONON_LIBRARY} ${QT_QTCORE_LIBRARY} ${PHONON_LIBS})


INSTALL(TARGETS qyoto-phonon-native LIBRARY DESTINATION ${LIB_INSTALL_DIR}
						  		   ARCHIVE DESTINATION ${LIB_INSTALL_DIR}
						  		   RUNTIME DESTINATION bin)
if (WIN32)
	if (CMAKE_COMPILER_IS_GNUCC)
		# Realign the stack, for compatibility with an older ABI.
		set_target_properties (qyoto-phonon-native PROPERTIES COMPILE_FLAGS -mstackrealign)
	endif (CMAKE_COMPILER_IS_GNUCC)

    # Get rid of the "lib" prefix on archives/DLLs in Windows.
    set_target_properties(qyoto-phonon-native
        PROPERTIES PREFIX "" IMPORT_PREFIX "")
endif (WIN32)
