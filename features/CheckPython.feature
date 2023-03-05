Feature: Test python mixed with assembly

  This executes ACME and tests Python integration.

  Background:
    Given I have a simple overclocked 6502 system
    And I am using C64 processor port options
    Given add C64 hardware



  Scenario Outline: Execute assembly with python additions

    And I run the command line: <build>\acme.exe --pdb target\TestPython.pdb --labeldump target\TestPython.lbl TestPython.a

    Given open file "target\TestPython.pdb" for reading
    Then expect the next line to contain "INCLUDES:0"
    Then expect the next line to contain "FILES:2"
    Then expect the next line to contain "1:TestPython.py"
    Then expect the next line to contain "0:TestPython.a"
    Then expect the next line to contain "ADDRS:42"
    # Check the correct file and line number references are output for !scriptpythonfile and !scriptpythoninline commands
    Then expect the next line to contain "$400:1:0:14"
    Then expect the next line to contain "$401:1:1:4"
    Then expect the next line to contain "$402:1:1:4"
    Then expect the next line to contain "$403:1:1:4"
    Then expect the next line to contain "$404:1:1:4"
    Then expect the next line to contain "$405:1:1:4"
    Then expect the next line to contain "$406:1:1:5"
    Then expect the next line to contain "$407:1:1:5"
    Then expect the next line to contain "$408:1:1:5"
    Then expect the next line to contain "$409:1:1:6"
    Then expect the next line to contain "$40a:1:0:21"
    Then expect the next line to contain "$40b:1:0:21"
    Then expect the next line to contain "$40c:1:0:21"
    Then expect the next line to contain "$40d:1:0:21"
    Then expect the next line to contain "$40e:1:0:21"
    Then expect the next line to contain "$40f:1:1:4"


    And I load prg "TestPython.prg"
    And I load labels "target\TestPython.lbl"

    # Tests expected assembly output in memory
    When I hex dump memory between $0400 and $0500
    Then property "test.BDD6502.lastHexDump" must contain string "400: ea e6 02 ee 00 04 ee 20  d0 60 e6 03 ee 01 04 e6"
    Then property "test.BDD6502.lastHexDump" must contain string "410: 02 ee 00 04 ee 20 d0 60  e6 02 ee 00 04 ee 20 d0"
    Then property "test.BDD6502.lastHexDump" must contain string "420: 60 e6 02 ee 00 04 ee 20  d0 60 00 00 00 00 00 00"

    When I execute the procedure at start for no more than 10 instructions

    When I hex dump memory between $0400 and $0500
    Then property "test.BDD6502.lastHexDump" must contain string "400: eb e6 02 ee 00 04 ee 20  d0 60 e6 03 ee 01 04 e6"
    Then property "test.BDD6502.lastHexDump" must contain string "410: 02 ee 00 04 ee 20 d0 60  e6 02 ee 00 04 ee 20 d0"
    Then property "test.BDD6502.lastHexDump" must contain string "420: 60 e6 02 ee 00 04 ee 20  d0 60 00 00 00 00 00 00"

  Examples:
    | build   |
    | Debug   |
    | Release |
