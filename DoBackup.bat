cd ..
del TTTTACME.zip
rmdir /S /Q TTTTACME
mkdir TTTTACME
xcopy ACME\*.* TTTTACME\ACME\ /S /E
cd TTTTACME
cd ACME
call CleanProjectFullyWithAttrib.bat
