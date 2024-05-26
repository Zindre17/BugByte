include "../lib/file.bb"
include "../lib/parsing.bb"
include "../lib/str.bb"

0"day6.txt" read-file

alloc[8] cursor
alloc[32] window
alloc[112] longWindow
aka longSize 14
aka shortSize 4

# drop drop
using streamSize streamStart:
    # init window
    readNext 0 windowIndex store
    readNext 1 windowIndex store
    readNext 2 windowIndex store
    readNext 3 windowIndex store

    window while it hasDuplicates: 
        shiftWindowLeft
        readNext
        append
        bumpCursor
        
        it
    ;
    drop 
    
    cursor load print
    
    # Part 2
    0 cursor store
    
    readNext 0  longWindowIndex store
    readNext 1  longWindowIndex store
    readNext 2  longWindowIndex store
    readNext 3  longWindowIndex store
    readNext 4  longWindowIndex store
    readNext 5  longWindowIndex store
    readNext 6  longWindowIndex store
    readNext 7  longWindowIndex store
    readNext 8  longWindowIndex store
    readNext 9  longWindowIndex store
    readNext 10 longWindowIndex store
    readNext 11 longWindowIndex store
    readNext 12 longWindowIndex store
    readNext 13 longWindowIndex store
    
    longWindow while it hasLongDuplicates: 
        shiftLongWindowLeft
        readNext
        appendLong
        bumpCursor
        
        it
    ;
    drop
    
    cursor load print
    
    readNext() int: cursor load streamStart + load-byte ;
;

bumpCursor(): cursor load 1 + cursor store ;

append(int): 3 windowIndex store ;
appendLong(int): 13 longWindowIndex store ;

shiftWindowLeft(): window shortSize shiftLeft ;

shiftLongWindowLeft(): longWindow longSize shiftLeft ;

shiftLeft(ptr int): 
    using buffer size:
        1 while index size < :
            buffer index shiftItemLeft
            index 1 +
        ;
        drop
    ;
;

shiftItemLeft(ptr int):
    8 * + 
    dup
    load
    swap 8 - store
;

windowIndex(int) ptr: 8 * window + ;

windowItem(int) int: windowIndex load ;

longWindowIndex(int) ptr: 8 * longWindow + ;

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

hasLongDuplicates(ptr) bool: 
    alloc[8] sum
    0 sum store
    
    using windowStart:
        0 while i 13 <: 
            i 1 + while j 13 <=:
                i j compare
                sum load + sum store
                
                j 1 +
            ;
            drop
            
            i 1 +
        ;
        drop
        
        compare(int int) bool:
            8 * windowStart + load-byte
            swap 
            8 * windowStart + load-byte
            =
        ;
    ;
    
    sum load 0 > 
;
