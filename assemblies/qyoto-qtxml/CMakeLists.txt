set (qyoto_qtxml_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/AssemblyInfo.cs
)

add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/qyoto-qtxml.dll
                   COMMAND ${MONO_EXECUTABLE} ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
                            -unsafe -out:qyoto-qtxml.dll -r:qyoto-qtcore.dll
                            -plugins:QyotoGenerator.dll
                            -code-file:qyoto-qtxml.cs "-keyfile:${KEYFILE}"
                            libsmokeqtxml.so
                            ${qyoto_qtxml_SRC}
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH}
                   DEPENDS ${qyoto_qtxml_SRC})

add_custom_target(qyoto-qtxml ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/qyoto-qtxml.dll)
add_dependencies(qyoto-qtxml assemblygen QyotoGenerator qyoto-qtcore)
set_property(TARGET qyoto-qtxml PROPERTY _assembly ${LIBRARY_OUTPUT_PATH}/qyoto-qtxml.dll)
set_property(TARGET qyoto-qtxml PROPERTY _assembly_type dll)

GET_LIBRARY_OUTPUT_DIR(library_target_dir)
FILE(WRITE ${library_target_dir}/qyoto-qtxml.dll.config "<configuration><dllmap dll=\"qyoto-qtxml-native\" target=\"libqyoto-qtxml-native.so.2\"/></configuration>")

install_assembly(qyoto-qtxml DESTINATION lib${LIB_SUFFIX} PACKAGE qyoto)

add_subdirectory(native)
