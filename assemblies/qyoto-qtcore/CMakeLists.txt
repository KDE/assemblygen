set (qyoto_qtcore_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/AssemblyInfo.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QMetaTypeExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QPair.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QtExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QVariantExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/Qyoto.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QyotoMetaData.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/SmokeInvocation.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/SmokeMarshallers.cs
)

add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/qyoto-qtcore.dll
                   COMMAND ${MONO_EXECUTABLE} ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
                            -unsafe -out:qyoto-qtcore.dll -global-class:Qt
                            -plugins:QyotoGenerator.dll
                            -code-file:qyoto-qtcore.cs "-keyfile:${KEYFILE}"
                            libsmokeqtcore.so
                            ${qyoto_qtcore_SRC}
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH})

add_custom_target(qyoto-qtcore ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/qyoto-qtcore.dll)
add_dependencies(qyoto-qtcore assemblygen QyotoGenerator)
