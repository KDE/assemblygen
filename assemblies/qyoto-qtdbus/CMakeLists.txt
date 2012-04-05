set(qyoto_qtdbus_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/QDBusObjectPath.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QDBusReply.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QDBusSignature.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QDBusVariant.cs

    ${CMAKE_CURRENT_SOURCE_DIR}/AssemblyInfo.cs
)

GET_LIBRARY_OUTPUT_DIR(library_target_dir)
add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/qyoto-qtdbus.dll
                   COMMAND ${MONO_EXECUTABLE} ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
                            -unsafe -out:qyoto-qtdbus.dll -r:qyoto-qtcore.dll
                            -plugins:QyotoGenerator.dll -dest:${library_target_dir}
                            -code-file:qyoto-qtdbus.cs \"-keyfile:${KEYFILE}\"
                            ${CMAKE_SHARED_LIBRARY_PREFIX}smokeqtdbus${CMAKE_SHARED_LIBRARY_SUFFIX}
                            ${qyoto_qtdbus_SRC}
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH}
                   DEPENDS ${qyoto_qtdbus_SRC})

add_custom_target(qyoto-qtdbus ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/qyoto-qtdbus.dll)
add_dependencies(qyoto-qtdbus assemblygen QyotoGenerator qyoto-qtcore)
set_property(TARGET qyoto-qtdbus PROPERTY _assembly ${LIBRARY_OUTPUT_PATH}/qyoto-qtdbus.dll)
set_property(TARGET qyoto-qtdbus PROPERTY _assembly_type dll)

FILE(WRITE ${library_target_dir}/qyoto-qtdbus.dll.config "<configuration><dllmap dll=\"qyoto-qtdbus-native\" target=\"libqyoto-qtdbus-native.so.2\"/></configuration>")

install_assembly(qyoto-qtdbus DESTINATION lib${LIB_SUFFIX} PACKAGE qyoto)

add_subdirectory(native)
