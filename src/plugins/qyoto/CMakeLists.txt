find_package(Qt4 REQUIRED)
find_package(Smoke REQUIRED)

set(QyotoGenerator_SRC
    QyotoHooks.cs
    QyotoTranslator.cs
    PropertyGenerator.cs
)

set(qyotogenerator_native_SRC
    qyotogenerator-native.cpp
)

add_library(qyotogenerator-native SHARED ${qyotogenerator_native_SRC})
target_link_libraries(qyotogenerator-native ${QT_QTCORE_LIBRARY} ${SMOKE_BASE_LIBRARY})

csharp_add_library(QyotoGenerator ${QyotoGenerator_SRC} UNSAFE REFERENCES assemblygen)
