Feature: Test label defined detection and error

  This executes ACME and tests redefining a previously defined label is decteddetected in a macro and correctly reported

  Scenario Outline: Execute assembly for label defined test

    Given I run the command line ignoring return code: <build>\acme.exe -v9 --pdb target\TestLabelDefined.pdb --labeldump target\TestLabelDefined.lbl TestLabelDefined.a
    Then property "test.BDD6502.lastProcessOutput" must contain string "(Macro MTest): Label already defined. : pos1 : start : p1 : p1"
    Then property "test.BDD6502.lastProcessOutput" must contain string "TestLabelDefined.a(15) : Previous context hint"
    

  Examples:
    | build   |
    | Debug   |
    | Release |
