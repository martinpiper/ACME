import acme
# Output some assembly to ... assemble :)
print("Python is assembling...")
acme.source("	inc .myzp\n	inc $0400")
acme.source("	inc $d020")
acme.source("	rts")
