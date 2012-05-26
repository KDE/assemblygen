set (qyoto_qtsql_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/AssemblyInfo.cs
)

GET_LIBRARY_OUTPUT_DIR(library_target_dir)
set (ASSEMBLYGEN_ARGS
	${LIBRARY_OUTPUT_PATH}/assemblygen.exe
	-unsafe -out:qyoto-qtsql.dll -r:qyoto-qtcore.dll -r:qyoto-qtgui.dll
	-plugins:QyotoGenerator.dll -dest:${CMAKE_INSTALL_PREFIX}
	-code-file:qyoto-qtsql.cs \"-keyfile:${KEYFILE}\"
	${CMAKE_SHARED_LIBRARY_PREFIX}smokeqtsql${CMAKE_SHARED_LIBRARY_SUFFIX}
	${qyoto_qtsql_SRC}
	WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH}
	DEPENDS ${qyoto_qtsql_SRC}
)
if (MONO_EXECUTABLE)
	add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/qyoto-qtsql.dll
		COMMAND ${MONO_EXECUTABLE} ${ASSEMBLYGEN_ARGS})
else (MONO_EXECUTABLE)
	add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/qyoto-qtsql.dll
		COMMAND ${ASSEMBLYGEN_ARGS})
endif (MONO_EXECUTABLE)

add_custom_target(qyoto-qtsql ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/qyoto-qtsql.dll)
add_dependencies(qyoto-qtsql assemblygen QyotoGenerator qyoto-qtcore qyoto-qtgui)
set_property(TARGET qyoto-qtsql PROPERTY _assembly ${LIBRARY_OUTPUT_PATH}/qyoto-qtsql.dll)
set_property(TARGET qyoto-qtsql PROPERTY _assembly_type dll)

FILE(WRITE ${library_target_dir}/qyoto-qtsql.dll.config "<configuration><dllmap dll=\"qyoto-qtsql-native\" target=\"libqyoto-qtsql-native.so.2\"/></configuration>")

install_assembly(qyoto-qtsql DESTINATION lib${LIB_SUFFIX} PACKAGE qyoto)

add_subdirectory(native)
