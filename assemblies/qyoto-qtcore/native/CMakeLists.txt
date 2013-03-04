find_package(Qt4 REQUIRED COMPONENTS QtCore)
find_package(Smoke REQUIRED COMPONENTS QtCore)

# Enable QtDBus if present.
if (QT_QTDBUS_LIBRARY)
    add_definitions(-DUSING_QTDBUS)
endif (QT_QTDBUS_LIBRARY)

QT4_WRAP_CPP(qyoto_HEADERS_MOC "qyoto.h")

# source files
SET(SRC_QYOTO_QTCORE_NATIVE
    callbacks.cpp
    emitsignal.cpp
    handlers.cpp
    invokeslot.cpp
    methodcall.cpp
    methodreturnvalue.cpp
    modelfunctions.cpp
    qmetatype_interop.cpp
    qobject_interop.cpp
    qvariant_interop.cpp
    qyoto.cpp
    ${qyoto_HEADERS_MOC}
    qyotosmokebinding.cpp
    signalreturnvalue.cpp
    slotreturnvalue.cpp
    util.cpp
    virtualmethodcall.cpp
    virtualmethodreturnvalue.cpp
)

INCLUDE_DIRECTORIES (${QT_INCLUDES} ${SMOKE_INCLUDE_DIR} ${SMOKE_QTCORE_INCLUDE_DIR}/smoke)

ADD_LIBRARY (qyoto-qtcore-native SHARED ${SRC_QYOTO_QTCORE_NATIVE})
TARGET_LINK_LIBRARIES (qyoto-qtcore-native ${SMOKE_BASE_LIBRARY} ${SMOKE_QTCORE_LIBRARY} ${QT_QTCORE_LIBRARY})
set_target_properties(qyoto-qtcore-native PROPERTIES COMPILE_DEFINITIONS QYOTO_BUILDING)

INSTALL(TARGETS qyoto-qtcore-native LIBRARY DESTINATION ${LIB_INSTALL_DIR}
						  			ARCHIVE DESTINATION ${LIB_INSTALL_DIR}
						  			RUNTIME DESTINATION bin)
if (WIN32)
    if (CMAKE_COMPILER_IS_GNUCC)
        # Realign the stack, for compatibility with an older ABI.
        set_target_properties (qyoto-qtcore-native PROPERTIES COMPILE_FLAGS -mstackrealign)
    endif (CMAKE_COMPILER_IS_GNUCC)

    # Get rid of the "lib" prefix on archives/DLLs in Windows.
    set_target_properties(qyoto-qtcore-native
        PROPERTIES PREFIX "" IMPORT_PREFIX "")
endif (WIN32)
INSTALL(FILES callbacks.h delegateinvocation.h emitsignal.h invokeslot.h marshall.h marshall_macros.h methodcall.h
              methodreturnvalue.h qyoto.h qyotosmokebinding.h signalreturnvalue.h slotreturnvalue.h smokeqyoto.h
              virtualmethodcall.h virtualmethodreturnvalue.h DESTINATION include/qyoto)
