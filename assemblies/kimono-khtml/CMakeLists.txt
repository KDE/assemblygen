set(kimono_khtml_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/AssemblyInfo.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/KHTMLWidget.cs
)

add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/kimono-khtml.dll
                   COMMAND ${MONO_EXECUTABLE} --debug ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
                            -unsafe -out:kimono-khtml.dll -r:qyoto-qtcore.dll -r:qyoto-qtgui.dll -r:kimono-kdecore.dll -r:kimono-kdeui.dll -r:kimono-kparts.dll -r:kimono-kio.dll
                            -namespace:Kimono -import:Qyoto -plugins:QyotoGenerator.dll,KimonoGenerator.dll
                            -code-file:kimono-khtml.cs \"-keyfile:${KEYFILE}\"
                            ${CMAKE_SHARED_LIBRARY_PREFIX}smokekhtml.so
                            ${kimono_khtml_SRC}
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH}
                   DEPENDS ${kimono_khtml_SRC})

add_custom_target(kimono-khtml ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/kimono-khtml.dll)
add_dependencies(kimono-khtml assemblygen QyotoGenerator KimonoGenerator qyoto-qtcore qyoto-qtgui kimono-kdecore kimono-kdeui kimono-kparts kimono-kio)
set_property(TARGET kimono-khtml PROPERTY _assembly ${LIBRARY_OUTPUT_PATH}/kimono-khtml.dll)
set_property(TARGET kimono-khtml PROPERTY _assembly_type dll)

GET_LIBRARY_OUTPUT_DIR(library_target_dir)
FILE(WRITE ${library_target_dir}/kimono-khtml.dll.config "<configuration><dllmap dll=\"kimono-khtml-native\" target=\"libkimono-khtml-native.so.2\"/></configuration>")

install_assembly(kimono-khtml DESTINATION lib${LIB_SUFFIX} PACKAGE qyoto)

add_subdirectory(native)
