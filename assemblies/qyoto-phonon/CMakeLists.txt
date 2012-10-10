set (qyoto_phonon_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/AssemblyInfo.cs
)

GET_LIBRARY_OUTPUT_DIR(library_target_dir)
set (ASSEMBLYGEN_ARGS
   ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
   -unsafe -out:qyoto-phonon.dll -r:qyoto-qtcore.dll -r:qyoto-qtgui.dll
   -plugins:QyotoGenerator.dll -dest:${CMAKE_INSTALL_PREFIX}
   -code-file:qyoto-phonon.cs -docs:"${DOCUMENTATION_PATH}" \"-keyfile:${KEYFILE}\"
   ${CMAKE_SHARED_LIBRARY_PREFIX}smokephonon${CMAKE_SHARED_LIBRARY_SUFFIX}
   ${qyoto_phonon_SRC} WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH}
   DEPENDS ${qyoto_phonon_SRC}
)
if (MONO_EXECUTABLE)
    add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/qyoto-phonon.dll
        COMMAND ${MONO_EXECUTABLE} ${ASSEMBLYGEN_ARGS})
else (MONO_EXECUTABLE)
    add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/qyoto-phonon.dll
        COMMAND ${ASSEMBLYGEN_ARGS})
endif (MONO_EXECUTABLE)

add_custom_target(qyoto-phonon ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/qyoto-phonon.dll)
add_dependencies(qyoto-phonon assemblygen QyotoGenerator qyoto-qtcore qyoto-qtgui)
set_property(TARGET qyoto-phonon PROPERTY _assembly ${LIBRARY_OUTPUT_PATH}/qyoto-phonon.dll)
set_property(TARGET qyoto-phonon PROPERTY _assembly_type dll)



install_assembly(qyoto-phonon DESTINATION lib${LIB_SUFFIX} PACKAGE qyoto)
install(FILES ${LIBRARY_OUTPUT_PATH}/qyoto-phonon.xml DESTINATION share/qyoto)

add_subdirectory(native)
