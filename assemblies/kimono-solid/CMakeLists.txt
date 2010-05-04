set(kimono_solid_SRC
)

add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/kimono-solid.dll
                   COMMAND ${MONO_EXECUTABLE} --debug ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
                            -unsafe -out:kimono-solid.dll -r:qyoto-qtcore.dll -r:qyoto-qtdbus.dll
                            -global-class:KDE -namespace:Kimono -import:Qyoto -plugins:QyotoGenerator.dll,KimonoGenerator.dll
                            -code-file:kimono-solid.cs "-keyfile:${KEYFILE}"
                            libsmokesolid.so
                            ${kimono_solid_SRC}
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH})

add_custom_target(kimono-solid ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/kimono-solid.dll)
add_dependencies(kimono-solid assemblygen QyotoGenerator KimonoGenerator qyoto-qtcore qyoto-qtdbus)
