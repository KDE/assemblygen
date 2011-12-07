find_package(Qt4 REQUIRED COMPONENTS QtCore QtXmlPatterns)
find_package(Smoke REQUIRED COMPONENTS QtXmlPatterns)

# source files
SET(SRC_QYOTO_QTXMLPATTERNS_NATIVE
    qtxmlpatterns.cpp
)

INCLUDE_DIRECTORIES (${QT_INCLUDES} ${SMOKE_INCLUDE_DIR} ${SMOKE_QTXMLPATTERNS_INCLUDE_DIR}/smoke ../../qyoto-qtcore/native)

ADD_LIBRARY (qyoto-qtxmlpatterns-native SHARED ${SRC_QYOTO_QTXMLPATTERNS_NATIVE})
TARGET_LINK_LIBRARIES (qyoto-qtxmlpatterns-native qyoto-qtcore-native ${SMOKE_BASE_LIBRARY} ${SMOKE_QTXMLPATTERNS_LIBRARY} ${QT_QTCORE_LIBRARY} ${QT_QTXMLPATTERNS_LIBRARY})
set_target_properties(qyoto-qtxmlpatterns-native PROPERTIES VERSION 2.0.0 SOVERSION 2)

INSTALL(TARGETS qyoto-qtxmlpatterns-native LIBRARY DESTINATION ${LIB_INSTALL_DIR}
						  				   ARCHIVE DESTINATION ${LIB_INSTALL_DIR}
						  				   RUNTIME DESTINATION lib)