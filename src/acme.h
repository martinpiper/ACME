//
// ACME - a crossassembler for producing 6502/65c02/65816 code.
// Copyright (C) 1998-2006 Marco Baye
// Have a look at "acme.c" for further info
//
// Main definitions
#ifndef acme_H
#define acme_H

#include "config.h"
#include <stdio.h>


// Variables
extern const char*	labeldump_filename;
extern const char*	vicelabeldump_filename;
extern const char*	PDB_filename;
extern const char*	output_filename;
extern int	labeldump_allSections;
extern int previouscontext_enable;
// maximum recursion depth for macro calls and "!source"
extern signed long	macro_recursions_left;
extern signed long	source_recursions_left;
extern zone_t		zone_max;

extern char *gLibraryIncludes[256];
extern int gNumLibraryIncludes;


// Prototypes

// Tidy up before exiting by saving label dump
extern int	ACME_finalize(int exit_code);

extern void PDBInit( void );
extern void PDBAddFileLineToAddr( const int address , const char *filename , const int lineNumber , const int zone );
extern void PDBSave( FILE *fp );
extern int GetLabelAge(const int currentPass, const char *label, const char *filename, int linenumber, int zone);
extern int IsLabelSameAsLastValue(const int theValue, const char *label, const char *filename, int linenumber, int zone);
extern int GetLabelNumberDifferences(const char *label, const char *filename, int linenumber, int zone);
extern void SortFile( const char *filename );

#endif
