#include <stdio.h>
#include <stdlib.h>
#include <map>
#include <set>
#include <string>
#include <fstream>

extern "C" {
#include "acme.h"
#include "input.h"
#include "global.h"
}

static int device = 0;

struct DebugInfo
{
	DebugInfo() : mLineNumber( 0 )
	{
	}
	std::string mFilename;
	int mLineNumber;
	int mZone;
	int mDevice;
	bool mIsPseudo;
};

std::multimap<int , DebugInfo> sAddrMap;

extern void PDBInit( void )
{
	sAddrMap.clear();
	device = 0;
}

extern "C" void PDBAddFileLineToAddr( const int address , const char *filename , const int lineNumber , const int zone , const bool isPseudo )
{
	DebugInfo debug;
	debug.mFilename = filename;
	debug.mLineNumber = lineNumber;
	debug.mZone = zone;
	debug.mDevice = device;
	debug.mIsPseudo = isPseudo;

	std::pair<std::multimap<int , DebugInfo>::iterator , std::multimap<int , DebugInfo>::iterator> range = sAddrMap.equal_range(address);
	while (range.first != range.second)
	{
		DebugInfo &test = range.first->second;
		if ( (test.mLineNumber == debug.mLineNumber) && (test.mZone == debug.mZone) && (test.mDevice == debug.mDevice) && (test.mFilename == debug.mFilename))
		{
			return;
		}
		range.first++;
	}

	sAddrMap.insert(std::pair<int,DebugInfo>(address,debug));
}

extern "C" void PDBSave( FILE *fp )
{
	std::map< std::string , int > filenameIndex;
	int theIndex = 0;

	std::multimap<int , DebugInfo>::iterator st = sAddrMap.begin();
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
		fprintf( fp , "$%04x:%d:%d:%d:%d\n" , st->first , debug.mZone , filenameIndex[ debug.mFilename ] , debug.mLineNumber , debug.mDevice );

		st++;
	}
}

extern "C" void PDBSave2( FILE *fp )
{
	int previousUsed = -1;
	int countBlocks = 0;
	std::multimap<int , DebugInfo>::iterator st = sAddrMap.begin();
	while ( st != sAddrMap.end() )
	{
		DebugInfo &debug = st->second;
		// Only display pseudo addresses for device 0, or non-pseudo addresses
		if (!debug.mIsPseudo || (debug.mIsPseudo && debug.mDevice == 0))
		{
			int addr = st->first;
			if (addr > 0 && addr > (previousUsed+1))
			{
				countBlocks++;
			}

			previousUsed = addr;
		}

		st++;
	}
	if (previousUsed + 1 < 0xffff)
	{
		countBlocks++;
	}
	// Now write the blocks
	previousUsed = -1;
	fprintf( fp , "FREEMAP:%d\n" , countBlocks);
	st = sAddrMap.begin();
	while ( st != sAddrMap.end() )
	{
		DebugInfo &debug = st->second;
		if (!debug.mIsPseudo || (debug.mIsPseudo && debug.mDevice == 0))
		{
			int addr = st->first;
			if (addr > 0 && addr > (previousUsed+1))
			{
				fprintf( fp , "$%04x-$%04x:$%04x\n" , previousUsed + 1 , addr-1 , (addr-1) - previousUsed );
			}

			previousUsed = addr;
		}

		st++;
	}
	if (previousUsed + 1 < 0xffff)
	{
		fprintf( fp , "$%04x-$ffff:$%x\n" , previousUsed + 1 , 65535 - previousUsed);
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



extern "C" void SortFile( const char *filename )
{
	std::set<std::string> textLines;

	std::ifstream file(filename);
	if (file.is_open())
	{
		std::string line;
		while (std::getline(file , line))
		{
			textLines.insert(line);
		}
		
		file.close();
	}


	std::ofstream fileOut(filename);

	if (fileOut.is_open())
	{
		std::set<std::string>::iterator st = textLines.begin();
		while (st != textLines.end())
		{
			std::string line = *st++;
			fileOut << line << std::endl;
		}

		fileOut.close();
	}
}

extern "C" void SetDevice( const int idevice )
{
	device = idevice;
}

