include "../lib/file.bb"
include "../lib/parsing.bb"
include "../lib/str.bb"

0"day6.txt" read-file

alloc[8] cursor
alloc[32] window

# drop drop
using streamSize streamStart:
    # init window
    readNext 0 windowIndex store
    readNext 1 windowIndex store
    readNext 2 windowIndex store
    readNext 3 windowIndex store

    window while it hasDuplicates: 
        shiftLeftInWindow
        readNext
        append
        bumpCursor
        
        it
    ;
    drop 
    
    cursor load print
    
    readNext() int: cursor load streamStart + load-byte ;
;

bumpCursor(): cursor load 1 + cursor store ;

append(int): window 24 + store ;

shiftLeftInWindow(): 
    1 shiftItemLeft
    2 shiftItemLeft 
    3 shiftItemLeft
;

shiftItemLeft(int):
    dup windowItem
    swap 1 - windowIndex store
;

windowIndex(int) ptr: 8 * window + ;

windowItem(int) int: windowIndex load ;

hasDuplicates(ptr) bool:
    using windowStart:
        0 1 compare
        0 2 compare +
        0 3 compare +
        
        1 2 compare +
        1 3 compare +
        
        2 3 compare +
        
        0 >
        
        compare(int int) bool: 
            8 * windowStart + load-byte
            swap 
            8 * windowStart + load-byte
            =
        ;
    ;
;
