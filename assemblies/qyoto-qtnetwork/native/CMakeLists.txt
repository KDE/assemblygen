find_package(Qt4 REQUIRED COMPONENTS QtCore QtNetwork)
find_package(Smoke REQUIRED COMPONENTS QtNetwork)

# source files
SET(SRC_QYOTO_QTNETWORK_NATIVE
    handlers.cpp
    qtnetwork.cpp
)

INCLUDE_DIRECTORIES (${QT_INCLUDES} ${SMOKE_INCLUDE_DIR} ${SMOKE_QTNETWORK_INCLUDE_DIR}/smoke ../../qyoto-qtcore/native)

ADD_LIBRARY (qyoto-qtnetwork-native SHARED ${SRC_QYOTO_QTNETWORK_NATIVE})
TARGET_LINK_LIBRARIES (qyoto-qtnetwork-native qyoto-qtcore-native ${SMOKE_BASE_LIBRARY} ${SMOKE_QTNETWORK_LIBRARY} ${QT_QTCORE_LIBRARY} ${QT_QTNETWORK_LIBRARY})
set_target_properties(qyoto-qtnetwork-native PROPERTIES VERSION 2.0.0 SOVERSION 2)

INSTALL(TARGETS qyoto-qtnetwork-native LIBRARY DESTINATION ${LIB_INSTALL_DIR}
						  			   ARCHIVE DESTINATION ${LIB_INSTALL_DIR}
						  			   RUNTIME DESTINATION lib)