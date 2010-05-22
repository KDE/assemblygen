add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/qyoto-qtscript.dll
                   COMMAND ${MONO_EXECUTABLE} ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
                            -unsafe -out:qyoto-qtscript.dll -r:qyoto-qtcore.dll
                            -plugins:QyotoGenerator.dll
                            -code-file:qyoto-qtscript.cs "-keyfile:${KEYFILE}"
                            libsmokeqtscript.so
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH})

add_custom_target(qyoto-qtscript ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/qyoto-qtscript.dll)
add_dependencies(qyoto-qtscript assemblygen QyotoGenerator qyoto-qtcore)
