find_package(Qt4 REQUIRED COMPONENTS QtCore QtWebKit)
find_package(Smoke REQUIRED COMPONENTS QtWebKit)

# source files
SET(SRC_QYOTO_QTWEBKIT_NATIVE
    qtwebkit.cpp
    qtwebkithandlers.cpp
)

INCLUDE_DIRECTORIES (${QT_INCLUDES} ${SMOKE_INCLUDE_DIR} ${SMOKE_QTWEBKIT_INCLUDE_DIR}/smoke ../../qyoto-qtcore/native)

ADD_LIBRARY (qyoto-qtwebkit-native SHARED ${SRC_QYOTO_QTWEBKIT_NATIVE})
TARGET_LINK_LIBRARIES (qyoto-qtwebkit-native qyoto-qtcore-native ${SMOKE_BASE_LIBRARY} ${SMOKE_QTWEBKIT_LIBRARY} ${QT_QTCORE_LIBRARY} ${QT_QTWEBKIT_LIBRARY})
set_target_properties(qyoto-qtwebkit-native PROPERTIES VERSION 2.0.0 SOVERSION 2)

INSTALL(TARGETS qyoto-qtwebkit-native LIBRARY DESTINATION ${LIB_INSTALL_DIR}
						  			  ARCHIVE DESTINATION ${LIB_INSTALL_DIR}
						  			  RUNTIME DESTINATION bin)
if (WIN32)
	# Realign the stack, for compatibility with an older ABI.
	set_target_properties (qyoto-qtwebkit-native PROPERTIES COMPILE_FLAGS -mstackrealign)

    # Get rid of the "lib" prefix on archives/DLLs in Windows.
    set_target_properties(qyoto-qtwebkit-native
        PROPERTIES PREFIX "" IMPORT_PREFIX "")
endif (WIN32)
