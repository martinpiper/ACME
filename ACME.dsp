# Microsoft Developer Studio Project File - Name="ACME" - Package Owner=<4>
# Microsoft Developer Studio Generated Build File, Format Version 6.00
# ** DO NOT EDIT **

# TARGTYPE "Win32 (x86) Console Application" 0x0103

CFG=ACME - Win32 Debug
!MESSAGE This is not a valid makefile. To build this project using NMAKE,
!MESSAGE use the Export Makefile command and run
!MESSAGE 
!MESSAGE NMAKE /f "ACME.mak".
!MESSAGE 
!MESSAGE You can specify a configuration when running NMAKE
!MESSAGE by defining the macro CFG on the command line. For example:
!MESSAGE 
!MESSAGE NMAKE /f "ACME.mak" CFG="ACME - Win32 Debug"
!MESSAGE 
!MESSAGE Possible choices for configuration are:
!MESSAGE 
!MESSAGE "ACME - Win32 Release" (based on "Win32 (x86) Console Application")
!MESSAGE "ACME - Win32 Debug" (based on "Win32 (x86) Console Application")
!MESSAGE 

# Begin Project
# PROP AllowPerConfigDependencies 0
# PROP Scc_ProjName "Perforce Project"
# PROP Scc_LocalPath "."
CPP=cl.exe
RSC=rc.exe

!IF  "$(CFG)" == "ACME - Win32 Release"

# PROP BASE Use_MFC 0
# PROP BASE Use_Debug_Libraries 0
# PROP BASE Output_Dir "Release"
# PROP BASE Intermediate_Dir "Release"
# PROP BASE Target_Dir ""
# PROP Use_MFC 0
# PROP Use_Debug_Libraries 0
# PROP Output_Dir "Release"
# PROP Intermediate_Dir "Release"
# PROP Target_Dir ""
# ADD BASE CPP /nologo /W3 /GX /O2 /D "WIN32" /D "NDEBUG" /D "_CONSOLE" /D "_MBCS" /YX /FD /c
# ADD CPP /nologo /W3 /GX /O2 /D "WIN32" /D "NDEBUG" /D "_CONSOLE" /D "_MBCS" /YX /FD /c
# ADD BASE RSC /l 0x809 /d "NDEBUG"
# ADD RSC /l 0x809 /d "NDEBUG"
BSC32=bscmake.exe
# ADD BASE BSC32 /nologo
# ADD BSC32 /nologo
LINK32=link.exe
# ADD BASE LINK32 kernel32.lib user32.lib gdi32.lib winspool.lib comdlg32.lib advapi32.lib shell32.lib ole32.lib oleaut32.lib uuid.lib odbc32.lib odbccp32.lib kernel32.lib user32.lib gdi32.lib winspool.lib comdlg32.lib advapi32.lib shell32.lib ole32.lib oleaut32.lib uuid.lib odbc32.lib odbccp32.lib /nologo /subsystem:console /machine:I386
# ADD LINK32 kernel32.lib user32.lib gdi32.lib winspool.lib comdlg32.lib advapi32.lib shell32.lib ole32.lib oleaut32.lib uuid.lib odbc32.lib odbccp32.lib kernel32.lib user32.lib gdi32.lib winspool.lib comdlg32.lib advapi32.lib shell32.lib ole32.lib oleaut32.lib uuid.lib odbc32.lib odbccp32.lib /nologo /subsystem:console /machine:I386

!ELSEIF  "$(CFG)" == "ACME - Win32 Debug"

