find_package(Qt4 REQUIRED COMPONENTS QtCore QtXml)
find_package(Smoke REQUIRED COMPONENTS QtXml)

# source files
SET(SRC_QYOTO_QTXML_NATIVE
    qtxml.cpp
)

INCLUDE_DIRECTORIES (${QT_INCLUDES} ${SMOKE_INCLUDE_DIR} ${SMOKE_QTXML_INCLUDE_DIR}/smoke ../../qyoto-qtcore/native)

ADD_LIBRARY (qyoto-qtxml-native SHARED ${SRC_QYOTO_QTXML_NATIVE})
TARGET_LINK_LIBRARIES (qyoto-qtxml-native qyoto-qtcore-native ${SMOKE_BASE_LIBRARY} ${SMOKE_QTXML_LIBRARY} ${QT_QTCORE_LIBRARY} ${QT_QTXML_LIBRARY})
set_target_properties(qyoto-qtxml-native PROPERTIES VERSION 2.0.0 SOVERSION 2)

INSTALL(TARGETS qyoto-qtxml-native LIBRARY DESTINATION ${LIB_INSTALL_DIR}
						  		   ARCHIVE DESTINATION ${LIB_INSTALL_DIR}
						  		   RUNTIME DESTINATION bin)
if (WIN32)
    # Get rid of the "lib" prefix on archives/DLLs in Windows.
    set_target_properties(qyoto-qtxml-native
        PROPERTIES PREFIX "" IMPORT_PREFIX "")
endif (WIN32)
