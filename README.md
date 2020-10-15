ACME
====

A 6502/6510 assembler used for my C64 and other 8 bit projects


VICE PDB Monitor (in the folder VICEPDBMonitor) is a remote monitor for newer version of VICE.
It allows better source level debugging than the standard VICE monitor. It does this by reading PDB (program database) files produced by this version of ACME.



Quick start
===========

This example will use ACME to assemble an example, start the visual debugger, start Vice in remote debugger mode, and show how single stepping works.

* Open a command prompt window this, I usually use: "Windows key + R" type "cmd" then press return
* In the command prompt window, use the "cd" command to change to the directory of this ACME archive, for example: cd c:\Work\ACME
* Next assemble the Test.a example file using: Release\ACME.exe -v3 Test.a
* Next start the remote debugger with the "Test.pdb" file: VICEPDBMonitor\bin\Release\VICEPDBMonitor.exe %cd%\Test.pdb
  * The "Test.pdb" is the debug information file written by ACME using the "!pdb" pseudo-op command found in "Test.a"
  * The "%cd%" is used to pass the full current directory path to the remote debugger application.
  * The vidual debugger window should show: Waiting to connect...
* Next start Vice, with remote monitor mode, and using the full path of the assembled output file "Test.prg": C:\Downloads\WinVICE-3.1-x86-r34062\WinVICE-3.1-x86-r34062\x64sc.exe -autostartprgmode 1 -remotemonitor %cd%\Test.prg
* The visual debugger should quickly catch Vice as it starts, by connecting to the default TCP port 6510 for the remote monitor, the Vice window should freeze (usually during the black screen for the C64 boot).
  * The top window frame in the visual debugger should show address, register, and status details.
  * The bottom edit input portion of the window should be active and allow a command to be typed in, use the remote monitor command: break main
  * The break window, on the right, should indicate a new breakpoint added at "0400" which is the address of the "main" label.
* To continue the Vice C64 boot, enter the visual debugger command: x
  * Vice should continue to boot to the BASIC ready prompt and the top of the screen should show the data from "Test.prg"
* At this point, the visual debugger can execute the code using the command: g main
  * Or BASIC can start the code using: sys 1024
  * The visual debugger window should update to show the source code with the first line of the "main' routine highlighted.
* The visual debugger "Step over" or "Step in" buttons on the right of the window can be used as expected.
* Use the "Go" button, the Test.prg demo code should show rapidly changing border colours and some screen characters being updated.

Visual Debugger functions
=========================

* The "Disassembly" check box will enable mixed disassembly and source file display mode, if source is enabled. Or normal disassembly from Vice is source is not enabled.
* The "Source" check box will enable source code display, by default this is enabled if source can be loaded.
* The "+Dump" check box will add live memory data dump information (4 bytes) for the labels view (top right) and add memory data dump information mixed into the assembly view, where the memory address can be calculated.
  * This view is very useful for quickly checking the contents of memory as you are stepping through code.
* The "Used labels" check box will filter the labels view to only those labels that are visible from the scope of the assembled source code from the current program counter point of view.
* "Script panel" opens a local or remote script execution window.
* "Sprites" shows a sprite memory debug window.
* "Chars" shows a character memory debug window.
* "Screen" shows a screen debug window.
* "Calculator" ise a useful memory address calculator.
* "CHIS" is a CPU execution history window, with very useful indentation and IRQ filtering.


Code profiling
==============

* Press "Break" to stop the execution
* Tick the "Exec use" check box to enable "execution profiling" and "Access use" to enable memory read/write access profiling.
* Now press "Profile clear" and the code should continue to run.
* When you want to get one profile snapshot press "Profile add". The execution will quickly pause, then continue.
  * The labels view will update using data gathered for that current profile snapshot
  * Where the label name is prefixed with "A*number*" to indicate that data at the label has been accessed *number* times.
  * Where the label name is prefixed with "E*number*" to indicate that code at the label has been executed *number* times.
  * For example this indicates the label "test" at location $42e has has its code executed 13 times and memory accessed (read/write) 2 times and : E13:A2:test $42E
