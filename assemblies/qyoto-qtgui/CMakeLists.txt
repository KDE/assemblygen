project(qyoto-qtgui)

set (qyoto_qtgui_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/AssemblyInfo.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QApplicationExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QBrushExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QColorExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QCursorExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QIconExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QKeySequenceExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QPenExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QPolygonExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QPolygonFExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QRegionExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QTransformExtras.cs
)

GET_LIBRARY_OUTPUT_DIR(library_target_dir)
set (ASSEMBLYGEN_ARGS
	${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/assemblygen.exe
	-unsafe -out:qyoto-qtgui.dll -r:qyoto-qtcore.dll -namespace:QtGui
	-plugins:QyotoGenerator.dll -smokePath:${SMOKE_INSTALL_PREFIX}
	-code-file:qyoto-qtgui.cs -docs:"${DOCUMENTATION_PATH}" \"-keyfile:\\\"${KEYFILE}\\\"\"
	${CMAKE_SHARED_LIBRARY_PREFIX}smokeqtgui${CMAKE_SHARED_LIBRARY_SUFFIX}
	${qyoto_qtgui_SRC}
	WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}
	DEPENDS ${qyoto_qtgui_SRC}
)
if (MONO_EXECUTABLE)
	add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtgui.dll
		COMMAND ${MONO_EXECUTABLE} ${ASSEMBLYGEN_ARGS}
		COMMAND ${SN_EXECUTABLE} -R ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtgui.dll \\\"${KEYFILE}\\\")
else (MONO_EXECUTABLE)
	add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtgui.dll
		COMMAND ${ASSEMBLYGEN_ARGS})
endif (MONO_EXECUTABLE)

add_custom_target(qyoto-qtgui ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtgui.dll)
add_dependencies(qyoto-qtgui assemblygen QyotoGenerator qyoto-qtcore)
set_property(TARGET qyoto-qtgui PROPERTY _assembly ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtgui.dll)
set_property(TARGET qyoto-qtgui PROPERTY _assembly_type dll)



install_assembly(qyoto-qtgui DESTINATION lib${LIB_SUFFIX} PACKAGE qyoto)
install(FILES ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtgui.xml DESTINATION share/qyoto)

add_subdirectory(native)
