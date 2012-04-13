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
add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/qyoto-qtgui.dll
                   COMMAND ${MONO_EXECUTABLE} ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
                            -unsafe -out:qyoto-qtgui.dll -r:qyoto-qtcore.dll
                            -plugins:QyotoGenerator.dll -dest:${CMAKE_INSTALL_PREFIX}
                            -code-file:qyoto-qtgui.cs \"-keyfile:${KEYFILE}\"
                            ${CMAKE_SHARED_LIBRARY_PREFIX}smokeqtgui${CMAKE_SHARED_LIBRARY_SUFFIX}
                            ${qyoto_qtgui_SRC}
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH}
                   DEPENDS ${qyoto_qtgui_SRC})

add_custom_target(qyoto-qtgui ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/qyoto-qtgui.dll)
add_dependencies(qyoto-qtgui assemblygen QyotoGenerator qyoto-qtcore)
set_property(TARGET qyoto-qtgui PROPERTY _assembly ${LIBRARY_OUTPUT_PATH}/qyoto-qtgui.dll)
set_property(TARGET qyoto-qtgui PROPERTY _assembly_type dll)

FILE(WRITE ${library_target_dir}/qyoto-qtgui.dll.config "<configuration><dllmap dll=\"qyoto-qtgui-native\" target=\"libqyoto-qtgui-native.so.2\"/></configuration>")

install_assembly(qyoto-qtgui DESTINATION lib${LIB_SUFFIX} PACKAGE qyoto)

add_subdirectory(native)
