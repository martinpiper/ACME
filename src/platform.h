//
// ACME - a crossassembler for producing 6502/65c02/65816 code.
// Copyright (C) 1998-2006 Marco Baye
// Have a look at "acme.c" for further info
//
// Platform specific stuff


// Amiga
#ifdef _AMIGA
#define PLATFORM_VERSION	"Ported to AmigaOS by Christoph Mammitzsch."
#include "_amiga.h"
#endif

// DOS and OS/2
#ifdef __DJGPP__
#define PLATFORM_VERSION	"Ported to DOS by Marco Baye."
#include "_dos.h"
#endif
#ifdef __OS2__
#define PLATFORM_VERSION	"Ported to OS/2 by Malte Eckhardt."
#include "_dos.h"
#endif

// Windows Win32 with MS Visual Studio 6.0 or better
#if defined(_WIN32) && _MSC_VER >= 1200
#define PLATFORM_VERSION	"Ported to Windows by Martin Piper"
#include "_win32.h"
#endif

// RISC OS
#ifdef __riscos__
#define PLATFORM_VERSION	"Ported to RISC OS by Marco Baye."
#include "_riscos.h"
#endif

// add further platform files here

// Unix/Linux/others (surprisingly also works on Windows)
#ifndef PLATFORM_VERSION
#define PLATFORM_VERSION	"Platform independent version.\nCurrent maintainer Krzysztof Dabrowski aka BruSH/ElysiuM"
#endif
#include "_std.h"
