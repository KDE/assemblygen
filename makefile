all: main.exe

libsmokeloader.so: smokeloader.cpp
	g++ `pkg-config --libs --cflags QtCore` -L`kde4-config --prefix`/lib -lsmokeqtcore -fPIC -I/opt/kde4/include -shared -o libsmokeloader.so smokeloader.cpp

KDEBINDINGS_PATH=$$HOME/dev/kde/kdebindings

KEYFILE=$(KDEBINDINGS_PATH)/csharp/key.snk

CS_SOURCE=Smoke.cs SmokeMethods.cs ByteArrayManager.cs MethodsGenerator.cs PropertyGenerator.cs ClassesGenerator.cs EnumGenerator.cs Translator.cs \
	  SmokeSupport.cs ClassInterfacesGenerator.cs SmokeMethodEqualityComparer.cs GeneratorData.cs Util.cs AttributeGenerator.cs PluginInterfaces.cs \
	  CodeDomExtensions.cs main.cs

main.exe: libsmokeloader.so $(CS_SOURCE)
	gmcs -define:DEBUG -debug -unsafe -out:main.exe $(CS_SOURCE)

QyotoGenerator.dll: QyotoHooks.cs QyotoTranslator.cs main.exe
	gmcs -debug -unsafe -target:library -r:main.exe -out:QyotoGenerator.dll QyotoHooks.cs QyotoTranslator.cs

KimonoGenerator.dll: KimonoTranslator.cs main.exe
	gmcs -debug -unsafe -target:library -r:main.exe -out:KimonoGenerator.dll KimonoTranslator.cs

qtcore: main.exe QyotoGenerator.dll
	mono --debug main.exe -unsafe -out:qyoto-qtcore.dll -plugins:QyotoGenerator.dll -code-file:qyoto-qtcore.cs -keyfile:$(KEYFILE) libsmokeqtcore.so \
		QPair.cs QVariantExtras.cs QMetaTypeExtras.cs QtExtras.cs $(KDEBINDINGS_PATH)/csharp/qyoto/src/*.cs

qtgui: main.exe QyotoGenerator.dll
	mono --debug main.exe -unsafe -out:qyoto-qtgui.dll -plugins:QyotoGenerator.dll -code-file:qyoto-qtgui.cs -keyfile:$(KEYFILE) libsmokeqtgui.so \
		-r:qyoto-qtcore.dll

qtnetwork: main.exe QyotoGenerator.dll
	mono --debug main.exe -unsafe -out:qyoto-qtnetwork.dll -plugins:QyotoGenerator.dll -code-file:qyoto-qtnetwork.cs -keyfile:$(KEYFILE) libsmokeqtnetwork.so \
		-r:qyoto-qtcore.dll

qtdbus: main.exe QyotoGenerator.dll
	mono --debug main.exe -unsafe -out:qyoto-qtdbus.dll -plugins:QyotoGenerator.dll -code-file:qyoto-qtdbus.cs -keyfile:$(KEYFILE) libsmokeqtdbus.so \
		-r:qyoto-qtcore.dll -langversion:3 $(KDEBINDINGS_PATH)/csharp/qyoto/qdbus/QDBusReply.cs $(KDEBINDINGS_PATH)/csharp/qyoto/qdbus/QDBusVariant.cs \
		QDBusSignature.cs QDBusObjectPath.cs

qtsvg: main.exe QyotoGenerator.dll
	mono --debug main.exe -unsafe -out:qyoto-qtsvg.dll -plugins:QyotoGenerator.dll -code-file:qyoto-qtsvg.cs -keyfile:$(KEYFILE) libsmokeqtsvg.so \
		-r:qyoto-qtcore.dll -r:qyoto-qtgui.dll

qtscript: main.exe QyotoGenerator.dll
	mono --debug main.exe -unsafe -out:qyoto-qtscript.dll -plugins:QyotoGenerator.dll -code-file:qyoto-qtscript.cs -keyfile:$(KEYFILE) libsmokeqtscript.so \
		-r:qyoto-qtcore.dll

qtopengl: main.exe QyotoGenerator.dll
	mono --debug main.exe -unsafe -out:qyoto-qtopengl.dll -plugins:QyotoGenerator.dll -code-file:qyoto-qtopengl.cs -keyfile:$(KEYFILE) libsmokeqtopengl.so \
		-r:qyoto-qtcore.dll -r:qyoto-qtgui.dll

qtsql: main.exe QyotoGenerator.dll
	mono --debug main.exe -unsafe -out:qyoto-qtsql.dll -plugins:QyotoGenerator.dll -code-file:qyoto-qtsql.cs -keyfile:$(KEYFILE) libsmokeqtsql.so \
		-r:qyoto-qtcore.dll

qtxml: main.exe QyotoGenerator.dll
	mono --debug main.exe -unsafe -out:qyoto-qtxml.dll -plugins:QyotoGenerator.dll -code-file:qyoto-qtxml.cs -keyfile:$(KEYFILE) libsmokeqtxml.so \
		-r:qyoto-qtcore.dll

qtxmlpatterns: main.exe QyotoGenerator.dll
	mono --debug main.exe -unsafe -out:qyoto-qtxmlpatterns.dll -plugins:QyotoGenerator.dll -code-file:qyoto-qtxmlpatterns.cs -keyfile:$(KEYFILE) libsmokeqtxmlpatterns.so \
		-r:qyoto-qtcore.dll -r:qyoto-qtxml.dll -r:qyoto-qtnetwork.dll

qtmultimedia: main.exe QyotoGenerator.dll
	mono --debug main.exe -unsafe -out:qyoto-qtmultimedia.dll -plugins:QyotoGenerator.dll -code-file:qyoto-qtmultimedia.cs -keyfile:$(KEYFILE) libsmokeqtmultimedia.so \
		-r:qyoto-qtcore.dll -r:qyoto-qtgui.dll

qtwebkit: main.exe QyotoGenerator.dll
	mono --debug main.exe -unsafe -out:qyoto-qtwebkit.dll -plugins:QyotoGenerator.dll -code-file:qyoto-qtwebkit.cs -keyfile:$(KEYFILE) libsmokeqtwebkit.so \
		-r:qyoto-qtcore.dll -r:qyoto-qtgui.dll -r:qyoto-qtnetwork.dll -r:qyoto-qtscript.dll

kdecore: main.exe QyotoGenerator.dll KimonoGenerator.dll
	mono --debug main.exe -unsafe -out:kimono-kdecore.dll -plugins:QyotoGenerator.dll,KimonoGenerator.dll -code-file:kimono-kdecore.cs -keyfile:$(KEYFILE) libsmokekdecore.so \
		-r:qyoto-qtcore.dll -r:qyoto-qtnetwork.dll -r:qyoto-qtdbus.dll

# kate: space-indent off; mixed-indent off
