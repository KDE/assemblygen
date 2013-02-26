set (kimono_solid_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/AssemblyInfo.cs
)

add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/kimono-solid.dll
                   COMMAND ${MONO_EXECUTABLE} ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/assemblygen.exe
                            -unsafe -out:kimono-solid.dll -r:qyoto-qtcore.dll
                            -namespace:Kimono -import:Qyoto -plugins:QyotoGenerator.dll,KimonoGenerator.dll -smokePath:${SMOKE_INSTALL_PREFIX}
                            -code-file:kimono-solid.cs \"-keyfile:${KEYFILE}\"
                            ${CMAKE_SHARED_LIBRARY_PREFIX}smokesolid.so
                            ${kimono_solid_SRC}
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}
                   DEPENDS ${kimono_solid_SRC})

add_custom_target(kimono-solid ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/kimono-solid.dll)
add_dependencies(kimono-solid assemblygen QyotoGenerator KimonoGenerator qyoto-qtcore)
set_property(TARGET kimono-solid PROPERTY _assembly ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/kimono-solid.dll)
set_property(TARGET kimono-solid PROPERTY _assembly_type dll)

GET_LIBRARY_OUTPUT_DIR(library_target_dir)


install_assembly(kimono-solid DESTINATION lib${LIB_SUFFIX} PACKAGE qyoto)

add_subdirectory(native)
