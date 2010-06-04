set(kimono_kdecore_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/AssemblyInfo.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/KCmdLineArgsExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/KPluginFactoryExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/KServiceExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/KUrlExtras.cs
)

add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/kimono-kdecore.dll
                   COMMAND ${MONO_EXECUTABLE} ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
                            -unsafe -out:kimono-kdecore.dll -r:qyoto-qtcore.dll -r:qyoto-qtgui.dll -r:qyoto-qtnetwork.dll -r:qyoto-qtdbus.dll
                            -global-class:KDE -namespace:Kimono -import:Qyoto -plugins:QyotoGenerator.dll,KimonoGenerator.dll
                            -code-file:kimono-kdecore.cs "-keyfile:${KEYFILE}"
                            libsmokekdecore.so
                            ${kimono_kdecore_SRC}
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH}
                   DEPENDS ${kimono_kdecore_SRC})

add_custom_target(kimono-kdecore ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/kimono-kdecore.dll)
add_dependencies(kimono-kdecore assemblygen QyotoGenerator KimonoGenerator qyoto-qtcore qyoto-qtgui qyoto-qtnetwork qyoto-qtdbus)
set_property(TARGET kimono-kdecore PROPERTY _assembly ${LIBRARY_OUTPUT_PATH}/kimono-kdecore.dll)
set_property(TARGET kimono-kdecore PROPERTY _assembly_type dll)

GET_LIBRARY_OUTPUT_DIR(library_target_dir)
FILE(WRITE ${library_target_dir}/kimono-kdecore.dll.config "<configuration><dllmap dll=\"kimono-kdecore-native\" target=\"libkimono-kdecore-native.so.2\"/></configuration>")

install_assembly(kimono-kdecore DESTINATION lib${LIB_SUFFIX} PACKAGE qyoto)

add_subdirectory(native)
