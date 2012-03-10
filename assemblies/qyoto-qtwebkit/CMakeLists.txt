set (qyoto_qtwebkit_SRC
    ${CMAKE_CURRENT_SOURCE_DIR}/AssemblyInfo.cs
)

add_custom_command(OUTPUT ${LIBRARY_OUTPUT_PATH}/qyoto-qtwebkit.dll
                   COMMAND ${MONO_EXECUTABLE} ${LIBRARY_OUTPUT_PATH}/assemblygen.exe
                            -unsafe -out:qyoto-qtwebkit.dll -r:qyoto-qtcore.dll -r:qyoto-qtgui.dll -r:qyoto-qtnetwork.dll -r:qyoto-qtscript.dll
                            -plugins:QyotoGenerator.dll
                            -code-file:qyoto-qtwebkit.cs \"-keyfile:${KEYFILE}\"
                            ${CMAKE_SHARED_LIBRARY_PREFIX}smokeqtwebkit${CMAKE_SHARED_LIBRARY_SUFFIX}
                            ${qyoto_qtwebkit_SRC}
                   WORKING_DIRECTORY ${LIBRARY_OUTPUT_PATH}
                   DEPENDS ${qyoto_qtwebkit_SRC})

add_custom_target(qyoto-qtwebkit ALL DEPENDS ${LIBRARY_OUTPUT_PATH}/qyoto-qtwebkit.dll)
add_dependencies(qyoto-qtwebkit assemblygen QyotoGenerator qyoto-qtcore qyoto-qtgui qyoto-qtnetwork qyoto-qtscript)
set_property(TARGET qyoto-qtwebkit PROPERTY _assembly ${LIBRARY_OUTPUT_PATH}/qyoto-qtwebkit.dll)
set_property(TARGET qyoto-qtwebkit PROPERTY _assembly_type dll)

GET_LIBRARY_OUTPUT_DIR(library_target_dir)
FILE(WRITE ${library_target_dir}/qyoto-qtwebkit.dll.config "<configuration><dllmap dll=\"qyoto-qtwebkit-native\" target=\"libqyoto-qtwebkit-native.so.2\"/></configuration>")

install_assembly(qyoto-qtwebkit DESTINATION lib${LIB_SUFFIX} PACKAGE qyoto)

add_subdirectory(native)
