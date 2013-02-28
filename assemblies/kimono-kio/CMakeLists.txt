set(kimono_kio_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/AssemblyInfo.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/KFileItemList.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/KIO_MetaData.cs
)

add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/kimono-kio.dll
                   COMMAND ${MONO_EXECUTABLE} --debug ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/assemblygen.exe
                            -unsafe -out:kimono-kio.dll -r:qyoto-qtcore.dll -r:qyoto-qtgui.dll -r:qyoto-qtdbus.dll -r:qyoto-qtnetwork.dll -r:qyoto-qtxml.dll
                                                        -r:kimono-kdecore.dll -r:kimono-kdeui.dll -r:kimono-solid.dll
                            -namespace:Kimono -import:Qyoto -plugins:QyotoGenerator.dll,KimonoGenerator.dll -smokePath:${SMOKE_INSTALL_PREFIX}
                            -code-file:kimono-kio.cs -keyfile:"${KEYFILE}"
                            ${CMAKE_SHARED_LIBRARY_PREFIX}smokekio.so
                            ${kimono_kio_SRC}
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}
                   DEPENDS ${kimono_kio_SRC})

add_custom_target(kimono-kio ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/kimono-kio.dll)
add_dependencies(kimono-kio assemblygen QyotoGenerator KimonoGenerator qyoto-qtcore qyoto-qtgui qyoto-qtdbus qyoto-qtnetwork qyoto-qtxml kimono-kdecore kimono-kdeui kimono-solid)
set_property(TARGET kimono-kio PROPERTY _assembly ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/kimono-kio.dll)
set_property(TARGET kimono-kio PROPERTY _assembly_type dll)

GET_LIBRARY_OUTPUT_DIR(library_target_dir)


install_assembly(kimono-kio DESTINATION lib${LIB_SUFFIX} PACKAGE qyoto)

add_subdirectory(native)
