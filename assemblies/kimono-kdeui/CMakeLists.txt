add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/kimono-kdeui.dll
                   COMMAND ${MONO_EXECUTABLE} ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
                            -unsafe -out:kimono-kdeui.dll -r:qyoto-qtcore.dll -r:qyoto-qtgui.dll -r:qyoto-qtsvg.dll -r:qyoto-qtxml.dll -r:kimono-kdecore.dll
                            -namespace:Kimono -import:Qyoto -plugins:QyotoGenerator.dll,KimonoGenerator.dll
                            -code-file:kimono-kdeui.cs "-keyfile:${KEYFILE}"
                            libsmokekdeui.so
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH})

add_custom_target(kimono-kdeui ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/kimono-kdeui.dll)
add_dependencies(kimono-kdeui assemblygen QyotoGenerator KimonoGenerator qyoto-qtcore qyoto-qtgui qyoto-qtsvg qyoto-qtxml kimono-kdecore)
