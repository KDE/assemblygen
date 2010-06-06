find_package(Qt4 REQUIRED COMPONENTS QtCore)
find_package(KDE4 REQUIRED)
find_package(Smoke REQUIRED COMPONENTS Solid)

# source files
SET(SRC_KIMONO_SOLID_NATIVE
    handlers.cpp
    solid.cpp
)

INCLUDE_DIRECTORIES (${QT_INCLUDES} ${KDE4_INCLUDES} ${SMOKE_INCLUDE_DIR} ${SMOKE_SOLID_INCLUDE_DIR}/smoke ../../qyoto-qtcore/native)

ADD_LIBRARY (kimono-solid-native SHARED ${SRC_KIMONO_SOLID_NATIVE})
TARGET_LINK_LIBRARIES (kimono-solid-native qyoto-qtcore-native ${SMOKE_BASE_LIBRARY} ${SMOKE_SOLID_LIBRARY} ${QT_QTCORE_LIBRARY} ${KDE4_SOLID_LIBRARY})
set_target_properties(kimono-solid-native PROPERTIES VERSION 2.0.0 SOVERSION 2)

INSTALL(TARGETS kimono-solid-native LIBRARY DESTINATION lib)
