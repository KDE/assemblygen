add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/qyoto-qtsql.dll
                   COMMAND ${MONO_EXECUTABLE} ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
                            -unsafe -out:qyoto-qtsql.dll -r:qyoto-qtcore.dll
                            -plugins:QyotoGenerator.dll
                            -code-file:qyoto-qtsql.cs "-keyfile:${KEYFILE}"
                            libsmokeqtsql.so
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH})

add_custom_target(qyoto-qtsql ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/qyoto-qtsql.dll)
add_dependencies(qyoto-qtsql assemblygen QyotoGenerator qyoto-qtcore)
