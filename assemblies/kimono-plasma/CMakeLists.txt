set(kimono_plasma_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/AssemblyInfo.cs
)

add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/kimono-plasma.dll
                   COMMAND ${MONO_EXECUTABLE} ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
                            -unsafe -out:kimono-plasma.dll -r:qyoto-qtcore.dll -r:qyoto-qtgui.dll -r:qyoto-qtxml.dll -r:qyoto-phonon.dll -r:qyoto-qtwebkit.dll -r:kimono-kdecore.dll -r:kimono-kdeui.dll -r:kimono-kio.dll
                            -namespace:Kimono -import:Qyoto -plugins:QyotoGenerator.dll,KimonoGenerator.dll
                            -code-file:kimono-plasma.cs "-keyfile:${KEYFILE}"
                            libsmokeplasma.so
                            ${kimono_plasma_SRC}
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH}
                   DEPENDS ${kimono_plasma_SRC})

add_custom_target(kimono-plasma ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/kimono-plasma.dll)
add_dependencies(kimono-plasma assemblygen QyotoGenerator KimonoGenerator qyoto-qtcore qyoto-qtgui qyoto-qtxml qyoto-phonon qyoto-qtwebkit kimono-kdecore kimono-kdeui kimono-kio)
set_property(TARGET kimono-plasma PROPERTY _assembly ${LIBRARY_OUTPUT_PATH}/kimono-plasma.dll)
set_property(TARGET kimono-plasma PROPERTY _assembly_type dll)

GET_LIBRARY_OUTPUT_DIR(library_target_dir)
FILE(WRITE ${library_target_dir}/kimono-plasma.dll.config "<configuration><dllmap dll=\"kimono-plasma-native\" target=\"libkimono-plasma-native.so.2\"/></configuration>")

install_assembly(kimono-plasma DESTINATION lib${LIB_SUFFIX} PACKAGE qyoto)

add_subdirectory(native)
