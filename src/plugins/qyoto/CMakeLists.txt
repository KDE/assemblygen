set(QyotoGenerator_SRC
    QyotoHooks.cs
    QyotoTranslator.cs
)

csharp_add_library(QyotoGenerator ${QyotoGenerator_SRC} UNSAFE REFERENCES assemblygen)
