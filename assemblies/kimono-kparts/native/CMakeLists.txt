find_package(Qt4 REQUIRED COMPONENTS QtCore)
find_package(Smoke REQUIRED COMPONENTS KParts)

# source files
SET(SRC_KIMONO_KPARTS_NATIVE
    kparts.cpp
    kdehandlers.cpp
)

INCLUDE_DIRECTORIES (${QT_INCLUDES} ${KDE4_INCLUDES} ${SMOKE_INCLUDE_DIR} ${SMOKE_QTCORE_INCLUDE_DIR}
                     ${SMOKE_KPARTS_INCLUDE_DIR}/smoke ../../qyoto-qtcore/native ../../kimono-kdecore/native)

ADD_LIBRARY (kimono-kparts-native SHARED ${SRC_KIMONO_KPARTS_NATIVE})
TARGET_LINK_LIBRARIES (kimono-kparts-native qyoto-qtcore-native ${SMOKE_BASE_LIBRARY} ${SMOKE_KPARTS_LIBRARY} ${QT_QTCORE_LIBRARY} ${KDE4_KPARTS_LIBRARY})
set_target_properties(kimono-kparts-native PROPERTIES VERSION 2.0.0 SOVERSION 2)

INSTALL(TARGETS kimono-kparts-native LIBRARY DESTINATION lib)
