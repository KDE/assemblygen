find_package(Qt4 REQUIRED COMPONENTS QtCore)
find_package(KDE4 REQUIRED)
find_package(Smoke REQUIRED COMPONENTS QtCore KDECore)

# source files
SET(SRC_KIMONO_KDECORE_NATIVE
    kimono.cpp
    kdehandlers.cpp
    kpluginfactory_fix.cpp
    kurl_list.cpp
)

INCLUDE_DIRECTORIES (${QT_INCLUDES} ${KDE4_INCLUDES} ${SMOKE_INCLUDE_DIR} ${SMOKE_QTCORE_INCLUDE_DIR} ${SMOKE_KDECORE_INCLUDE_DIR}/smoke ../../qyoto-qtcore/native)

ADD_LIBRARY (kimono-kdecore-native SHARED ${SRC_KIMONO_KDECORE_NATIVE})
TARGET_LINK_LIBRARIES (kimono-kdecore-native qyoto-qtcore-native ${SMOKE_BASE_LIBRARY} ${SMOKE_QTCORE_LIBRARY} ${SMOKE_KDECORE_LIBRARY} ${QT_QTCORE_LIBRARY} ${KDE4_KDECORE_LIBRARY})
set_target_properties(kimono-kdecore-native PROPERTIES VERSION 2.0.0 SOVERSION 2)

INSTALL(TARGETS kimono-kdecore-native LIBRARY DESTINATION lib)
