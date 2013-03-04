find_package(Qt4 REQUIRED COMPONENTS QtCore QtNetwork)
find_package(Smoke REQUIRED COMPONENTS QtNetwork)

# source files
SET(SRC_QYOTO_QTNETWORK_NATIVE
    handlers.cpp
    qtnetwork.cpp
)

INCLUDE_DIRECTORIES (${QT_INCLUDES} ${SMOKE_INCLUDE_DIR} ${SMOKE_QTNETWORK_INCLUDE_DIR}/smoke ../../qyoto-qtcore/native)

ADD_LIBRARY (qyoto-qtnetwork-native SHARED ${SRC_QYOTO_QTNETWORK_NATIVE})
TARGET_LINK_LIBRARIES (qyoto-qtnetwork-native qyoto-qtcore-native ${SMOKE_BASE_LIBRARY} ${SMOKE_QTNETWORK_LIBRARY} ${QT_QTCORE_LIBRARY} ${QT_QTNETWORK_LIBRARY})


INSTALL(TARGETS qyoto-qtnetwork-native LIBRARY DESTINATION ${LIB_INSTALL_DIR}
						  			   ARCHIVE DESTINATION ${LIB_INSTALL_DIR}
						  			   RUNTIME DESTINATION bin)
if (WIN32)
    if (CMAKE_COMPILER_IS_GNUCC)
      # Realign the stack, for compatibility with an older ABI.
      set_target_properties (qyoto-qtnetwork-native PROPERTIES COMPILE_FLAGS -mstackrealign)
    endif (CMAKE_COMPILER_IS_GNUCC)

    # Get rid of the "lib" prefix on archives/DLLs in Windows.
    set_target_properties(qyoto-qtnetwork-native
        PROPERTIES PREFIX "" IMPORT_PREFIX "")
endif (WIN32)
