#include <stdio.h>
#include <stdlib.h>
#include <map>
#include <string>

extern "C" {
#include "acme.h"
#include "input.h"
#include "global.h"
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

static std::string buildLabelIdentifier(const char *label, const char *filename, int linenumber, int zone)
{
	std::string theLabel = label;
	theLabel += ":";
	theLabel += filename;
	theLabel += ":";
	theLabel += std::to_string(linenumber);
	theLabel += ":";
	theLabel += std::to_string(zone);
	return theLabel;
}

std::map<std::string , int> sLabelToPass;
extern "C" int GetLabelAge(const int currentPass, const char *label, const char *filename, int linenumber, int zone)
{
	// Make a unique reference for this occurrence so it can be tracked precisely
	std::string theLabel = buildLabelIdentifier(label,filename,linenumber,zone);
	std::map<std::string , int>::iterator found = sLabelToPass.find(theLabel);
	if (found != sLabelToPass.end())
	{
		return currentPass - found->second;
	}
	sLabelToPass.insert(std::pair<std::string,int>(theLabel,currentPass));
	return 0;
}

std::map<std::string , int> sLabelLastValue;
std::map<std::string , int> sLabelDifferentCount;
extern "C" int IsLabelSameAsLastValue(const int theValue, const char *label, const char *filename, int linenumber, int zone)
{
	// Make a unique reference for this occurrence so it can be tracked precisely
	std::string theLabel = buildLabelIdentifier(label,filename,linenumber,zone);
	std::map<std::string , int>::iterator found = sLabelLastValue.find(theLabel);
	if (found != sLabelLastValue.end())
	{
		bool same = (found->second == theValue);
		if (!same)
		{
			sLabelDifferentCount[theLabel] = sLabelDifferentCount[theLabel] + 1;
			if(Process_verbosity > 3)
			{
				char message[1024];
				sprintf(message, "Detected result change: %d: %s : From %d to %d\n" , sLabelDifferentCount[theLabel] , theLabel.c_str() , found->second , theValue);
				Throw_warning(message);
			}
			// And update it after the comparison
			found->second = theValue;
			return FALSE;
		}
		return TRUE;
	}
	sLabelLastValue.insert(std::pair<std::string,int>(theLabel,theValue));
	sLabelDifferentCount[theLabel] = 0;
	return TRUE;
}

extern "C" int GetLabelNumberDifferences(const char *label, const char *filename, int linenumber, int zone)
{
	std::string theLabel = buildLabelIdentifier(label,filename,linenumber,zone);
	return sLabelDifferentCount[theLabel];
}
