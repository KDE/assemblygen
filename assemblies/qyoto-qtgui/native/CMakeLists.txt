find_package(Qt4 REQUIRED COMPONENTS QtCore QtGui)
find_package(Smoke REQUIRED COMPONENTS QtGui)

# source files
SET(SRC_QYOTO_QTGUI_NATIVE
    handlers.cpp
    qtgui.cpp
)

INCLUDE_DIRECTORIES (${QT_INCLUDES} ${SMOKE_INCLUDE_DIR} ${SMOKE_QTGUI_INCLUDE_DIR}/smoke ../../qyoto-qtcore/native)

ADD_LIBRARY (qyoto-qtgui-native SHARED ${SRC_QYOTO_QTGUI_NATIVE})
TARGET_LINK_LIBRARIES (qyoto-qtgui-native qyoto-qtcore-native ${SMOKE_BASE_LIBRARY} ${SMOKE_QTGUI_LIBRARY} ${QT_QTCORE_LIBRARY} ${QT_QTGUI_LIBRARY})


INSTALL(TARGETS qyoto-qtgui-native LIBRARY DESTINATION ${LIB_INSTALL_DIR}
						  		   ARCHIVE DESTINATION ${LIB_INSTALL_DIR}
						  		   RUNTIME DESTINATION bin)
if (WIN32)
	# Realign the stack, for compatibility with an older ABI.
	set_target_properties (qyoto-qtgui-native PROPERTIES COMPILE_FLAGS -mstackrealign)

    # Get rid of the "lib" prefix on archives/DLLs in Windows.
    set_target_properties(qyoto-qtgui-native
        PROPERTIES PREFIX "" IMPORT_PREFIX "")
endif (WIN32)