# PROP BASE Use_MFC 0
# PROP BASE Use_Debug_Libraries 1
# PROP BASE Output_Dir "Debug"
# PROP BASE Intermediate_Dir "Debug"
# PROP BASE Target_Dir ""
# PROP Use_MFC 0
# PROP Use_Debug_Libraries 1
# PROP Output_Dir "Debug"
# PROP Intermediate_Dir "Debug"
# PROP Ignore_Export_Lib 0
# PROP Target_Dir ""
# ADD BASE CPP /nologo /W3 /Gm /GX /ZI /Od /D "WIN32" /D "_DEBUG" /D "_CONSOLE" /D "_MBCS" /YX /FD /GZ /c
# ADD CPP /nologo /W3 /Gm /GX /ZI /Od /D "WIN32" /D "_DEBUG" /D "_CONSOLE" /D "_MBCS" /FR /YX /FD /GZ /c
# ADD BASE RSC /l 0x809 /d "_DEBUG"
# ADD RSC /l 0x809 /d "_DEBUG"
BSC32=bscmake.exe
# ADD BASE BSC32 /nologo
# ADD BSC32 /nologo
LINK32=link.exe
# ADD BASE LINK32 kernel32.lib user32.lib gdi32.lib winspool.lib comdlg32.lib advapi32.lib shell32.lib ole32.lib oleaut32.lib uuid.lib odbc32.lib odbccp32.lib kernel32.lib user32.lib gdi32.lib winspool.lib comdlg32.lib advapi32.lib shell32.lib ole32.lib oleaut32.lib uuid.lib odbc32.lib odbccp32.lib /nologo /subsystem:console /debug /machine:I386 /pdbtype:sept
# ADD LINK32 kernel32.lib user32.lib gdi32.lib winspool.lib comdlg32.lib advapi32.lib shell32.lib ole32.lib oleaut32.lib uuid.lib odbc32.lib odbccp32.lib kernel32.lib user32.lib gdi32.lib winspool.lib comdlg32.lib advapi32.lib shell32.lib ole32.lib oleaut32.lib uuid.lib odbc32.lib odbccp32.lib /nologo /subsystem:console /debug /machine:I386 /pdbtype:sept

!ENDIF 

# Begin Target

# Name "ACME - Win32 Release"
# Name "ACME - Win32 Debug"
# Begin Group "Source Files"

# PROP Default_Filter "cpp;c;cxx;rc;def;r;odl;idl;hpj;bat"
# Begin Source File

SOURCE=.\src\_dos.c
# End Source File
# Begin Source File

SOURCE=.\src\_riscos.c
# End Source File
# Begin Source File

SOURCE=.\src\_std.c
# End Source File
# Begin Source File

SOURCE=.\src\acme.c
# End Source File
# Begin Source File

SOURCE=.\src\alu.c
# End Source File
# Begin Source File

SOURCE=.\src\basics.c
# End Source File
# Begin Source File

SOURCE=.\src\cliargs.c
# End Source File
# Begin Source File

SOURCE=.\src\cpu.c
# End Source File
# Begin Source File

SOURCE=.\src\dynabuf.c
# End Source File
# Begin Source File

SOURCE=.\src\encoding.c
# End Source File
# Begin Source File

SOURCE=.\src\flow.c
# End Source File
# Begin Source File

SOURCE=.\src\global.c
# End Source File
# Begin Source File

SOURCE=.\src\input.c
# End Source File
# Begin Source File

SOURCE=.\src\label.c
# End Source File
# Begin Source File

SOURCE=.\src\macro.c
# End Source File
# Begin Source File

SOURCE=.\src\mnemo.c
# End Source File
# Begin Source File

SOURCE=.\src\output.c
# End Source File
# Begin Source File

SOURCE=.\src\platform.c
# End Source File
# Begin Source File

SOURCE=.\src\section.c
# End Source File
# Begin Source File

SOURCE=.\src\tree.c
# End Source File
# End Group
# Begin Group "Header Files"

# PROP Default_Filter "h;hpp;hxx;hm;inl"
# Begin Source File

SOURCE=.\src\_amiga.h
# End Source File
# Begin Source File

SOURCE=.\src\_dos.h
# End Source File
# Begin Source File

SOURCE=.\src\_riscos.h
# End Source File
# Begin Source File

SOURCE=.\src\_std.h
# End Source File
# Begin Source File

SOURCE=.\src\_win32.h
# End Source File
# Begin Source File

SOURCE=.\src\acme.h
# End Source File
# Begin Source File

SOURCE=.\src\alu.h
# End Source File
# Begin Source File

SOURCE=.\src\basics.h
# End Source File
# Begin Source File

SOURCE=.\src\cliargs.h
# End Source File
# Begin Source File

SOURCE=.\src\config.h
# End Source File
# Begin Source File

SOURCE=.\src\cpu.h
# End Source File
# Begin Source File

SOURCE=.\src\dynabuf.h
# End Source File
# Begin Source File

