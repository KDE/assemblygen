add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/qyoto-qtnetwork.dll
                   COMMAND ${MONO_EXECUTABLE} ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
                            -unsafe -out:qyoto-qtnetwork.dll -r:qyoto-qtcore.dll
                            -plugins:QyotoGenerator.dll
                            -code-file:qyoto-qtnetwork.cs "-keyfile:${KEYFILE}"
                            libsmokeqtnetwork.so
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH})

add_custom_target(qyoto-qtnetwork ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/qyoto-qtnetwork.dll)
add_dependencies(qyoto-qtnetwork assemblygen QyotoGenerator qyoto-qtcore)
