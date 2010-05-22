#!/bin/sh

export LD_LIBRARY_PATH="@CMAKE_INSTALL_PREFIX@/lib/assemblygen:$LD_LIBRARY_PATH"
exec @MONO_EXECUTABLE@ @CMAKE_INSTALL_PREFIX@/lib/assemblygen/assemblygen.exe "$@"
