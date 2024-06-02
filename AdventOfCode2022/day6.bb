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
    readNext 0 window bufferPosition store
    readNext 1 window bufferPosition store
    readNext 2 window bufferPosition store
    readNext 3 window bufferPosition store

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
    
    readNext 0  longWindow bufferPosition store
    readNext 1  longWindow bufferPosition store
    readNext 2  longWindow bufferPosition store
    readNext 3  longWindow bufferPosition store
    readNext 4  longWindow bufferPosition store
    readNext 5  longWindow bufferPosition store
    readNext 6  longWindow bufferPosition store
    readNext 7  longWindow bufferPosition store
    readNext 8  longWindow bufferPosition store
    readNext 9  longWindow bufferPosition store
    readNext 10 longWindow bufferPosition store
    readNext 11 longWindow bufferPosition store
    readNext 12 longWindow bufferPosition store
    readNext 13 longWindow bufferPosition store
    
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

bufferPosition(int ptr) ptr: swap 8 * + ;

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
