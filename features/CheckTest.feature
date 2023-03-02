Feature: Test general assembly

  This executes ACME and tests integration.

  Background:
    Given I have a simple overclocked 6502 system
    And I am using C64 processor port options
    Given add C64 hardware



  Scenario Outline: Execute assembly from general assembly test 

    And I run the command line: <build>\acme.exe -v9 Test.a
    Then property "test.BDD6502.lastProcessOutput" must contain string "Parsing source file 'Test.a'"
    Then property "test.BDD6502.lastProcessOutput" must contain string "Parsing source file 'ACME_Lib/6502/std.a'"

    Given open file "Test.pdb" for reading
    Then expect the next line to contain "INCLUDES:0"
    Then expect the next line to contain "FILES:2"
    Then expect the next line to contain "1:Test.a"
    Then expect the next line to contain "0:ACME_Lib/6502/std.a"
    Then expect the next line to contain "ADDRS:251"
    Then expect the next line to contain "$400:2:0:27"

    Given open file "Test.lbl" for reading
    Then expect the next line to contain "al C:0001 .Lib_6502_std_a"
    Then expect the next line to contain "al C:0001 .foo"
    Then expect the next line to contain "al C:0408 .GlobalLabel"
    Then expect the next line to contain "al C:0408 .j"
    Then expect the next line to contain "al C:0408 .main"
    Then expect the next line to contain "al C:0422 .loop"
    Then expect the next line to contain "al C:042f .localLabel"


    And I load prg "Test.prg"
    And I load labels "Test.map"

    # Tests expected assembly output in memory
    When I hex dump memory between $0400 and $0500
    Then property "test.BDD6502.lastHexDump" must contain string "400: ee 00 07 d0 03 ee 01 07  78 a9 00 8d 00 04 a9 00"
    Then property "test.BDD6502.lastHexDump" must contain string "410: 8d 00 05 a9 00 8d 00 05  a9 00 8d 00 05 a9 00 8d"
    Then property "test.BDD6502.lastHexDump" must contain string "420: 00 05 20 2f 04 20 36 04  ee 20 d0 4c 22 04 60 ad"

    When I execute the procedure at localLabel for no more than 3 instructions

    When I hex dump memory between $0400 and $0500
    Then property "test.BDD6502.lastHexDump" must contain string "400: ee 00 07 d0 03 ee 01 07  78 a9 00 8d 00 04 a9 00"
    Then property "test.BDD6502.lastHexDump" must contain string "410: 8d 00 05 a9 00 8d 00 05  a9 00 8d 00 05 a9 00 8d"
    Then property "test.BDD6502.lastHexDump" must contain string "420: 00 05 20 2f 04 20 36 04  ee 20 d0 4c 22 04 60 ad"

  Examples:
    | build   |
    | Debug   |
    | Release |
