set(KimonoGenerator_SRC
    KimonoTranslator.cs
)

csharp_add_library(KimonoGenerator ${KimonoGenerator_SRC} UNSAFE REFERENCES assemblygen)
