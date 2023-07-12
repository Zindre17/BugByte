include "lib.bb"

alloc[1024] fileStr

0"./Examples/fizzbuzz.bb"  open 
dup < 0 ? yes: "Error opening: " prints dup print;

using fd :
    1024 fileStr fd read
    dup < 0 ? yes: "Error reading: " prints dup print; 
    drop

    fileStr while 1 over !== "\n":
        + 1
    ;
    - fileStr + 1 fileStr prints
    
    fd close
    dup < 0 ? yes: "Error closing: " prints dup print;
    drop
;
