set (qyoto_qtscript_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/AssemblyInfo.cs
)

add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/qyoto-qtscript.dll
                   COMMAND ${MONO_EXECUTABLE} ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
                            -unsafe -out:qyoto-qtscript.dll -r:qyoto-qtcore.dll
                            -plugins:QyotoGenerator.dll
                            -code-file:qyoto-qtscript.cs "-keyfile:${KEYFILE}"
                            libsmokeqtscript${CMAKE_SHARED_LIBRARY_SUFFIX}
                            ${qyoto_qtscript_SRC}
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH}
                   DEPENDS ${qyoto_qtscript_SRC})

add_custom_target(qyoto-qtscript ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/qyoto-qtscript.dll)
add_dependencies(qyoto-qtscript assemblygen QyotoGenerator qyoto-qtcore)
set_property(TARGET qyoto-qtscript PROPERTY _assembly ${LIBRARY_OUTPUT_PATH}/qyoto-qtscript.dll)
set_property(TARGET qyoto-qtscript PROPERTY _assembly_type dll)

GET_LIBRARY_OUTPUT_DIR(library_target_dir)
FILE(WRITE ${library_target_dir}/qyoto-qtscript.dll.config "<configuration><dllmap dll=\"qyoto-qtscript-native\" target=\"libqyoto-qtscript-native.so.2\"/></configuration>")

install_assembly(qyoto-qtscript DESTINATION lib${LIB_SUFFIX} PACKAGE qyoto)

add_subdirectory(native)
