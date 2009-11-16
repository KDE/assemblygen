all: main.exe

libsmokeloader.so: smokeloader.cpp
	g++ `pkg-config --libs --cflags QtCore` -fPIC -I/opt/kde4/include -shared -o libsmokeloader.so smokeloader.cpp

CS_SOURCE=Smoke.cs SmokeMethods.cs ByteArrayManager.cs MethodsGenerator.cs ClassesGenerator.cs Translator.cs \
	  SmokeSupport.cs main.cs

main.exe: libsmokeloader.so $(CS_SOURCE)
	gmcs -debug -unsafe -out:main.exe $(CS_SOURCE)
