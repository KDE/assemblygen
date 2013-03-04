find_package(Qt4 REQUIRED)
find_package(Smoke REQUIRED)

set(QyotoGenerator_SRC
	Documentation.cs
    QyotoHooks.cs
    QyotoTranslator.cs
    PropertyGenerator.cs
)

set(qyotogenerator_native_SRC
    qyotogenerator-native.cpp
)

add_library(qyotogenerator-native SHARED ${qyotogenerator_native_SRC})
target_link_libraries(qyotogenerator-native ${QT_QTCORE_LIBRARY} ${SMOKE_BASE_LIBRARY})

if (WIN32 AND CMAKE_COMPILER_IS_GNUCC)
	# Realign the stack, for compatibility with an older ABI.
	set_target_properties (qyotogenerator-native PROPERTIES COMPILE_FLAGS -mstackrealign)
endif (WIN32 AND CMAKE_COMPILER_IS_GNUCC)

csharp_add_library(QyotoGenerator ${QyotoGenerator_SRC} UNSAFE REFERENCES System.Data.dll assemblygen Mono.Data.Sqlite.dll zlib.net.dll)
add_custom_command(TARGET QyotoGenerator PRE_BUILD COMMAND ${CMAKE_COMMAND} -E copy ${CMAKE_CURRENT_SOURCE_DIR}/Mono.Data.Sqlite.dll ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/Mono.Data.Sqlite.dll)
add_custom_command(TARGET QyotoGenerator PRE_BUILD COMMAND ${CMAKE_COMMAND} -E copy ${CMAKE_CURRENT_SOURCE_DIR}/zlib.net.dll ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/zlib.net.dll)
add_custom_command(TARGET QyotoGenerator PRE_BUILD COMMAND ${CMAKE_COMMAND} -E copy ${CMAKE_CURRENT_SOURCE_DIR}/sqlite3.dll ${LIBRARY_OUTPUT_PATH}/${CMAKE_CFG_INTDIR}/sqlite3.dll)

add_dependencies(QyotoGenerator qyotogenerator-native)

install(TARGETS qyotogenerator-native DESTINATION lib${LIB_SUFFIX}/assemblygen/plugins)
install_assembly(NO_GAC QyotoGenerator DESTINATION lib${LIB_SUFFIX}/assemblygen/plugins)
