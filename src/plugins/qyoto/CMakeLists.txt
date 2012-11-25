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

if (WIN32)
	# Realign the stack, for compatibility with an older ABI.
	set_target_properties (qyotogenerator-native PROPERTIES COMPILE_FLAGS -mstackrealign)
endif (WIN32)

csharp_add_library(QyotoGenerator ${QyotoGenerator_SRC} UNSAFE REFERENCES assemblygen Mono.Data.Sqlite.dll zlib.net.dll)
file(COPY Mono.Data.Sqlite.dll zlib.net.dll sqlite3.dll DESTINATION ${LIBRARY_OUTPUT_PATH})

install(TARGETS qyotogenerator-native DESTINATION lib${LIB_SUFFIX}/assemblygen/plugins)
install_assembly(NO_GAC QyotoGenerator DESTINATION lib${LIB_SUFFIX}/assemblygen/plugins)
