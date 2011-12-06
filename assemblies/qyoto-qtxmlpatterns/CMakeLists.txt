set (qyoto_qtxmlpatterns_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/AssemblyInfo.cs
)

add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/qyoto-qtxmlpatterns.dll
                   COMMAND ${MONO_EXECUTABLE} ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
                            -unsafe -out:qyoto-qtxmlpatterns.dll -r:qyoto-qtcore.dll -r:qyoto-qtnetwork.dll
                            -plugins:QyotoGenerator.dll
                            -code-file:qyoto-qtxmlpatterns.cs "-keyfile:${KEYFILE}"
                            libsmokeqtxmlpatterns${CMAKE_SHARED_LIBRARY_SUFFIX}
                            ${qyoto_qtxmlpatterns_SRC}
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH}
                   DEPENDS ${qyoto_qtxmlpatterns_SRC})

add_custom_target(qyoto-qtxmlpatterns ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/qyoto-qtxmlpatterns.dll)
add_dependencies(qyoto-qtxmlpatterns assemblygen QyotoGenerator qyoto-qtcore qyoto-qtnetwork)
set_property(TARGET qyoto-qtxmlpatterns PROPERTY _assembly ${LIBRARY_OUTPUT_PATH}/qyoto-qtxmlpatterns.dll)
set_property(TARGET qyoto-qtxmlpatterns PROPERTY _assembly_type dll)

GET_LIBRARY_OUTPUT_DIR(library_target_dir)
FILE(WRITE ${library_target_dir}/qyoto-qtxmlpatterns.dll.config "<configuration><dllmap dll=\"qyoto-qtxmlpatterns-native\" target=\"libqyoto-qtxmlpatterns-native.so.2\"/></configuration>")

install_assembly(qyoto-qtxmlpatterns DESTINATION lib${LIB_SUFFIX} PACKAGE qyoto)

add_subdirectory(native)
