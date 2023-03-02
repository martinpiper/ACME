Feature: Test forward reference assembly

  This executes ACME and tests a complex forward reference can be resolved.

  Background:
    Given I have a simple overclocked 6502 system
    And I am using C64 processor port options
    Given add C64 hardware
    And I enable uninitialised memory read protection with immediate fail



  Scenario Outline: Execute assembly

    And I run the command line: <build>\acme.exe -v9 --pdb target\TestForForwardReference.pdb --labeldump target\TestForForwardReference.lbl TestForForwardReference.a
    Then property "test.BDD6502.lastProcessOutput" must contain string "Detected result change: 1: length:TestForForwardReference.a:19:1 : From 0 to 24"
    
    Given open file "target\TestForForwardReference.pdb" for reading
    Then expect the next line to contain "INCLUDES:0"
    Then expect the next line to contain "FILES:1"
    Then expect the next line to contain "0:TestForForwardReference.a"
    Then expect the next line to contain "ADDRS:288"
    Then expect the next line to contain "$6a4:1:0:11"
    Then expect the next line to contain "$6a5:1:0:11"
    Then expect the next line to contain "$6a6:1:0:13"

    And I load prg "TestForForwardReference.prg"
    And I load labels "target\TestForForwardReference.lbl"

    # Tests expected assembly output in memory
    When I hex dump memory between 1700 and 1800
    Then property "test.BDD6502.lastHexDump" must contain string "6a4: a2 1c bd 94 07 9d 00 04  ca 10 f7 ad 94 07 8d 00"
    Then property "test.BDD6502.lastHexDump" must contain string "6b4: 05 ad 95 07 8d 01 05 ad  96 07 8d 02 05 ad 97 07"
    Then property "test.BDD6502.lastHexDump" must contain string "6c4: 8d 03 05 ad 98 07 8d 04  05 ad 99 07 8d 05 05 ad"
    Then property "test.BDD6502.lastHexDump" must contain string "6d4: 9a 07 8d 06 05 ad 9b 07  8d 07 05 ad 9c 07 8d 08"

    When I hex dump memory between $400 and $500
    Then property "test.BDD6502.lastHexDump" must contain string "400: 00 00 00 00 00 00 00 00  00 00 00 00 00 00 00 00"


    When I execute the procedure at start for no more than 194 instructions

    When I hex dump memory between 1700 and 1800
    Then property "test.BDD6502.lastHexDump" must contain string "6a4: a2 1c bd 94 07 9d 00 04  ca 10 f7 ad 94 07 8d 00"
    Then property "test.BDD6502.lastHexDump" must contain string "6b4: 05 ad 95 07 8d 01 05 ad  96 07 8d 02 05 ad 97 07"
    Then property "test.BDD6502.lastHexDump" must contain string "6c4: 8d 03 05 ad 98 07 8d 04  05 ad 99 07 8d 05 05 ad"
    Then property "test.BDD6502.lastHexDump" must contain string "6d4: 9a 07 8d 06 05 ad 9b 07  8d 07 05 ad 9c 07 8d 08"

    # And check the intended result
    When I hex dump memory between $400 and $500
    Then property "test.BDD6502.lastHexDump" must contain string "400: 54 08 09 13 20 09 13 20  2a 16 05 12 19 2a 20 15"
    Then property "test.BDD6502.lastHexDump" must contain string "410: 07 0c 19 20 03 0f 04 05  2e 2e 2e 2e 2e 00 00 00"

  Examples:
    | build   |
    | Debug   |
    | Release |
