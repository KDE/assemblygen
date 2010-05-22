add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/qyoto-phonon.dll
                   COMMAND ${MONO_EXECUTABLE} ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
                            -unsafe -out:qyoto-phonon.dll -r:qyoto-qtcore.dll -r:qyoto-qtgui.dll
                            -plugins:QyotoGenerator.dll
                            -code-file:qyoto-phonon.cs "-keyfile:${KEYFILE}"
                            libsmokephonon.so
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH})

add_custom_target(qyoto-phonon ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/qyoto-phonon.dll)
add_dependencies(qyoto-phonon assemblygen QyotoGenerator qyoto-qtcore qyoto-qtgui)
