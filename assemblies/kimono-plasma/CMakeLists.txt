set(kimono_plasma_SRC
)

add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/kimono-plasma.dll
                   COMMAND ${MONO_EXECUTABLE} ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
                            -unsafe -out:kimono-plasma.dll -r:qyoto-qtcore.dll -r:qyoto-qtgui.dll -r:qyoto-qtxml.dll -r:qyoto-phonon.dll -r:qyoto-qtwebkit.dll -r:kimono-kdecore.dll -r:kimono-kdeui.dll -r:kimono-kio.dll
                            -namespace:Kimono -import:Qyoto -plugins:QyotoGenerator.dll,KimonoGenerator.dll
                            -code-file:kimono-plasma.cs "-keyfile:${KEYFILE}"
                            libsmokeplasma.so
                            ${kimono_plasma_SRC}
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH})

add_custom_target(kimono-plasma ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/kimono-plasma.dll)
add_dependencies(kimono-plasma assemblygen QyotoGenerator KimonoGenerator qyoto-qtcore qyoto-qtgui qyoto-qtxml qyoto-phonon qyoto-qtwebkit kimono-kdecore kimono-kdeui kimono-kio)
