set(kimono_kfile_SRC
)

add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/kimono-kfile.dll
                   COMMAND ${MONO_EXECUTABLE} --debug ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
                            -unsafe -out:kimono-kfile.dll -r:qyoto-qtcore.dll -r:qyoto-qtgui.dll -r:qyoto-qtdbus.dll -r:qyoto-qtxml.dll
                                                          -r:kimono-kdecore.dll -r:kimono-kdeui.dll -r:kimono-kio.dll -r:kimono-solid.dll
                            -namespace:Kimono -import:Qyoto -plugins:QyotoGenerator.dll,KimonoGenerator.dll
                            -code-file:kimono-kfile.cs "-keyfile:${KEYFILE}"
                            libsmokekfile.so
                            ${kimono_kfile_SRC}
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH})

add_custom_target(kimono-kfile ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/kimono-kfile.dll)
add_dependencies(kimono-kfile assemblygen QyotoGenerator KimonoGenerator qyoto-qtcore qyoto-qtgui qyoto-qtdbus qyoto-qtxml kimono-kdecore kimono-kdeui kimono-kio kimono-solid)
