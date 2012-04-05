set (qyoto_phonon_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/AssemblyInfo.cs
)

GET_LIBRARY_OUTPUT_DIR(library_target_dir)
add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/qyoto-phonon.dll
                   COMMAND ${MONO_EXECUTABLE} ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
                            -unsafe -out:qyoto-phonon.dll -r:qyoto-qtcore.dll -r:qyoto-qtgui.dll
                            -plugins:QyotoGenerator.dll -dest:${library_target_dir}
                            -code-file:qyoto-phonon.cs \"-keyfile:${KEYFILE}\"
                            ${CMAKE_SHARED_LIBRARY_PREFIX}smokephonon${CMAKE_SHARED_LIBRARY_SUFFIX}
                            ${qyoto_phonon_SRC}
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH}
                   DEPENDS ${qyoto_phonon_SRC})

add_custom_target(qyoto-phonon ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/qyoto-phonon.dll)
add_dependencies(qyoto-phonon assemblygen QyotoGenerator qyoto-qtcore qyoto-qtgui)
set_property(TARGET qyoto-phonon PROPERTY _assembly ${LIBRARY_OUTPUT_PATH}/qyoto-phonon.dll)
set_property(TARGET qyoto-phonon PROPERTY _assembly_type dll)

FILE(WRITE ${library_target_dir}/qyoto-phonon.dll.config "<configuration><dllmap dll=\"qyoto-phonon-native\" target=\"libqyoto-phonon-native.so.2\"/></configuration>")

install_assembly(qyoto-phonon DESTINATION lib${LIB_SUFFIX} PACKAGE qyoto)

add_subdirectory(native)
