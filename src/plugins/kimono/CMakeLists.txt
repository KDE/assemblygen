set(KimonoGenerator_SRC
    KimonoTranslator.cs
)

csharp_add_library(KimonoGenerator ${KimonoGenerator_SRC} UNSAFE REFERENCES assemblygen)

install_assembly(NO_GAC KimonoGenerator DESTINATION lib${LIB_SUFFIX}/assemblygen/plugins)
