find_package(Qt4 REQUIRED COMPONENTS QtCore QtUiTools)
find_package(Smoke REQUIRED COMPONENTS QtUiTools)

# source files
SET(SRC_QYOTO_QTUITOOLS_NATIVE
    qtuitools.cpp
)

INCLUDE_DIRECTORIES (${QT_INCLUDES} ${SMOKE_INCLUDE_DIR} ${SMOKE_QTUITOOLS_INCLUDE_DIR}/smoke ../../qyoto-qtcore/native)

ADD_LIBRARY (qyoto-qtuitools-native SHARED ${SRC_QYOTO_QTUITOOLS_NATIVE})
TARGET_LINK_LIBRARIES (qyoto-qtuitools-native qyoto-qtcore-native ${SMOKE_BASE_LIBRARY} ${SMOKE_QTUITOOLS_LIBRARY} ${QT_QTCORE_LIBRARY} ${QT_QTUITOOLS_LIBRARY})
set_target_properties(qyoto-qtuitools-native PROPERTIES VERSION 2.0.0 SOVERSION 2)

INSTALL(TARGETS qyoto-qtuitools-native LIBRARY DESTINATION lib)
