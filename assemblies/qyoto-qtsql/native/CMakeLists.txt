find_package(Qt4 REQUIRED COMPONENTS QtCore QtSql)
find_package(Smoke REQUIRED COMPONENTS QtSql)

# source files
SET(SRC_QYOTO_QTSQL_NATIVE
    qtsql.cpp
)

INCLUDE_DIRECTORIES (${QT_INCLUDES} ${SMOKE_INCLUDE_DIR} ${SMOKE_QTSQL_INCLUDE_DIR}/smoke ../../qyoto-qtcore/native)

ADD_LIBRARY (qyoto-qtsql-native SHARED ${SRC_QYOTO_QTSQL_NATIVE})
TARGET_LINK_LIBRARIES (qyoto-qtsql-native qyoto-qtcore-native ${SMOKE_BASE_LIBRARY} ${SMOKE_QTSQL_LIBRARY} ${QT_QTCORE_LIBRARY} ${QT_QTSQL_LIBRARY})


INSTALL(TARGETS qyoto-qtsql-native LIBRARY DESTINATION ${LIB_INSTALL_DIR}
						  		   ARCHIVE DESTINATION ${LIB_INSTALL_DIR}
						  		   RUNTIME DESTINATION bin)
if (WIN32)
    if (CMAKE_COMPILER_IS_GNUCC)
        # Realign the stack, for compatibility with an older ABI.
        set_target_properties (qyoto-qtsql-native PROPERTIES COMPILE_FLAGS -mstackrealign)
    endif (CMAKE_COMPILER_IS_GNUCC)

    # Get rid of the "lib" prefix on archives/DLLs in Windows.
    set_target_properties(qyoto-qtsql-native
        PROPERTIES PREFIX "" IMPORT_PREFIX "")
endif (WIN32)
