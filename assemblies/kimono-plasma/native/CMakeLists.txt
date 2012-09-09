find_package(Qt4 REQUIRED COMPONENTS QtCore)
find_package(Smoke REQUIRED COMPONENTS Plasma)

# source files
SET(SRC_KIMONO_PLASMA_NATIVE
    plasma.cpp
    plasmahandlers.cpp
)

INCLUDE_DIRECTORIES (${QT_INCLUDES} ${KDE4_INCLUDES} ${SMOKE_INCLUDE_DIR} ${SMOKE_QTCORE_INCLUDE_DIR}
                     ${SMOKE_PLASMA_INCLUDE_DIR}/smoke ../../qyoto-qtcore/native ../../kimono-kdecore/native)

ADD_LIBRARY (kimono-plasma-native SHARED ${SRC_KIMONO_PLASMA_NATIVE})
TARGET_LINK_LIBRARIES (kimono-plasma-native qyoto-qtcore-native ${SMOKE_BASE_LIBRARY} ${SMOKE_PLASMA_LIBRARY} ${QT_QTCORE_LIBRARY} ${KDE4_PLASMA_LIBRARY})


INSTALL(TARGETS kimono-plasma-native LIBRARY DESTINATION lib)
