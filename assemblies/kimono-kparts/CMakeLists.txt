set(kimono_kparts_SRC
)

add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/kimono-kparts.dll
                   COMMAND ${MONO_EXECUTABLE} --debug ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
                            -unsafe -out:kimono-kparts.dll -r:qyoto-qtcore.dll -r:qyoto-qtgui.dll -r:qyoto-qtxml.dll -r:kimono-kdecore.dll -r:kimono-kdeui.dll -r:kimono-kio.dll
                            -namespace:Kimono -import:Qyoto -plugins:QyotoGenerator.dll,KimonoGenerator.dll
                            -code-file:kimono-kparts.cs "-keyfile:${KEYFILE}"
                            libsmokekparts.so
                            ${kimono_kparts_SRC}
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH})

add_custom_target(kimono-kparts ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/kimono-kparts.dll)
add_dependencies(kimono-kparts assemblygen QyotoGenerator KimonoGenerator qyoto-qtcore qyoto-qtgui qyoto-qtxml kimono-kdecore kimono-kdeui kimono-kio)
