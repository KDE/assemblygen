cmake_minimum_required(VERSION 2.6)

project (assemblygen)

set(CMAKE_MODULE_PATH ${CMAKE_MODULE_PATH} ${CMAKE_SOURCE_DIR}/cmake/modules)

set(EXECUTABLE_OUTPUT_PATH ${CMAKE_CURRENT_BINARY_DIR}/bin)
set(LIBRARY_OUTPUT_PATH ${CMAKE_CURRENT_BINARY_DIR}/bin)

if (NOT LIB_INSTALL_DIR)
    set (LIB_INSTALL_DIR ${CMAKE_INSTALL_PREFIX}/lib${LIB_SUFFIX})
endif (NOT LIB_INSTALL_DIR)

if (WIN32)
    # Don't expect "lib" at the beginning of Windows DLLs.
    set(CMAKE_SHARED_LIBRARY_PREFIX "")
endif (WIN32)

enable_language(CSharp)

add_subdirectory(src)
add_subdirectory(assemblies)
add_subdirectory(tools)
