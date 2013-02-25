set (qyoto_qtopengl_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/AssemblyInfo.cs
)

GET_LIBRARY_OUTPUT_DIR(library_target_dir)
set (ASSEMBLYGEN_ARGS
	${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/assemblygen.exe -unsafe
	-out:qyoto-qtopengl.dll -r:qyoto-qtcore.dll -r:qyoto-qtgui.dll -namespace:QtOpenGL
	-plugins:QyotoGenerator.dll -dest:${CMAKE_INSTALL_PREFIX}
	-code-file:qyoto-qtopengl.cs -docs:"${DOCUMENTATION_PATH}" \"-keyfile:${KEYFILE}\"
	${CMAKE_SHARED_LIBRARY_PREFIX}smokeqtopengl${CMAKE_SHARED_LIBRARY_SUFFIX}
	${qyoto_qtopengl_SRC}
	WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}
	DEPENDS ${qyoto_qtopengl_SRC}
)
if (MONO_EXECUTABLE)
	add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtopengl.dll
		COMMAND ${MONO_EXECUTABLE} ${ASSEMBLYGEN_ARGS}
		COMMAND ${SN_EXECUTABLE} -R ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtopengl.dll ${KEYFILE})
else (MONO_EXECUTABLE)
	add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtopengl.dll
		COMMAND ${ASSEMBLYGEN_ARGS})
endif (MONO_EXECUTABLE)

add_custom_target(qyoto-qtopengl ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtopengl.dll)
add_dependencies(qyoto-qtopengl assemblygen QyotoGenerator qyoto-qtcore qyoto-qtgui)
set_property(TARGET qyoto-qtopengl PROPERTY _assembly ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtopengl.dll)
set_property(TARGET qyoto-qtopengl PROPERTY _assembly_type dll)



install_assembly(qyoto-qtopengl DESTINATION lib${LIB_SUFFIX} PACKAGE qyoto)
install(FILES ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtopengl.xml DESTINATION share/qyoto)

add_subdirectory(native)
