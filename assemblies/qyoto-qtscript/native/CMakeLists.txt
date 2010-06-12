find_package(Qt4 REQUIRED COMPONENTS QtCore QtScript)
find_package(Smoke REQUIRED COMPONENTS QtScript)

# source files
SET(SRC_QYOTO_QTSCRIPT_NATIVE
    qtscript.cpp
    qtscripthandlers.cpp
)

INCLUDE_DIRECTORIES (${QT_INCLUDES} ${SMOKE_INCLUDE_DIR} ${SMOKE_QTSCRIPT_INCLUDE_DIR}/smoke ../../qyoto-qtcore/native)

ADD_LIBRARY (qyoto-qtscript-native SHARED ${SRC_QYOTO_QTSCRIPT_NATIVE})
TARGET_LINK_LIBRARIES (qyoto-qtscript-native qyoto-qtcore-native ${SMOKE_BASE_LIBRARY} ${SMOKE_QTSCRIPT_LIBRARY} ${QT_QTCORE_LIBRARY} ${QT_QTSCRIPT_LIBRARY})
set_target_properties(qyoto-qtscript-native PROPERTIES VERSION 2.0.0 SOVERSION 2)

INSTALL(TARGETS qyoto-qtscript-native LIBRARY DESTINATION lib)