SOURCE=.\src\encoding.h
# End Source File
# Begin Source File

SOURCE=.\src\flow.h
# End Source File
# Begin Source File

SOURCE=.\src\global.h
# End Source File
# Begin Source File

SOURCE=.\src\input.h
# End Source File
# Begin Source File

SOURCE=.\src\label.h
# End Source File
# Begin Source File

SOURCE=.\src\macro.h
# End Source File
# Begin Source File

SOURCE=.\src\mnemo.h
# End Source File
# Begin Source File

SOURCE=.\src\output.h
# End Source File
# Begin Source File

SOURCE=.\src\platform.h
# End Source File
# Begin Source File

SOURCE=.\src\section.h
# End Source File
# Begin Source File

SOURCE=.\src\tree.h
# End Source File
# End Group
# Begin Group "Resource Files"

# PROP Default_Filter "ico;cur;bmp;dlg;rc2;rct;bin;rgs;gif;jpg;jpeg;jpe"
# Begin Group "Docs etc"

# PROP Default_Filter ""
# Begin Source File

SOURCE=.\docs\65816.txt
# End Source File
# Begin Source File

SOURCE=.\docs\AddrModes.txt
# End Source File
# Begin Source File

SOURCE=.\docs\AllPOs.txt
# End Source File
# Begin Source File

SOURCE=.\docs\Changes.txt
# End Source File
# Begin Source File

SOURCE=.\examples\me\const.a
# End Source File
# Begin Source File

SOURCE=.\docs\COPYING
# End Source File
# Begin Source File

SOURCE=.\examples\me\core.a
# End Source File
# Begin Source File

SOURCE=.\examples\me\cursor.a
# End Source File
# Begin Source File

SOURCE=.\examples\ddrv.a
# End Source File
# Begin Source File

SOURCE=.\examples\ddrv128.exp
# End Source File
# Begin Source File

SOURCE=.\examples\ddrv64.exp
# End Source File
# Begin Source File

SOURCE=.\docs\Errors.txt
# End Source File
# Begin Source File

SOURCE=.\docs\Example.txt
# End Source File
# Begin Source File

SOURCE=.\examples\me\file.a
# End Source File
# Begin Source File

SOURCE=.\ACME_Lib\6502\Help.txt
# End Source File
# Begin Source File

SOURCE=.\ACME_Lib\65816\Help.txt
# End Source File
# Begin Source File

SOURCE=.\ACME_Lib\Own\Help.txt
# End Source File
# Begin Source File

SOURCE=.\docs\Help.txt
# End Source File
# Begin Source File

SOURCE=.\docs\Illegals.txt
# End Source File
# Begin Source File

SOURCE=.\docs\Lib.txt
# End Source File
# Begin Source File

SOURCE=.\examples\macedit.a
# End Source File
# Begin Source File

SOURCE=.\examples\macedit.exp
# End Source File
# Begin Source File

SOURCE=.\examples\me\macros.a
# End Source File
# Begin Source File

SOURCE=.\examples\me\out.a
# End Source File
# Begin Source File

SOURCE=.\docs\QuickRef.txt
# End Source File
# Begin Source File

SOURCE=.\docs\Source.txt
# End Source File
# Begin Source File

SOURCE=.\ACME_Lib\6502\std.a
# End Source File
# Begin Source File

SOURCE=.\ACME_Lib\65816\std.a
# End Source File
# Begin Source File

SOURCE=.\examples\me\tables.bin
# End Source File
# Begin Source File

SOURCE=.\docs\Upgrade.txt
# End Source File
# Begin Source File

SOURCE=.\examples\me\vars.a
# End Source File
# End Group
# Begin Source File

SOURCE=.\CleanProject.bat
# End Source File
# Begin Source File

SOURCE=.\CleanProjectFully.bat
# End Source File
# Begin Source File

SOURCE=.\CleanProjectFullyWithAttrib.bat
# End Source File
# Begin Source File

SOURCE=.\DoBackup.bat
# End Source File
# Begin Source File

SOURCE=.\Test.a
# End Source File
# End Group
# Begin Group "Txt"

# PROP Default_Filter ""
# Begin Source File

SOURCE=.\ReadMe.txt
# End Source File
# End Group
# End Target
# End Project
