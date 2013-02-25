set (qyoto_qtsql_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/AssemblyInfo.cs
)

GET_LIBRARY_OUTPUT_DIR(library_target_dir)
set (ASSEMBLYGEN_ARGS
	${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/assemblygen.exe
	-unsafe -out:qyoto-qtsql.dll -r:qyoto-qtcore.dll -r:qyoto-qtgui.dll -namespace:QtSql
	-plugins:QyotoGenerator.dll -dest:${CMAKE_INSTALL_PREFIX}
	-code-file:qyoto-qtsql.cs -docs:"${DOCUMENTATION_PATH}" \"-keyfile:${KEYFILE}\"
	${CMAKE_SHARED_LIBRARY_PREFIX}smokeqtsql${CMAKE_SHARED_LIBRARY_SUFFIX}
	${qyoto_qtsql_SRC}
	WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}
	DEPENDS ${qyoto_qtsql_SRC}
)
if (MONO_EXECUTABLE)
	add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtsql.dll
		COMMAND ${MONO_EXECUTABLE} ${ASSEMBLYGEN_ARGS}
		COMMAND ${SN_EXECUTABLE} -R ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtsql.dll ${KEYFILE})
else (MONO_EXECUTABLE)
	add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtsql.dll
		COMMAND ${ASSEMBLYGEN_ARGS})
endif (MONO_EXECUTABLE)

add_custom_target(qyoto-qtsql ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtsql.dll)
add_dependencies(qyoto-qtsql assemblygen QyotoGenerator qyoto-qtcore qyoto-qtgui)
set_property(TARGET qyoto-qtsql PROPERTY _assembly ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtsql.dll)
set_property(TARGET qyoto-qtsql PROPERTY _assembly_type dll)



install_assembly(qyoto-qtsql DESTINATION lib${LIB_SUFFIX} PACKAGE qyoto)
install(FILES ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtsql.xml DESTINATION share/qyoto)

add_subdirectory(native)
