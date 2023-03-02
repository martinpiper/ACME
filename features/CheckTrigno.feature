Feature: Test trig assembly

  This executes ACME and tests trig  integration.

  Background:
    Given I have a simple overclocked 6502 system
    And I am using C64 processor port options
    Given add C64 hardware



  Scenario Outline: Execute assembly from general trig assembly test 

    And I run the command line: <build>\acme.exe -v9 --pdb target\trigono.pdb --labeldump target\trigono.lbl trigono.a
    Then property "test.BDD6502.lastProcessOutput" must contain string "First pass."
    Then property "test.BDD6502.lastProcessOutput" must contain string "Parsing source file 'trigono.a'"

    Given open file "target\trigono.pdb" for reading
    Then expect the next line to contain "INCLUDES:0"
    Then expect the next line to contain "FILES:1"
    Then expect the next line to contain "0:trigono.a"


    Given open file "target\trigono.lbl" for reading
    Then expect the next line to contain "PI  =3.141592653589792700000000000000"
    Then expect the next line to contain "x  =$400"


    And I load bin "trigono.o" at $c000

    # Tests expected assembly output in memory
    When I hex dump memory between $c000 and $c100
    Then property "test.BDD6502.lastHexDump" must contain string "c000: 63 6f 73 5b 30 2c 70 69  2f 32 5d 20 73 63 61 6c : cos[0,pi/2] scal"
    Then property "test.BDD6502.lastHexDump" must contain string "c010: 65 64 20 74 6f 20 30 2d  32 35 35 20 72 61 6e 67 : ed to 0-255 rang"
    Then property "test.BDD6502.lastHexDump" must contain string "c080: e1 e0 df de de dd dc db  da da d9 d8 d7 d6 d5 d5"


  Examples:
    | build   |
    | Debug   |
    | Release |
