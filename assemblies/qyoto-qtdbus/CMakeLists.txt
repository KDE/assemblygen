set(qyoto_qtdbus_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/QDBusObjectPath.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QDBusReply.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QDBusSignature.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QDBusVariant.cs

    ${CMAKE_CURRENT_SOURCE_DIR}/AssemblyInfo.cs
)

GET_LIBRARY_OUTPUT_DIR(library_target_dir)
set (ASSEMBLYGEN_ARGS
	${LIBRARY_OUTPUT_PATH}/assemblygen.exe
	-unsafe -out:qyoto-qtdbus.dll -r:qyoto-qtcore.dll
	-plugins:QyotoGenerator.dll -dest:${CMAKE_INSTALL_PREFIX}
	-code-file:qyoto-qtdbus.cs \"-keyfile:${KEYFILE}\"
	${CMAKE_SHARED_LIBRARY_PREFIX}smokeqtdbus${CMAKE_SHARED_LIBRARY_SUFFIX}
	${qyoto_qtdbus_SRC}
	WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH}
	DEPENDS ${qyoto_qtdbus_SRC}
)
if (MONO_EXECUTABLE)
	add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/qyoto-qtdbus.dll
		COMMAND ${MONO_EXECUTABLE} ${ASSEMBLYGEN_ARGS})
else (MONO_EXECUTABLE)
	add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/qyoto-qtdbus.dll
		COMMAND ${ASSEMBLYGEN_ARGS})
endif (MONO_EXECUTABLE)

add_custom_target(qyoto-qtdbus ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/qyoto-qtdbus.dll)
add_dependencies(qyoto-qtdbus assemblygen QyotoGenerator qyoto-qtcore)
set_property(TARGET qyoto-qtdbus PROPERTY _assembly ${LIBRARY_OUTPUT_PATH}/qyoto-qtdbus.dll)
set_property(TARGET qyoto-qtdbus PROPERTY _assembly_type dll)



install_assembly(qyoto-qtdbus DESTINATION lib${LIB_SUFFIX} PACKAGE qyoto)

add_subdirectory(native)
