set (qyoto_qtnetwork_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/AssemblyInfo.cs
)

GET_LIBRARY_OUTPUT_DIR(library_target_dir)
add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/qyoto-qtnetwork.dll
                   COMMAND ${MONO_EXECUTABLE} ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
                            -unsafe -out:qyoto-qtnetwork.dll -r:qyoto-qtcore.dll
                            -plugins:QyotoGenerator.dll -dest:${CMAKE_INSTALL_PREFIX}
                            -code-file:qyoto-qtnetwork.cs \"-keyfile:${KEYFILE}\"
                            ${CMAKE_SHARED_LIBRARY_PREFIX}smokeqtnetwork${CMAKE_SHARED_LIBRARY_SUFFIX}
                            ${qyoto_qtnetwork_SRC}
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH}
                   DEPENDS ${qyoto_qtnetwork_SRC})

add_custom_target(qyoto-qtnetwork ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/qyoto-qtnetwork.dll)
add_dependencies(qyoto-qtnetwork assemblygen QyotoGenerator qyoto-qtcore)
set_property(TARGET qyoto-qtnetwork PROPERTY _assembly ${LIBRARY_OUTPUT_PATH}/qyoto-qtnetwork.dll)
set_property(TARGET qyoto-qtnetwork PROPERTY _assembly_type dll)

FILE(WRITE ${library_target_dir}/qyoto-qtnetwork.dll.config "<configuration><dllmap dll=\"qyoto-qtnetwork-native\" target=\"libqyoto-qtnetwork-native.so.2\"/></configuration>")

install_assembly(qyoto-qtnetwork DESTINATION lib${LIB_SUFFIX} PACKAGE qyoto)

add_subdirectory(native)
