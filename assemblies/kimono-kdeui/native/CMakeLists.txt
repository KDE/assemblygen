find_package(Qt4 REQUIRED COMPONENTS QtCore)
find_package(KDE4 REQUIRED)
find_package(Smoke REQUIRED COMPONENTS KDEUi)

# source files
SET(SRC_KIMONO_KDEUI_NATIVE
    kdeui.cpp
    kdehandlers.cpp
)

INCLUDE_DIRECTORIES (${QT_INCLUDES} ${KDE4_INCLUDES} ${SMOKE_INCLUDE_DIR} ${SMOKE_QTCORE_INCLUDE_DIR}
                     ${SMOKE_KDEUI_INCLUDE_DIR}/smoke ../../qyoto-qtcore/native ../../kimono-kdecore/native)

ADD_LIBRARY (kimono-kdeui-native SHARED ${SRC_KIMONO_KDEUI_NATIVE})
TARGET_LINK_LIBRARIES (kimono-kdeui-native qyoto-qtcore-native ${SMOKE_BASE_LIBRARY} ${SMOKE_KDEUI_LIBRARY} ${QT_QTCORE_LIBRARY} ${KDE4_KDEUI_LIBRARY})
set_target_properties(kimono-kdeui-native PROPERTIES VERSION 2.0.0 SOVERSION 2)

INSTALL(TARGETS kimono-kdeui-native LIBRARY DESTINATION lib)
