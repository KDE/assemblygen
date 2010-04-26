add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/qyoto-qtxml.dll
                   COMMAND ${MONO_EXECUTABLE} ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
                            -unsafe -out:qyoto-qtxml.dll -r:qyoto-qtcore.dll
                            -plugins:QyotoGenerator.dll
                            -code-file:qyoto-qtxml.cs "-keyfile:${KEYFILE}"
                            libsmokeqtxml.so
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH})

add_custom_target(qyoto-qtxml ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/qyoto-qtxml.dll)
add_dependencies(qyoto-qtxml assemblygen QyotoGenerator qyoto-qtcore)
