all: main.exe

libsmokeloader.so: smokeloader.cpp
	g++ `pkg-config --libs --cflags QtCore` -L`kde4-config --prefix`/lib -lsmokeqtcore -fPIC -I/opt/kde4/include -shared -o libsmokeloader.so smokeloader.cpp

KEYFILE=$$HOME/dev/kde/kdebindings/csharp/key.snk

CS_SOURCE=Smoke.cs SmokeMethods.cs ByteArrayManager.cs MethodsGenerator.cs PropertyGenerator.cs ClassesGenerator.cs EnumGenerator.cs Translator.cs \
	  SmokeSupport.cs ClassInterfacesGenerator.cs SmokeMethodEqualityComparer.cs GeneratorData.cs Util.cs AttributeGenerator.cs QyotoHooks.cs main.cs

main.exe: libsmokeloader.so $(CS_SOURCE)
	gmcs -define:DEBUG -debug -unsafe -out:main.exe $(CS_SOURCE)

qtcore: main.exe
	mono --debug main.exe -unsafe -out:qyoto-qtcore.dll -code-file:qyoto-qtcore.cs -keyfile:$(KEYFILE) libsmokeqtcore.so \
		QPair.cs QVariantExtras.cs QMetaTypeExtras.cs QtExtras.cs ~/dev/kde/kdebindings/csharp/qyoto/src/*.cs

qtgui: main.exe
	mono --debug main.exe -unsafe -out:qyoto-qtgui.dll -code-file:qyoto-qtgui.cs -keyfile:$(KEYFILE) libsmokeqtgui.so \
		-r:qyoto-qtcore.dll

qtnetwork: main.exe
	mono --debug main.exe -unsafe -out:qyoto-qtnetwork.dll -code-file:qyoto-qtnetwork.cs -keyfile:$(KEYFILE) libsmokeqtnetwork.so \
		-r:qyoto-qtcore.dll

kdecore: main.exe
	mono main.exe -unsafe -out:kimono-kdecore.dll -code-file:kimono-kdecore.cs -keyfile:$$HOME/dev/KDE/kdebindings/csharp/key.snk libsmokekdecore.so \
		QPair.cs QVariantExtras.cs ~/dev/KDE/kdebindings/csharp/qyoto/src/*.cs -r:qyoto-qtcore.dll

# kate: space-indent off; mixed-indent off
