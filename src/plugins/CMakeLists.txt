option (ENABLE_QYOTO_PLUGIN "Build the Qyoto plugin" TRUE)
option (ENABLE_KIMONO_PLUGIN "Build the Kimono plugin" FALSE)

if (ENABLE_QYOTO_PLUGIN)
    add_subdirectory(qyoto)
endif (ENABLE_QYOTO_PLUGIN)

if (ENABLE_KIMONO_PLUGIN)
    add_subdirectory(kimono)
endif (ENABLE_KIMONO_PLUGIN)
