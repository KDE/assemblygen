set (qyoto_qtwebkit_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/AssemblyInfo.cs
)

GET_LIBRARY_OUTPUT_DIR(library_target_dir)
set (ASSEMBLYGEN_ARGS
	${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/assemblygen.exe
	 -namespace:QtWebKit
	-unsafe -out:qyoto-qtwebkit.dll -r:qyoto-qtcore.dll -r:qyoto-qtgui.dll -r:qyoto-qtnetwork.dll -r:qyoto-qtscript.dll
	-plugins:QyotoGenerator.dll -smokePath:${SMOKE_INSTALL_PREFIX}
	-code-file:qyoto-qtwebkit.cs -docs:"${DOCUMENTATION_PATH}" \"-keyfile:\\\"${KEYFILE}\\\"\"
	${CMAKE_SHARED_LIBRARY_PREFIX}smokeqtwebkit${CMAKE_SHARED_LIBRARY_SUFFIX}
	${qyoto_qtwebkit_SRC}
	WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}
	DEPENDS ${qyoto_qtwebkit_SRC}
)
if (MONO_EXECUTABLE)
	add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtwebkit.dll
		COMMAND ${MONO_EXECUTABLE} ${ASSEMBLYGEN_ARGS}
                COMMAND ${SN_EXECUTABLE} -R ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtwebkit.dll \\\"${KEYFILE}\\\")
else (MONO_EXECUTABLE)
	add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtwebkit.dll
		COMMAND ${ASSEMBLYGEN_ARGS})
endif (MONO_EXECUTABLE)

add_custom_target(qyoto-qtwebkit ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtwebkit.dll)
add_dependencies(qyoto-qtwebkit assemblygen QyotoGenerator qyoto-qtcore qyoto-qtgui qyoto-qtnetwork qyoto-qtscript)
set_property(TARGET qyoto-qtwebkit PROPERTY _assembly ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtwebkit.dll)
set_property(TARGET qyoto-qtwebkit PROPERTY _assembly_type dll)



install_assembly(qyoto-qtwebkit DESTINATION lib${LIB_SUFFIX} PACKAGE qyoto)
install(FILES ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtwebkit.xml DESTINATION share/qyoto)

add_subdirectory(native)
