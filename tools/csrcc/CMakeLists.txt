find_package(Qt4 REQUIRED COMPONENTS QtCore QtXml)

SET(csrcc_SRCS rcc.cpp main.cpp )

INCLUDE_DIRECTORIES (${QT_INCLUDES})
ADD_EXECUTABLE(csrcc ${csrcc_SRCS})
TARGET_LINK_LIBRARIES (csrcc ${QT_QTCORE_LIBRARY} ${QT_QTXML_LIBRARY})

if (WIN32 AND CMAKE_COMPILER_IS_GNUCC)
	# Realign the stack, for compatibility with an older ABI.
	set_target_properties (csrcc PROPERTIES COMPILE_FLAGS -mstackrealign)
endif (WIN32 AND CMAKE_COMPILER_IS_GNUCC)

INSTALL(TARGETS csrcc DESTINATION ${CMAKE_INSTALL_PREFIX}/bin)
