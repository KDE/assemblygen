set(kimono_attica_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/ListJob.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/ItemJob.cs
)

add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/kimono-attica.dll
                   COMMAND ${MONO_EXECUTABLE} ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
                            -unsafe -out:kimono-attica.dll -r:qyoto-qtcore.dll -r:qyoto-qtnetwork.dll
                            -namespace:Kimono -import:Qyoto -plugins:QyotoGenerator.dll,KimonoGenerator.dll
                            -code-file:kimono-attica.cs "-keyfile:${KEYFILE}"
                            ${CMAKE_SHARED_LIBRARY_PREFIX}smokeattica.so
                            ${kimono_attica_SRC}
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH})

add_custom_target(kimono-attica ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/kimono-attica.dll)
add_dependencies(kimono-attica assemblygen QyotoGenerator KimonoGenerator qyoto-qtcore qyoto-qtnetwork)
