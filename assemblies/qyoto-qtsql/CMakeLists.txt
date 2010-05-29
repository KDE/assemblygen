set (qyoto_qtsql_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/AssemblyInfo.cs
)

add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/qyoto-qtsql.dll
                   COMMAND ${MONO_EXECUTABLE} ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
                            -unsafe -out:qyoto-qtsql.dll -r:qyoto-qtcore.dll
                            -plugins:QyotoGenerator.dll
                            -code-file:qyoto-qtsql.cs "-keyfile:${KEYFILE}"
                            libsmokeqtsql.so
                            ${qyoto_qtsql_SRC}
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH}
                   DEPENDS ${qyoto_qtsql_SRC})

add_custom_target(qyoto-qtsql ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/qyoto-qtsql.dll)
add_dependencies(qyoto-qtsql assemblygen QyotoGenerator qyoto-qtcore)
set_property(TARGET qyoto-qtsql PROPERTY _assembly ${LIBRARY_OUTPUT_PATH}/qyoto-qtsql.dll)
set_property(TARGET qyoto-qtsql PROPERTY _assembly_type dll)

GET_LIBRARY_OUTPUT_DIR(library_target_dir)
FILE(WRITE ${library_target_dir}/qyoto-qtsql.dll.config "<configuration><dllmap dll=\"qyoto-qtsql-native\" target=\"libqyoto-qtsql-native.so.2\"/></configuration>")

install_assembly(qyoto-qtsql DESTINATION lib${LIB_SUFFIX} PACKAGE qyoto)

add_subdirectory(native)
