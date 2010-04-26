project(assemblygen)

find_package(Qt4 REQUIRED)

set(assemblygen_native_SRC
    assemblygen-native.cpp
)

include_directories(${QT_INCLUDES})
add_library(assemblygen-native SHARED ${assemblygen_native_SRC})
target_link_libraries(assemblygen-native ${QT_QTCORE_LIBRARY} smokeqtcore)

set(assemblygen_SRC
    AttributeGenerator.cs
    ByteArrayManager.cs
    ClassesGenerator.cs
    ClassInterfacesGenerator.cs
    CodeDomExtensions.cs
    EnumGenerator.cs
    GeneratorData.cs
    main.cs
    MethodsGenerator.cs
    PluginInterfaces.cs
    PropertyGenerator.cs
    Smoke.cs
    SmokeMethodEqualityComparer.cs
    SmokeMethods.cs
    SmokeSupport.cs
    Translator.cs
    Util.cs
)

csharp_add_executable(assemblygen ${assemblygen_SRC} UNSAFE COMPILE_DEFINITIONS DEBUG)

add_subdirectory(plugins)
