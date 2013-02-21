set (qyoto_qtnetwork_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/AssemblyInfo.cs
)

GET_LIBRARY_OUTPUT_DIR(library_target_dir)
set (ASSEMBLYGEN_ARGS
	${LIBRARY_OUTPUT_PATH}/assemblygen.exe
	-unsafe -out:qyoto-qtnetwork.dll -r:qyoto-qtcore.dll -namespace:QtNetwork
	-plugins:QyotoGenerator.dll -dest:${CMAKE_INSTALL_PREFIX}
	-code-file:qyoto-qtnetwork.cs -docs:"${DOCUMENTATION_PATH}" \"-keyfile:${KEYFILE}\"
	${CMAKE_SHARED_LIBRARY_PREFIX}smokeqtnetwork${CMAKE_SHARED_LIBRARY_SUFFIX}
	${qyoto_qtnetwork_SRC}
	WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH}
	DEPENDS ${qyoto_qtnetwork_SRC}
)
if (MONO_EXECUTABLE)
	add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/qyoto-qtnetwork.dll
		COMMAND ${MONO_EXECUTABLE} ${ASSEMBLYGEN_ARGS}
		COMMAND ${SN_EXECUTABLE} -R ${LIBRARY_OUTPUT_PATH}/qyoto-qtnetwork.dll ${KEYFILE})
else (MONO_EXECUTABLE)
	add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/qyoto-qtnetwork.dll
		COMMAND ${ASSEMBLYGEN_ARGS})
endif (MONO_EXECUTABLE)

add_custom_target(qyoto-qtnetwork ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/qyoto-qtnetwork.dll)
add_dependencies(qyoto-qtnetwork assemblygen QyotoGenerator qyoto-qtcore)
set_property(TARGET qyoto-qtnetwork PROPERTY _assembly ${LIBRARY_OUTPUT_PATH}/qyoto-qtnetwork.dll)
set_property(TARGET qyoto-qtnetwork PROPERTY _assembly_type dll)



install_assembly(qyoto-qtnetwork DESTINATION lib${LIB_SUFFIX} PACKAGE qyoto)
install(FILES ${LIBRARY_OUTPUT_PATH}/qyoto-qtnetwork.xml DESTINATION share/qyoto)

add_subdirectory(native)
