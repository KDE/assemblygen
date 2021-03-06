set (qyoto_qtxml_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/AssemblyInfo.cs
)

GET_LIBRARY_OUTPUT_DIR(library_target_dir)
set (ASSEMBLYGEN_ARGS
	${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/assemblygen.exe
	-unsafe -out:qyoto-qtxml.dll -r:qyoto-qtcore.dll -namespace:QtXml
	-plugins:QyotoGenerator.dll -smokePath:${SMOKE_INSTALL_PREFIX}
	-code-file:qyoto-qtxml.cs -docs:${DOCUMENTATION_PATH} -keyfile:"${KEYFILE}"
	${CMAKE_SHARED_LIBRARY_PREFIX}smokeqtxml${CMAKE_SHARED_LIBRARY_SUFFIX}
	${qyoto_qtxml_SRC}
	WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}
	DEPENDS ${qyoto_qtxml_SRC}
)
if (MONO_EXECUTABLE)
	add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtxml.dll
		COMMAND ${MONO_EXECUTABLE} ${ASSEMBLYGEN_ARGS}
		COMMAND ${SN_EXECUTABLE} -R ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtxml.dll ${KEYFILE}
		VERBATIM)
else (MONO_EXECUTABLE)
	add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtxml.dll
		COMMAND ${ASSEMBLYGEN_ARGS} VERBATIM)
endif (MONO_EXECUTABLE)

add_custom_target(qyoto-qtxml ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtxml.dll)
add_dependencies(qyoto-qtxml assemblygen QyotoGenerator qyoto-qtcore)
set_property(TARGET qyoto-qtxml PROPERTY _assembly ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtxml.dll)
set_property(TARGET qyoto-qtxml PROPERTY _assembly_type dll)



install_assembly(qyoto-qtxml DESTINATION lib${LIB_SUFFIX} PACKAGE qyoto DOC share/qyoto)

add_subdirectory(native)
