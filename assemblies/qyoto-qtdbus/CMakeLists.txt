set(qyoto_qtdbus_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/QDBusObjectPath.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QDBusReply.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QDBusSignature.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QDBusVariant.cs
)

add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/qyoto-qtdbus.dll
                   COMMAND ${MONO_EXECUTABLE} ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
                            -unsafe -out:qyoto-qtdbus.dll -r:qyoto-qtcore.dll
                            -plugins:QyotoGenerator.dll
                            -code-file:qyoto-qtdbus.cs "-keyfile:${KEYFILE}"
                            libsmokeqtdbus.so
                            ${qyoto_qtdbus_SRC}
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH})

add_custom_target(qyoto-qtdbus ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/qyoto-qtdbus.dll)
add_dependencies(qyoto-qtdbus assemblygen QyotoGenerator qyoto-qtcore)
