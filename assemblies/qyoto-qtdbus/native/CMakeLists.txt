find_package(Qt4 REQUIRED COMPONENTS QtCore QtDBus)
find_package(Smoke REQUIRED COMPONENTS QtDBus)

# source files
SET(SRC_QYOTO_QTDBUS_NATIVE
    handlers.cpp
    qtdbus.cpp
    qdbus_interop.cpp
)

INCLUDE_DIRECTORIES (${QT_INCLUDES} ${SMOKE_INCLUDE_DIR} ${SMOKE_QTDBUS_INCLUDE_DIR}/smoke ../../qyoto-qtcore/native)

ADD_LIBRARY (qyoto-qtdbus-native SHARED ${SRC_QYOTO_QTDBUS_NATIVE})
TARGET_LINK_LIBRARIES (qyoto-qtdbus-native qyoto-qtcore-native ${SMOKE_BASE_LIBRARY} ${SMOKE_QTDBUS_LIBRARY} ${QT_QTCORE_LIBRARY} ${QT_QTDBUS_LIBRARY})
set_target_properties(qyoto-qtdbus-native PROPERTIES VERSION 2.0.0 SOVERSION 2)

INSTALL(TARGETS qyoto-qtdbus-native LIBRARY DESTINATION ${LIB_INSTALL_DIR}
						  		   ARCHIVE DESTINATION ${LIB_INSTALL_DIR}
						  		   RUNTIME DESTINATION bin)
if (WIN32)
    # Get rid of the "lib" prefix on archives/DLLs in Windows.
    set_target_properties(qyoto-qtdbus-native
        PROPERTIES PREFIX "" IMPORT_PREFIX "")
endif (WIN32)
