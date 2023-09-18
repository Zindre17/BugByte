include "../lib/file.bb"
include "../lib/parsing.bb"
include "../lib/str.bb"

0"day5.txt" read-file

alloc[80000] lines
lines get-lines

alloc[80000] stacks
alloc[800] stacksizes
alloc[8] stackcountmem
lines 0 get-line
drop 
1 + 4 /
stackcountmem store
stack-count()int: stackcountmem load;

using line-count:
    "stack count: " prints stack-count print
    0 while line-nr line-count < :
        lines line-nr get-line
        using size line-start:
            line-start is-crate-number-line ?
            no:
                line-nr 1 +        
            ;
            yes: 
                line-nr 1 -
                while line-nr 0 >= :
                    0
                    while stack-nr stack-count < :
                        lines line-nr get-line
                        stack-nr get-crate-slot
                        load-byte dup 0" " load-byte = ?
                        no:
                            dup stack-nr push-crate
                        ;
                        drop 
                        drop
                        stack-nr bump
                    ;
                    drop
                    line-nr 1 -
                ;
                drop
                
                show-tops
                
                "\n" prints
                line-nr line-count + 2 +
            ;
        ;
    ;
    line-count -
    while line-nr line-count < :
        # lines line-nr get-line prints "\n" prints
        lines line-nr get-line drop 0 = ?
        no: 
            line-nr parse-command execute-command
        ;
        line-nr bump
    ;
    drop
    show-tops
    "\n" prints
;

pop-stack(int)int:
    dup dup get-stack-size 1 - swap set-stack-size
    dup get-stack-size get-stack-item
;

execute-command(int int int):
    using count source target:
        0
        while i count <:
            source pop-crate 
            dup "moving " prints printc " to " prints 
            target
            dup print
            push-crate
            i bump
        ;
        drop
    ;
;

parse-command(int)int int int: 
    lines swap get-line
    over over prints "\n" prints
    words
    drop 
    using words-array:
        1 word-at-index parse-number
        3 word-at-index parse-number 1 -
        5 word-at-index parse-number 1 -
    ;
    
    size-index(int) ptr: 16 * words-array + ;
    start-index(int) ptr: 16 * 8 + words-array + ;
    word-at-index(int) int ptr: dup size-index load swap start-index load as ptr;
;

show-tops():
    "\n" prints
    0 
    while stack-nr stack-count < :
        stack-nr 
        stack-nr get-stack-size 1 -
        get-stack-item printc
        stack-nr bump
    ;
    drop
;

pop-crate(int)int:
    dup get-stack-size 1 - over set-stack-size
    dup get-stack-size get-stack-item
;

push-crate(int int):
    over over
    dup get-stack-size get-stack-index store
    dup get-stack-size bump swap set-stack-size drop
;

set-stack-size(int int): get-stack-size-index store;

get-stack-size(int)int: get-stack-size-index load;

get-stack-size-index(int)ptr: 8 * stacksizes +;

get-stack-item(int int)int: get-stack-index load;

get-stack-index(int int)ptr: stack-count * + 8 * stacks +;

get-crate-slot(ptr int)ptr: 4 * 1 + +;

is-crate-number-line(ptr)int:
    1 + load-byte 0"1" load-byte = 
;

bump(int)int: 1 + ;

get-line(ptr int) int ptr:
    using buffer index:
        index 16 *     buffer + load
        index 16 * 8 + buffer + load as ptr
    ;
;
