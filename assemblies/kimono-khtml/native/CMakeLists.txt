find_package(Qt4 REQUIRED COMPONENTS QtCore)
find_package(Smoke REQUIRED COMPONENTS khtml)

# source files
SET(SRC_KIMONO_KHTML_NATIVE
    khtml.cpp
)

INCLUDE_DIRECTORIES (${QT_INCLUDES} ${KDE4_INCLUDES} ${SMOKE_INCLUDE_DIR} ${SMOKE_QTCORE_INCLUDE_DIR}
                     ${SMOKE_KHTML_INCLUDE_DIR}/smoke ../../qyoto-qtcore/native ../../kimono-kdecore/native)

ADD_LIBRARY (kimono-khtml-native SHARED ${SRC_KIMONO_KHTML_NATIVE})
TARGET_LINK_LIBRARIES (kimono-khtml-native qyoto-qtcore-native ${SMOKE_BASE_LIBRARY} ${SMOKE_KHTML_LIBRARY} ${QT_QTCORE_LIBRARY} ${KDE4_KHTML_LIBRARY})
set_target_properties(kimono-khtml-native PROPERTIES VERSION 2.0.0 SOVERSION 2)

INSTALL(TARGETS kimono-khtml-native LIBRARY DESTINATION lib)
