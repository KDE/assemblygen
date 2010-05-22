add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/qyoto-qtxmlpatterns.dll
                   COMMAND ${MONO_EXECUTABLE} ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
                            -unsafe -out:qyoto-qtxmlpatterns.dll -r:qyoto-qtcore.dll -r:qyoto-qtnetwork.dll
                            -plugins:QyotoGenerator.dll
                            -code-file:qyoto-qtxmlpatterns.cs "-keyfile:${KEYFILE}"
                            libsmokeqtxmlpatterns.so
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH})

add_custom_target(qyoto-qtxmlpatterns ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/qyoto-qtxmlpatterns.dll)
add_dependencies(qyoto-qtxmlpatterns assemblygen QyotoGenerator qyoto-qtcore qyoto-qtnetwork)
