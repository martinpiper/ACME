#include <stdio.h>
#include <stdlib.h>
#include <map>
#include <string>

extern "C" {
#include "acme.h"
#include "input.h"
}

struct DebugInfo
{
	DebugInfo() : mLineNumber( 0 )
	{
	}
	std::string mFilename;
	int mLineNumber;
	int mZone;
};

std::map<int , DebugInfo> sAddrMap;

extern void PDBInit( void )
{
	sAddrMap.clear();
}

extern "C" void PDBAddFileLineToAddr( const int address , const char *filename , const int lineNumber , const int zone )
{
	DebugInfo &debug = sAddrMap[ address ];
	debug.mFilename = filename;
	debug.mLineNumber = lineNumber;
	debug.mZone = zone;
}

extern "C" void PDBSave( FILE *fp )
{
	std::map< std::string , int > filenameIndex;
	int theIndex = 0;

	std::map<int , DebugInfo>::iterator st = sAddrMap.begin();
	while ( st != sAddrMap.end() )
	{
		DebugInfo &debug = st->second;
		std::pair< std::map< std::string , int >::iterator , bool > inserted = filenameIndex.insert( std::pair< std::string , int  >( debug.mFilename , theIndex ) );
		if ( inserted.second )
		{
			theIndex++;
		}

		st++;
	}

	fprintf( fp , "INCLUDES:%d\n" , (int) gNumLibraryIncludes );
	int i;
	for ( i = 0 ; i < gNumLibraryIncludes ; i++ )
	{
		fprintf( fp , "%s\n" , gLibraryIncludes[i] );
	}

	fprintf( fp , "FILES:%d\n" , (int) filenameIndex.size() );
	std::map< std::string , int >::iterator st2 = filenameIndex.end();
	while ( st2 != filenameIndex.begin() )
	{
		st2--;
		fprintf( fp , "%d:%s\n" , st2->second , st2->first.c_str() );
	}

	fprintf( fp , "ADDRS:%d\n" , (int) sAddrMap.size() );
	st = sAddrMap.begin();
	while ( st != sAddrMap.end() )
	{
		DebugInfo &debug = st->second;
		fprintf( fp , "$%x:%d:%d:%d\n" , st->first , debug.mZone , filenameIndex[ debug.mFilename ] , debug.mLineNumber );

		st++;
	}
}
