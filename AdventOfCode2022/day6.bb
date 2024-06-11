include "../lib/file.bb"
include "../lib/parsing.bb"
include "../lib/str.bb"

0"day6.txt" read-file

aka longSize 14
aka shortSize 4

int cursor
int[4] window
int[14] longWindow

# drop drop
using streamSize streamStart:
    # init window
    shortSize repeat i : 
        readNext i window[] store
    ;

    window while it shortSize hasDuplicates: 
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
    
    longSize repeat i :
        readNext i longWindow[] store
    ;
    
    longWindow while it longSize hasDuplicates: 
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

insert(int int ptr): swap 8 * + store ;
append(int): shortSize 1 - window insert ;
appendLong(int): longSize 1 - longWindow insert ;

shiftWindowLeft(): window shortSize shiftLeft ;

shiftLongWindowLeft(): longWindow longSize shiftLeft ;

shiftLeft(ptr buffer int size): 
    1 while index size < :
        buffer index shiftItemLeft
        index 1 +
    ;
    drop
;

shiftItemLeft(ptr int):
    8 * + 
    dup
    load
    swap 8 - store
;

hasDuplicates(ptr int) bool: 
    int sum
    0 sum store
    
    1 -
    using windowStart lastIndex:
        0 while i lastIndex <: 
            i 1 + while j lastIndex <=:
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
