find_package(Qt4 REQUIRED COMPONENTS QtCore)
find_package(Smoke REQUIRED COMPONENTS KFile)

# source files
SET(SRC_KIMONO_KFILE_NATIVE
    kfile.cpp
)

INCLUDE_DIRECTORIES (${QT_INCLUDES} ${KDE4_INCLUDES} ${SMOKE_INCLUDE_DIR} ${SMOKE_QTCORE_INCLUDE_DIR}
                     ${SMOKE_KFILE_INCLUDE_DIR}/smoke ../../qyoto-qtcore/native ../../kimono-kdecore/native)

ADD_LIBRARY (kimono-kfile-native SHARED ${SRC_KIMONO_KFILE_NATIVE})
TARGET_LINK_LIBRARIES (kimono-kfile-native qyoto-qtcore-native ${SMOKE_BASE_LIBRARY} ${SMOKE_KFILE_LIBRARY} ${QT_QTCORE_LIBRARY} ${KDE4_KFILE_LIBRARY})
set_target_properties(kimono-kfile-native PROPERTIES VERSION 2.0.0 SOVERSION 2)

INSTALL(TARGETS kimono-kfile-native LIBRARY DESTINATION lib)
