project (qyoto-qtcore)

set (qyoto_qtcore_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/AssemblyInfo.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QAbstractItemModelExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QByteArrayExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QCoreApplicationExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QIODeviceExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QLineFExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QMetaTypeExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QModelIndexExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QObjectExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QPair.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QPointFExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QRectFExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QSizeFExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QtExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QUrlExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/Qyoto.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QyotoMetaData.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QEventArgs.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QEventHandler.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/SmokeInvocation.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/SmokeMarshallers.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/StreamWrapper.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QIODeviceWrapper.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/NativeLong.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/ISmokeObject.cs
)

GET_LIBRARY_OUTPUT_DIR(library_target_dir)

set (ASSEMBLYGEN_ARGS
	${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/assemblygen.exe
	-unsafe -out:qyoto-qtcore.dll -global-class:Qt -namespace:QtCore
	-plugins:QyotoGenerator.dll -smokePath:${SMOKE_INSTALL_PREFIX}
	-code-file:qyoto-qtcore.cs -docs:"${DOCUMENTATION_PATH}" 
	\"-keyfile:\\\"${KEYFILE}\\\"\"
	${CMAKE_SHARED_LIBRARY_PREFIX}smokeqtcore${CMAKE_SHARED_LIBRARY_SUFFIX}
	${qyoto_qtcore_SRC}
	WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}
	DEPENDS ${qyoto_qtcore_SRC}
)
if (MONO_EXECUTABLE)
	add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtcore.dll
		COMMAND ${MONO_EXECUTABLE} ${ASSEMBLYGEN_ARGS}
		COMMAND ${SN_EXECUTABLE} -R ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtcore.dll \\\"${KEYFILE}\\\")
else (MONO_EXECUTABLE)
	add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtcore.dll
		COMMAND ${ASSEMBLYGEN_ARGS})
endif (MONO_EXECUTABLE)

add_custom_target(qyoto-qtcore ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtcore.dll)
add_dependencies(qyoto-qtcore assemblygen QyotoGenerator)
set_property(TARGET qyoto-qtcore PROPERTY _assembly ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtcore.dll)
set_property(TARGET qyoto-qtcore PROPERTY _assembly_type dll)



install_assembly(qyoto-qtcore DESTINATION lib${LIB_SUFFIX} PACKAGE qyoto)
install(FILES ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/qyoto-qtcore.xml DESTINATION share/qyoto)

add_subdirectory(native)
