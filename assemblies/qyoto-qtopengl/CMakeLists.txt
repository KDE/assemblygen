set (qyoto_qtopengl_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/AssemblyInfo.cs
)

add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/qyoto-qtopengl.dll
                   COMMAND ${MONO_EXECUTABLE} ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
                            -unsafe -out:qyoto-qtopengl.dll -r:qyoto-qtcore.dll -r:qyoto-qtgui.dll
                            -plugins:QyotoGenerator.dll
                            -code-file:qyoto-qtopengl.cs "-keyfile:${KEYFILE}"
                            ${CMAKE_SHARED_LIBRARY_PREFIX}smokeqtopengl${CMAKE_SHARED_LIBRARY_SUFFIX}
                            ${qyoto_qtopengl_SRC}
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH}
                   DEPENDS ${qyoto_qtopengl_SRC})

add_custom_target(qyoto-qtopengl ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/qyoto-qtopengl.dll)
add_dependencies(qyoto-qtopengl assemblygen QyotoGenerator qyoto-qtcore qyoto-qtgui)
set_property(TARGET qyoto-qtopengl PROPERTY _assembly ${LIBRARY_OUTPUT_PATH}/qyoto-qtopengl.dll)
set_property(TARGET qyoto-qtopengl PROPERTY _assembly_type dll)

GET_LIBRARY_OUTPUT_DIR(library_target_dir)
FILE(WRITE ${library_target_dir}/qyoto-qtopengl.dll.config "<configuration><dllmap dll=\"qyoto-qtopengl-native\" target=\"libqyoto-qtopengl-native.so.2\"/></configuration>")

install_assembly(qyoto-qtopengl DESTINATION lib${LIB_SUFFIX} PACKAGE qyoto)

add_subdirectory(native)
