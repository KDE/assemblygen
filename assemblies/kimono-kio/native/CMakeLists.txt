find_package(Qt4 REQUIRED COMPONENTS QtCore)
find_package(Smoke REQUIRED COMPONENTS KIO)

# source files
SET(SRC_KIMONO_KIO_NATIVE
    kio.cpp
    kdehandlers.cpp
)

INCLUDE_DIRECTORIES (${QT_INCLUDES} ${KDE4_INCLUDES} ${SMOKE_INCLUDE_DIR} ${SMOKE_QTCORE_INCLUDE_DIR}
                     ${SMOKE_KIO_INCLUDE_DIR}/smoke ../../qyoto-qtcore/native ../../kimono-kdecore/native)

ADD_LIBRARY (kimono-kio-native SHARED ${SRC_KIMONO_KIO_NATIVE})
TARGET_LINK_LIBRARIES (kimono-kio-native qyoto-qtcore-native ${SMOKE_BASE_LIBRARY} ${SMOKE_KIO_LIBRARY} ${QT_QTCORE_LIBRARY} ${KDE4_KIO_LIBRARY})


INSTALL(TARGETS kimono-kio-native LIBRARY DESTINATION lib)
