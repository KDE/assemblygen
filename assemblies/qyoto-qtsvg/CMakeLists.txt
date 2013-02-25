set (qyoto_qtsvg_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/AssemblyInfo.cs
)

GET_LIBRARY_OUTPUT_DIR(library_target_dir)
set (ASSEMBLYGEN_ARGS
	${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/assemblygen.exe
	-unsafe -out:qyoto-qtsvg.dll -r:qyoto-qtcore.dll -r:qyoto-qtgui.dll -namespace:QtSvg
	-plugins:QyotoGenerator.dll -dest:${CMAKE_INSTALL_PREFIX}
	-code-file:qyoto-qtsvg.cs -docs:"${DOCUMENTATION_PATH}" \"-keyfile:${KEYFILE}\"
	${CMAKE_SHARED_LIBRARY_PREFIX}smokeqtsvg${CMAKE_SHARED_LIBRARY_SUFFIX}
	${qyoto_qtsvg_SRC}
	WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}
	DEPENDS ${qyoto_qtsvg_SRC}
)
if (MONO_EXECUTABLE)
	add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtsvg.dll
		COMMAND ${MONO_EXECUTABLE} ${ASSEMBLYGEN_ARGS}
		COMMAND ${SN_EXECUTABLE} -R ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtsvg.dll ${KEYFILE})
else (MONO_EXECUTABLE)
	add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtsvg.dll
		COMMAND ${ASSEMBLYGEN_ARGS})
endif (MONO_EXECUTABLE)

add_custom_target(qyoto-qtsvg ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtsvg.dll)
add_dependencies(qyoto-qtsvg assemblygen QyotoGenerator qyoto-qtcore qyoto-qtgui)
set_property(TARGET qyoto-qtsvg PROPERTY _assembly ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtsvg.dll)
set_property(TARGET qyoto-qtsvg PROPERTY _assembly_type dll)



install_assembly(qyoto-qtsvg DESTINATION lib${LIB_SUFFIX} PACKAGE qyoto)
install(FILES ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtsvg.xml DESTINATION share/qyoto)

add_subdirectory(native)
