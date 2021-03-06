set (qyoto_qtuitools_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/AssemblyInfo.cs
)

GET_LIBRARY_OUTPUT_DIR(library_target_dir)
set (ASSEMBLYGEN_ARGS
	${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/assemblygen.exe
	-unsafe -out:qyoto-qtuitools.dll -r:qyoto-qtcore.dll -r:qyoto-qtgui.dll -namespace:QtUiTools
	-plugins:QyotoGenerator.dll -smokePath:${SMOKE_INSTALL_PREFIX}
	-code-file:qyoto-qtuitools.cs -docs:${DOCUMENTATION_PATH} -keyfile:"${KEYFILE}"
	${CMAKE_SHARED_LIBRARY_PREFIX}smokeqtuitools${CMAKE_SHARED_LIBRARY_SUFFIX}
	${qyoto_qtuitools_SRC}
	WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}
	DEPENDS ${qyoto_qtuitools_SRC}
)
if (MONO_EXECUTABLE)
	add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtuitools.dll
		COMMAND ${MONO_EXECUTABLE} ${ASSEMBLYGEN_ARGS}
		COMMAND ${SN_EXECUTABLE} -R ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtuitools.dll ${KEYFILE}
		VERBATIM)
else (MONO_EXECUTABLE)
	add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtuitools.dll
		COMMAND ${ASSEMBLYGEN_ARGS} VERBATIM)
endif (MONO_EXECUTABLE)

add_custom_target(qyoto-qtuitools ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtuitools.dll)
add_dependencies(qyoto-qtuitools assemblygen QyotoGenerator qyoto-qtcore qyoto-qtgui)
set_property(TARGET qyoto-qtuitools PROPERTY _assembly ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtuitools.dll)
set_property(TARGET qyoto-qtuitools PROPERTY _assembly_type dll)



install_assembly(qyoto-qtuitools DESTINATION lib${LIB_SUFFIX} PACKAGE qyoto DOC share/qyoto)

add_subdirectory(native)
