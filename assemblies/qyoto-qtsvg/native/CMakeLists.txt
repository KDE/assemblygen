find_package(Qt4 REQUIRED COMPONENTS QtCore QtSvg)
find_package(Smoke REQUIRED COMPONENTS QtSvg)

# source files
SET(SRC_QYOTO_QTSVG_NATIVE
    qtsvg.cpp
)

INCLUDE_DIRECTORIES (${QT_INCLUDES} ${SMOKE_INCLUDE_DIR} ${SMOKE_QTSVG_INCLUDE_DIR}/smoke ../../qyoto-qtcore/native)

ADD_LIBRARY (qyoto-qtsvg-native SHARED ${SRC_QYOTO_QTSVG_NATIVE})
TARGET_LINK_LIBRARIES (qyoto-qtsvg-native qyoto-qtcore-native ${SMOKE_BASE_LIBRARY} ${SMOKE_QTSVG_LIBRARY} ${QT_QTCORE_LIBRARY} ${QT_QTSVG_LIBRARY})


INSTALL(TARGETS qyoto-qtsvg-native LIBRARY DESTINATION ${LIB_INSTALL_DIR}
						  		   ARCHIVE DESTINATION ${LIB_INSTALL_DIR}
						  		   RUNTIME DESTINATION bin)
if (WIN32)
    if (CMAKE_COMPILER_IS_GNUCC)
        # Realign the stack, for compatibility with an older ABI.
        set_target_properties (qyoto-qtsvg-native PROPERTIES COMPILE_FLAGS -mstackrealign)
    endif (CMAKE_COMPILER_IS_GNUCC)

    # Get rid of the "lib" prefix on archives/DLLs in Windows.
    set_target_properties(qyoto-qtsvg-native
        PROPERTIES PREFIX "" IMPORT_PREFIX "")
endif (WIN32)
