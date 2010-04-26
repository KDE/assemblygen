add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/qyoto-qtgui.dll
                   COMMAND ${MONO_EXECUTABLE} ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
                            -unsafe -out:qyoto-qtgui.dll -r:qyoto-qtcore.dll
                            -plugins:QyotoGenerator.dll
                            -code-file:qyoto-qtgui.cs "-keyfile:${KEYFILE}"
                            libsmokeqtgui.so
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH})

add_custom_target(qyoto-qtgui ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/qyoto-qtgui.dll)
add_dependencies(qyoto-qtgui assemblygen QyotoGenerator qyoto-qtcore)
