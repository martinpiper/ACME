Feature: Test python mixed with assembly

  This executes ACME and tests Python integration.

  Background:
    Given I have a simple overclocked 6502 system
    And I am using C64 processor port options
    Given add C64 hardware



  Scenario Outline: Execute assembly with python additions

    And I run the command line: <build>\acme.exe -f plain -o target\bin1.bin TestPythonBin1.a
    And I run the command line: <build>\acme.exe -f plain -o target\bin2.bin TestPythonBin2.a
    And I run the command line: <build>\acme.exe -f plain -o target\bin3.bin TestPythonBin3.a
    And I run the command line: <build>\acme.exe -f plain -o target\bin4.bin TestPythonBin4.a
    And I run the command line: <build>\acme.exe --pdb target\TestPython.pdb --labeldump target\TestPython.lbl TestPython.a

    Given open file "target\TestPython.pdb" for reading
    Then expect the next line to contain "INCLUDES:0"
    Then expect the next line to contain "FILES:2"
    Then expect the next line to contain "1:TestPython.py"
    Then expect the next line to contain "0:TestPython.a"
    Then expect the next line to contain "ADDRS:1088"
    # Check the correct file and line number references are output for !scriptpythonfile and !scriptpythoninline commands
    Then expect the next line to contain "$400:1:0:15"
    Then expect the next line to contain "$401:1:1:4"
    Then expect the next line to contain "$402:1:1:4"
    Then expect the next line to contain "$403:1:1:4"
    Then expect the next line to contain "$404:1:1:4"
    Then expect the next line to contain "$405:1:1:4"
    Then expect the next line to contain "$406:1:1:5"
    Then expect the next line to contain "$407:1:1:5"
    Then expect the next line to contain "$408:1:1:5"
    Then expect the next line to contain "$409:1:1:6"
    Then expect the next line to contain "$40a:1:0:22"
    Then expect the next line to contain "$40b:1:0:22"
    Then expect the next line to contain "$40c:1:0:22"
    Then expect the next line to contain "$40d:1:0:22"
    Then expect the next line to contain "$40e:1:0:22"
    Then expect the next line to contain "$40f:1:1:4"


    And I load prg "TestPython.prg"
    And I load labels "target\TestPython.lbl"

    # Tests expected assembly output in memory
    When I hex dump memory between $0400 and $0500
    Then property "test.BDD6502.lastHexDump" must contain string "400: ea e6 02 ee 00 04 ee 20  d0 60 e6 03 ee 01 04 e6"
    Then property "test.BDD6502.lastHexDump" must contain string "410: 02 ee 00 04 ee 20 d0 60  e6 02 ee 00 04 ee 20 d0"
    Then property "test.BDD6502.lastHexDump" must contain string "420: 60 e6 02 ee 00 04 ee 20  d0 60 a5 a5 a5 a5 a5 a5"

    When I hex dump memory between TestMemory and TestMemoryEnd
    Then property "test.BDD6502.lastHexDump" must contain string "1000: 0a 7b 00 ed 02 03 04 05  06 07 08 09 0a 08 09 0a"
    Then property "test.BDD6502.lastHexDump" must contain string "1010: 0b 0c 0d ca cb 45 46 47"

    When I execute the procedure at start for no more than 10 instructions

    When I hex dump memory between $0400 and $0500
    Then property "test.BDD6502.lastHexDump" must contain string "400: eb e6 02 ee 00 04 ee 20  d0 60 e6 03 ee 01 04 e6"
    Then property "test.BDD6502.lastHexDump" must contain string "410: 02 ee 00 04 ee 20 d0 60  e6 02 ee 00 04 ee 20 d0"
    Then property "test.BDD6502.lastHexDump" must contain string "420: 60 e6 02 ee 00 04 ee 20  d0 60 a5 a5 a5 a5 a5 a5"

    When I hex dump memory between $2000 and $2110
    Then property "test.BDD6502.lastHexDump" must contain string "2000: 00 01 02 03 04 05 06 07  08 09 0a 0b 0c 0d 0e 0f"
    Then property "test.BDD6502.lastHexDump" must contain string "2010: 10 11 12 13 14 15 16 17  18 19 1a 1b 1c 1d 1e 1f"
    Then property "test.BDD6502.lastHexDump" must contain string "2020: 20 21 22 23 24 25 26 27  28 29 2a 2b 2c 2d 2e 2f"
    Then property "test.BDD6502.lastHexDump" must contain string "2030: 30 31 32 33 34 35 36 37  38 39 3a 3b 3c 3d 3e 3f"
    Then property "test.BDD6502.lastHexDump" must contain string "2040: 40 41 42 43 44 45 46 47  48 49 4a 4b 4c 4d 4e 4f"
    Then property "test.BDD6502.lastHexDump" must contain string "2050: 50 51 52 53 54 55 56 57  58 59 5a 5b 5c 5d 5e 5f"
    Then property "test.BDD6502.lastHexDump" must contain string "2060: 60 61 62 63 64 65 66 67  68 69 6a 6b 6c 6d 6e 6f"
    Then property "test.BDD6502.lastHexDump" must contain string "2070: 70 71 72 73 74 75 76 77  78 79 7a 7b 7c 7d 7e 7f"
    Then property "test.BDD6502.lastHexDump" must contain string "2080: 80 81 82 83 84 85 86 87  88 89 8a 8b 8c 8d 8e 8f"
    Then property "test.BDD6502.lastHexDump" must contain string "2090: 90 91 92 93 94 95 96 97  98 99 9a 9b 9c 9d 9e 9f"
    Then property "test.BDD6502.lastHexDump" must contain string "20a0: a0 a1 a2 a3 a4 a5 a6 a7  a8 a9 aa ab ac ad ae af"
    Then property "test.BDD6502.lastHexDump" must contain string "20b0: b0 b1 b2 b3 b4 b5 b6 b7  b8 b9 ba bb bc bd be bf"
    Then property "test.BDD6502.lastHexDump" must contain string "20c0: c0 c1 c2 c3 c4 c5 c6 c7  c8 c9 ca cb cc cd ce cf"
    Then property "test.BDD6502.lastHexDump" must contain string "20d0: d0 d1 d2 d3 d4 d5 d6 d7  d8 d9 da db dc dd de df"
    Then property "test.BDD6502.lastHexDump" must contain string "20e0: e0 e1 e2 e3 e4 e5 e6 e7  e8 e9 ea eb ec ed ee ef"
    Then property "test.BDD6502.lastHexDump" must contain string "20f0: f0 f1 f2 f3 f4 f5 f6 f7  f8 f9 fa fb fc fd fe ff"
    Then property "test.BDD6502.lastHexDump" must contain string "2100: a5 a5 a5 a5 a5 a5 a5 a5  a5 a5 a5 a5 a5 a5 a5 a5"

    When I hex dump memory between $2200 and $2310
    Then property "test.BDD6502.lastHexDump" must contain string "2200: ff fe fd fc fb fa f9 f8  f7 f6 f5 f4 f3 f2 f1 f0"
    Then property "test.BDD6502.lastHexDump" must contain string "2210: ef ee ed ec eb ea e9 e8  e7 e6 e5 e4 e3 e2 e1 e0"
    Then property "test.BDD6502.lastHexDump" must contain string "2220: df de dd dc db da d9 d8  d7 d6 d5 d4 d3 d2 d1 d0"
    Then property "test.BDD6502.lastHexDump" must contain string "2230: cf ce cd cc cb ca c9 c8  c7 c6 c5 c4 c3 c2 c1 c0"
    Then property "test.BDD6502.lastHexDump" must contain string "2240: bf be bd bc bb ba b9 b8  b7 b6 b5 b4 b3 b2 b1 b0"
    Then property "test.BDD6502.lastHexDump" must contain string "2250: af ae ad ac ab aa a9 a8  a7 a6 a5 a4 a3 a2 a1 a0"
    Then property "test.BDD6502.lastHexDump" must contain string "2260: 9f 9e 9d 9c 9b 9a 99 98  97 96 95 94 93 92 91 90"
    Then property "test.BDD6502.lastHexDump" must contain string "2270: 8f 8e 8d 8c 8b 8a 89 88  87 86 85 84 83 82 81 80"
    Then property "test.BDD6502.lastHexDump" must contain string "2280: 7f 7e 7d 7c 7b 7a 79 78  77 76 75 74 73 72 71 70"
    Then property "test.BDD6502.lastHexDump" must contain string "2290: 6f 6e 6d 6c 6b 6a 69 68  67 66 65 64 63 62 61 60"
    Then property "test.BDD6502.lastHexDump" must contain string "22a0: 5f 5e 5d 5c 5b 5a 59 58  57 56 55 54 53 52 51 50"
    Then property "test.BDD6502.lastHexDump" must contain string "22b0: 4f 4e 4d 4c 4b 4a 49 48  47 46 45 44 43 42 41 40"
    Then property "test.BDD6502.lastHexDump" must contain string "22c0: 3f 3e 3d 3c 3b 3a 39 38  37 36 35 34 33 32 31 30"
    Then property "test.BDD6502.lastHexDump" must contain string "22d0: 2f 2e 2d 2c 2b 2a 29 28  27 26 25 24 23 22 21 20"
    Then property "test.BDD6502.lastHexDump" must contain string "22e0: 1f 1e 1d 1c 1b 1a 19 18  17 16 15 14 13 12 11 10"
    Then property "test.BDD6502.lastHexDump" must contain string "22f0: 0f 0e 0d 0c 0b 0a 09 08  07 06 05 04 03 02 01 00"
    Then property "test.BDD6502.lastHexDump" must contain string "2300: a5 a5 a5 a5 a5 a5 a5 a5  a5 a5 a5 a5 a5 a5 a5 a5"

    When I hex dump memory between $2400 and $2510
    Then property "test.BDD6502.lastHexDump" must contain string "2400: 00 01 02 03 04 05 06 07  08 09 0a 0b 0c 0d 0e 0f"
    Then property "test.BDD6502.lastHexDump" must contain string "2410: 10 11 12 13 14 15 16 17  18 19 1a 1b 1c 1d 1e 1f"
    Then property "test.BDD6502.lastHexDump" must contain string "2420: 20 21 23 24 25 26 27 28  29 2a 2b 2c 2d 2e 2f 30"
    Then property "test.BDD6502.lastHexDump" must contain string "2430: 31 32 33 34 35 36 37 38  39 3a 3b 3c 3d 3e 3f 40"
    Then property "test.BDD6502.lastHexDump" must contain string "2440: 41 42 43 44 45 46 47 48  49 4a 4b 4c 4d 4e 4f 50"
    Then property "test.BDD6502.lastHexDump" must contain string "2450: 51 52 53 54 55 56 57 58  59 5a 5b 5c 5d 5e 5f 60"
    Then property "test.BDD6502.lastHexDump" must contain string "2460: 61 62 63 64 65 66 67 68  69 6a 6b 6c 6d 6e 6f 70"
    Then property "test.BDD6502.lastHexDump" must contain string "2470: 71 72 73 74 75 76 77 78  79 7a 7b 7c 7d 7e 7f 80"
    Then property "test.BDD6502.lastHexDump" must contain string "2480: 81 82 83 84 85 86 87 88  89 8a 8b 8c 8d 8e 8f 90"
    Then property "test.BDD6502.lastHexDump" must contain string "2490: 91 92 93 94 95 96 97 98  99 9a 9b 9c 9d 9e 9f a0"
    Then property "test.BDD6502.lastHexDump" must contain string "24a0: a1 a2 a3 a4 a5 a6 a7 a8  a9 aa ab ac ad ae af b0"
    Then property "test.BDD6502.lastHexDump" must contain string "24b0: b1 b2 b3 b4 b5 b6 b7 b8  b9 ba bb bc bd be bf c0"
    Then property "test.BDD6502.lastHexDump" must contain string "24c0: c1 c2 c3 c4 c5 c6 c7 c8  c9 ca cb cc cd ce cf d0"
    Then property "test.BDD6502.lastHexDump" must contain string "24d0: d1 d2 d3 d4 d5 d6 d7 d8  d9 da db dc dd de df e0"
    Then property "test.BDD6502.lastHexDump" must contain string "24e0: e1 e2 e3 e4 e5 e6 e7 e8  e9 ea eb ec ed ee ef f0"
    Then property "test.BDD6502.lastHexDump" must contain string "24f0: f1 f2 f3 f4 f5 f6 f7 f8  f9 fa fb fc fd fe ff a5"
    Then property "test.BDD6502.lastHexDump" must contain string "2500: a5 a5 a5 a5 a5 a5 a5 a5  a5 a5 a5 a5 a5 a5 a5 a5"

    When I hex dump memory between $2600 and $2710
    Then property "test.BDD6502.lastHexDump" must contain string "2600: 00 01 02 03 04 05 06 07  08 09 0a 0b 0c 0d 0e 0f"
    Then property "test.BDD6502.lastHexDump" must contain string "2610: 10 11 12 13 14 15 16 17  18 19 1a 1b 1c 1d 1e 1f"
    Then property "test.BDD6502.lastHexDump" must contain string "2620: 20 21 22 23 24 25 26 28  29 2a 2b 2c 2d 2e 2f 30"
    Then property "test.BDD6502.lastHexDump" must contain string "2630: 31 32 33 34 35 36 37 38  39 3a 3b 3c 3d 3e 3f 40"
    Then property "test.BDD6502.lastHexDump" must contain string "2640: 41 42 43 44 45 46 47 48  49 4a 4b 4c 4d 4e 4f 50"
    Then property "test.BDD6502.lastHexDump" must contain string "2650: 51 52 53 54 55 56 57 58  59 5a 5b 5c 5d 5e 5f 60"
    Then property "test.BDD6502.lastHexDump" must contain string "2660: 61 62 63 64 65 66 67 68  69 6a 6b 6c 6d 6e 6f 70"
    Then property "test.BDD6502.lastHexDump" must contain string "2670: 71 72 73 74 75 76 77 78  79 7a 7b 7c 7d 7e 7f 80"
    Then property "test.BDD6502.lastHexDump" must contain string "2680: 81 82 83 84 85 86 87 88  89 8a 8b 8c 8d 8e 8f 90"
    Then property "test.BDD6502.lastHexDump" must contain string "2690: 91 92 93 94 95 96 97 98  99 9a 9b 9c 9d 9e 9f a0"
    Then property "test.BDD6502.lastHexDump" must contain string "26a0: a1 a2 a3 a4 a5 a6 a7 a8  a9 aa ab ac ad ae af b0"
    Then property "test.BDD6502.lastHexDump" must contain string "26b0: b1 b2 b3 b4 b5 b6 b7 b8  b9 ba bb bc bd be bf c0"
    Then property "test.BDD6502.lastHexDump" must contain string "26c0: c1 c2 c3 c4 c5 c6 c7 c8  c9 ca cb cc cd ce cf d0"
    Then property "test.BDD6502.lastHexDump" must contain string "26d0: d1 d2 d3 d4 d5 d6 d7 d8  d9 da db dc dd de df e0"
    Then property "test.BDD6502.lastHexDump" must contain string "26e0: e1 e2 e3 e4 e5 e6 e7 e8  e9 ea eb ec ed ee ef f0"
    Then property "test.BDD6502.lastHexDump" must contain string "26f0: f1 f2 f3 f4 f5 f6 f7 f8  f9 fa fb fc fd fe ff 00"
    # Because this is the end of loaded memory, not covered by !initmem $a5
    Then property "test.BDD6502.lastHexDump" must contain string "2700: 00 00 00 00 00 00 00 00  00 00 00 00 00 00 00 00"

  Examples:
    | build   |
    | Debug   |
    | Release |
