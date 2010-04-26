add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/qyoto-qtwebkit.dll
                   COMMAND ${MONO_EXECUTABLE} ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
                            -unsafe -out:qyoto-qtwebkit.dll -r:qyoto-qtcore.dll -r:qyoto-qtgui.dll -r:qyoto-qtnetwork.dll -r:qyoto-qtscript.dll
                            -plugins:QyotoGenerator.dll
                            -code-file:qyoto-qtwebkit.cs "-keyfile:${KEYFILE}"
                            libsmokeqtwebkit.so
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH})

add_custom_target(qyoto-qtwebkit ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/qyoto-qtwebkit.dll)
add_dependencies(qyoto-qtwebkit assemblygen QyotoGenerator qyoto-qtcore qyoto-qtgui qyoto-qtnetwork qyoto-qtscript)
