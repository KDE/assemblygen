set(kimono_ktexteditor_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/AssemblyInfo.cs
)

add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/kimono-ktexteditor.dll
                   COMMAND ${MONO_EXECUTABLE} --debug ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/assemblygen.exe
                            -unsafe -out:kimono-ktexteditor.dll -r:qyoto-qtcore.dll -r:qyoto-qtgui.dll -r:qyoto-qtxml.dll
                                -r:kimono-kdecore.dll -r:kimono-kdeui.dll -r:kimono-kparts.dll
                            -namespace:Kimono -import:Qyoto -plugins:QyotoGenerator.dll,KimonoGenerator.dll -dest:${CMAKE_INSTALL_PREFIX}
                            -code-file:kimono-ktexteditor.cs \"-keyfile:${KEYFILE}\"
                            ${CMAKE_SHARED_LIBRARY_PREFIX}smokektexteditor.so
                            ${kimono_ktexteditor_SRC}
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}
                   DEPENDS ${kimono_ktexteditor_SRC})

add_custom_target(kimono-ktexteditor ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/kimono-ktexteditor.dll)
add_dependencies(kimono-ktexteditor assemblygen QyotoGenerator KimonoGenerator qyoto-qtcore qyoto-qtgui qyoto-qtxml kimono-kdecore kimono-kdeui kimono-kparts)
set_property(TARGET kimono-ktexteditor PROPERTY _assembly ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/kimono-ktexteditor.dll)
set_property(TARGET kimono-ktexteditor PROPERTY _assembly_type dll)

GET_LIBRARY_OUTPUT_DIR(library_target_dir)


install_assembly(kimono-ktexteditor DESTINATION lib${LIB_SUFFIX} PACKAGE qyoto)

add_subdirectory(native)
