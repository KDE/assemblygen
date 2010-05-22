add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/qyoto-qtopengl.dll
                   COMMAND ${MONO_EXECUTABLE} ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
                            -unsafe -out:qyoto-qtopengl.dll -r:qyoto-qtcore.dll -r:qyoto-qtgui.dll
                            -plugins:QyotoGenerator.dll
                            -code-file:qyoto-qtopengl.cs "-keyfile:${KEYFILE}"
                            libsmokeqtopengl.so
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH})

add_custom_target(qyoto-qtopengl ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/qyoto-qtopengl.dll)
add_dependencies(qyoto-qtopengl assemblygen QyotoGenerator qyoto-qtcore qyoto-qtgui)
