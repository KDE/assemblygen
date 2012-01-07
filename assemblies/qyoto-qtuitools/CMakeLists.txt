set (qyoto_qtuitools_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/AssemblyInfo.cs
)

add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/qyoto-qtuitools.dll
                   COMMAND ${MONO_EXECUTABLE} ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
                            -unsafe -out:qyoto-qtuitools.dll -r:qyoto-qtcore.dll -r:qyoto-qtgui.dll
                            -plugins:QyotoGenerator.dll
                            -code-file:qyoto-qtuitools.cs "-keyfile:${KEYFILE}"
                            ${CMAKE_SHARED_LIBRARY_PREFIX}smokeqtuitools${CMAKE_SHARED_LIBRARY_SUFFIX}
                            ${qyoto_qtuitools_SRC}
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH}
                   DEPENDS ${qyoto_qtuitools_SRC})

add_custom_target(qyoto-qtuitools ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/qyoto-qtuitools.dll)
add_dependencies(qyoto-qtuitools assemblygen QyotoGenerator qyoto-qtcore qyoto-qtgui)
set_property(TARGET qyoto-qtuitools PROPERTY _assembly ${LIBRARY_OUTPUT_PATH}/qyoto-qtuitools.dll)
set_property(TARGET qyoto-qtuitools PROPERTY _assembly_type dll)

GET_LIBRARY_OUTPUT_DIR(library_target_dir)
FILE(WRITE ${library_target_dir}/qyoto-qtuitools.dll.config "<configuration><dllmap dll=\"qyoto-qtuitools-native\" target=\"libqyoto-qtuitools-native.so.2\"/></configuration>")

install_assembly(qyoto-qtuitools DESTINATION lib${LIB_SUFFIX} PACKAGE qyoto)

add_subdirectory(native)
