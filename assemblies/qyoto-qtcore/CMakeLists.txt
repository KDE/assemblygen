project (qyoto-qtcore)

set (qyoto_qtcore_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/AssemblyInfo.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QAbstractItemModelExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QByteArrayExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QCoreApplicationExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QIODeviceExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QLineFExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QMetaTypeExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QModelIndexExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QObjectExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QPair.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QPointFExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QRectFExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QSizeFExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QtExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QUrlExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QVariantExtras.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/Qyoto.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QyotoMetaData.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/SmokeInvocation.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/SmokeMarshallers.cs

    ${CMAKE_CURRENT_SOURCE_DIR}/StreamWrapper.cs
    ${CMAKE_CURRENT_SOURCE_DIR}/QIODeviceWrapper.cs
)

add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/qyoto-qtcore.dll
                   COMMAND ${MONO_EXECUTABLE} ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
                            -unsafe -out:qyoto-qtcore.dll -global-class:Qt
                            -plugins:QyotoGenerator.dll
                            -code-file:qyoto-qtcore.cs "-keyfile:${KEYFILE}"
                            libsmokeqtcore.so
                            ${qyoto_qtcore_SRC}
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH}
                   DEPENDS ${qyoto_qtcore_SRC})

add_custom_target(qyoto-qtcore ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/qyoto-qtcore.dll)
add_dependencies(qyoto-qtcore assemblygen QyotoGenerator)
set_property(TARGET qyoto-qtcore PROPERTY _assembly ${LIBRARY_OUTPUT_PATH}/qyoto-qtcore.dll)
set_property(TARGET qyoto-qtcore PROPERTY _assembly_type dll)

GET_LIBRARY_OUTPUT_DIR(library_target_dir)
FILE(WRITE ${library_target_dir}/qyoto-qtcore.dll.config "<configuration><dllmap dll=\"qyoto-qtcore-native\" target=\"libqyoto-qtcore-native.so.2\"/></configuration>")

install_assembly(qyoto-qtcore DESTINATION lib${LIB_SUFFIX} PACKAGE qyoto)

add_subdirectory(native)
