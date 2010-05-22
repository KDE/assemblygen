add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/qyoto-qtsvg.dll
                   COMMAND ${MONO_EXECUTABLE} ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
                            -unsafe -out:qyoto-qtsvg.dll -r:qyoto-qtcore.dll -r:qyoto-qtgui.dll
                            -plugins:QyotoGenerator.dll
                            -code-file:qyoto-qtsvg.cs "-keyfile:${KEYFILE}"
                            libsmokeqtsvg.so
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH})

add_custom_target(qyoto-qtsvg ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/qyoto-qtsvg.dll)
add_dependencies(qyoto-qtsvg assemblygen QyotoGenerator qyoto-qtcore qyoto-qtgui)
