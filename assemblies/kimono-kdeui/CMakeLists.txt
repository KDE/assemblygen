set(kimono_kdeui_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/AssemblyInfo.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/KApplicationExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/KUniqueApplicationExtras.cs
)

add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/kimono-kdeui.dll
                   COMMAND ${MONO_EXECUTABLE} ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/assemblygen.exe
                            -unsafe -out:kimono-kdeui.dll -r:qyoto-qtcore.dll -r:qyoto-qtgui.dll -r:qyoto-qtsvg.dll -r:qyoto-qtxml.dll -r:kimono-kdecore.dll
                            -namespace:Kimono -import:Qyoto -plugins:QyotoGenerator.dll,KimonoGenerator.dll -dest:${CMAKE_INSTALL_PREFIX}
                            -code-file:kimono-kdeui.cs \"-keyfile:${KEYFILE}\"
                            ${CMAKE_SHARED_LIBRARY_PREFIX}smokekdeui.so
                            ${kimono_kdeui_SRC}
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}
                   DEPENDS ${kimono_kdeui_SRC})

add_custom_target(kimono-kdeui ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/kimono-kdeui.dll)
add_dependencies(kimono-kdeui assemblygen QyotoGenerator KimonoGenerator qyoto-qtcore qyoto-qtgui qyoto-qtsvg qyoto-qtxml kimono-kdecore)
set_property(TARGET kimono-kdeui PROPERTY _assembly ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/kimono-kdeui.dll)
set_property(TARGET kimono-kdeui PROPERTY _assembly_type dll)

GET_LIBRARY_OUTPUT_DIR(library_target_dir)


install_assembly(kimono-kdeui DESTINATION lib${LIB_SUFFIX} PACKAGE qyoto)

add_subdirectory(native)
