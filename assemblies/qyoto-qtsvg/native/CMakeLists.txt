find_package(Qt4 REQUIRED COMPONENTS QtCore QtSvg)
find_package(Smoke REQUIRED COMPONENTS QtSvg)

# source files
SET(SRC_QYOTO_QTSVG_NATIVE
    qtsvg.cpp
)

INCLUDE_DIRECTORIES (${QT_INCLUDES} ${SMOKE_INCLUDE_DIR} ${SMOKE_QTSVG_INCLUDE_DIR}/smoke ../../qyoto-qtcore/native)

ADD_LIBRARY (qyoto-qtsvg-native SHARED ${SRC_QYOTO_QTSVG_NATIVE})
TARGET_LINK_LIBRARIES (qyoto-qtsvg-native qyoto-qtcore-native ${SMOKE_BASE_LIBRARY} ${SMOKE_QTSVG_LIBRARY} ${QT_QTCORE_LIBRARY} ${QT_QTSVG_LIBRARY})
set_target_properties(qyoto-qtsvg-native PROPERTIES VERSION 2.0.0 SOVERSION 2)

INSTALL(TARGETS qyoto-qtsvg-native LIBRARY DESTINATION lib)
