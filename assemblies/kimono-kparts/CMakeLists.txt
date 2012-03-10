set(kimono_kparts_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/AssemblyInfo.cs
)

add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/kimono-kparts.dll
                   COMMAND ${MONO_EXECUTABLE} --debug ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
                            -unsafe -out:kimono-kparts.dll -r:qyoto-qtcore.dll -r:qyoto-qtgui.dll -r:qyoto-qtxml.dll -r:kimono-kdecore.dll -r:kimono-kdeui.dll -r:kimono-kio.dll
                            -namespace:Kimono -import:Qyoto -plugins:QyotoGenerator.dll,KimonoGenerator.dll
                            -code-file:kimono-kparts.cs \"-keyfile:${KEYFILE}\"
                            ${CMAKE_SHARED_LIBRARY_PREFIX}smokekparts.so
                            ${kimono_kparts_SRC}
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH}
                   DEPENDS ${kimono_kparts_SRC})

add_custom_target(kimono-kparts ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/kimono-kparts.dll)
add_dependencies(kimono-kparts assemblygen QyotoGenerator KimonoGenerator qyoto-qtcore qyoto-qtgui qyoto-qtxml kimono-kdecore kimono-kdeui kimono-kio)
set_property(TARGET kimono-kparts PROPERTY _assembly ${LIBRARY_OUTPUT_PATH}/kimono-kparts.dll)
set_property(TARGET kimono-kparts PROPERTY _assembly_type dll)

GET_LIBRARY_OUTPUT_DIR(library_target_dir)
FILE(WRITE ${library_target_dir}/kimono-kparts.dll.config "<configuration><dllmap dll=\"kimono-kparts-native\" target=\"libkimono-kparts-native.so.2\"/></configuration>")

install_assembly(kimono-kparts DESTINATION lib${LIB_SUFFIX} PACKAGE qyoto)

add_subdirectory(native)
