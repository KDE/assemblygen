all: main.exe

libsmokeloader.so: smokeloader.cpp
	g++ `pkg-config --libs --cflags QtCore` -fPIC -I/opt/kde4/include -shared -o libsmokeloader.so smokeloader.cpp

CS_SOURCE=Smoke.cs SmokeMethods.cs ByteArrayManager.cs MethodsGenerator.cs PropertyGenerator.cs ClassesGenerator.cs EnumGenerator.cs Translator.cs \
	  SmokeSupport.cs ClassInterfacesGenerator.cs SmokeMethodEqualityComparer.cs GeneratorData.cs Util.cs main.cs

main.exe: libsmokeloader.so $(CS_SOURCE)
	gmcs -define:DEBUG -debug -unsafe -out:main.exe $(CS_SOURCE)

test: main.exe
	mono main.exe -unsafe -out:out.dll -code-file:out.cs -keyfile:$$HOME/dev/KDE/kdebindings/csharp/key.snk \
		QPair.cs QVariantExtras.cs ~/dev/KDE/kdebindings/csharp/qyoto/src/*.cs

# kate: space-indent off; mixed-indent off
