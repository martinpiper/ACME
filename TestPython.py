# Output some assembly to ... assemble :)
print("Python is assembling...")
print(acmeParameters)
acme.source("	inc .myzp\n	inc $0400")
acme.source("	inc $d020")
acme.source("	rts")
