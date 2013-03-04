set (qyoto_qtxmlpatterns_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/AssemblyInfo.cs
)

GET_LIBRARY_OUTPUT_DIR(library_target_dir)
set (ASSEMBLYGEN_ARGS
	${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/assemblygen.exe
	-unsafe -out:qyoto-qtxmlpatterns.dll -r:qyoto-qtcore.dll -r:qyoto-qtnetwork.dll -namespace:QtXmlPatterns
	-plugins:QyotoGenerator.dll -smokePath:${SMOKE_INSTALL_PREFIX}
	-code-file:qyoto-qtxmlpatterns.cs -docs:${DOCUMENTATION_PATH} -keyfile:"${KEYFILE}"
	${CMAKE_SHARED_LIBRARY_PREFIX}smokeqtxmlpatterns${CMAKE_SHARED_LIBRARY_SUFFIX}
	${qyoto_qtxmlpatterns_SRC}
	WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}
	DEPENDS ${qyoto_qtxmlpatterns_SRC}
)
if (MONO_EXECUTABLE)
	add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtxmlpatterns.dll
		COMMAND ${MONO_EXECUTABLE} ${ASSEMBLYGEN_ARGS}
		COMMAND ${SN_EXECUTABLE} -R ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtxmlpatterns.dll ${KEYFILE}
		VERBATIM)
else (MONO_EXECUTABLE)
	add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtxmlpatterns.dll
		COMMAND ${ASSEMBLYGEN_ARGS} VERBATIM)
endif (MONO_EXECUTABLE)

add_custom_target(qyoto-qtxmlpatterns ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtxmlpatterns.dll)
add_dependencies(qyoto-qtxmlpatterns assemblygen QyotoGenerator qyoto-qtcore qyoto-qtnetwork)
set_property(TARGET qyoto-qtxmlpatterns PROPERTY _assembly ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtxmlpatterns.dll)
set_property(TARGET qyoto-qtxmlpatterns PROPERTY _assembly_type dll)



install_assembly(qyoto-qtxmlpatterns DESTINATION lib${LIB_SUFFIX} PACKAGE qyoto DOC share/qyoto)

add_subdirectory(native)
