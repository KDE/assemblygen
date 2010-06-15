find_package(Qt4 REQUIRED COMPONENTS QtCore)
find_package(Smoke REQUIRED COMPONENTS KTextEditor)

# source files
SET(SRC_KIMONO_KTEXTEDITOR_NATIVE
    ktexteditor.cpp
)

INCLUDE_DIRECTORIES (${QT_INCLUDES} ${KDE4_INCLUDES} ${SMOKE_INCLUDE_DIR} ${SMOKE_QTCORE_INCLUDE_DIR}
                     ${SMOKE_KTEXTEDITOR_INCLUDE_DIR}/smoke ../../qyoto-qtcore/native ../../kimono-kdecore/native)

ADD_LIBRARY (kimono-ktexteditor-native SHARED ${SRC_KIMONO_KTEXTEDITOR_NATIVE})
TARGET_LINK_LIBRARIES (kimono-ktexteditor-native qyoto-qtcore-native ${SMOKE_BASE_LIBRARY} ${SMOKE_KTEXTEDITOR_LIBRARY} ${QT_QTCORE_LIBRARY} ${KDE4_KTEXTEDITOR_LIBRARY})
set_target_properties(kimono-ktexteditor-native PROPERTIES VERSION 2.0.0 SOVERSION 2)

INSTALL(TARGETS kimono-ktexteditor-native LIBRARY DESTINATION lib)
