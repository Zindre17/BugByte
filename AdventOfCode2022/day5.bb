include "../lib/file.bb"
include "../lib/parsing.bb"
include "../lib/str.bb"

0"day5.txt" read-file

alloc[80000] lines
lines get-lines

alloc[80000] stacks
alloc[800] stacksizes
int stackcountmem

0 get-line find-stack-count
stackcountmem store

get-stack-count()int: stackcountmem load;

dup find-stack-number-line
using line-count stack-number-line:
    init-stacks
    "\n" prints
    
    stack-number-line 1 + while line-nr line-count < :
        # lines line-nr get-line prints "\n" prints
        line-nr get-line drop 0 = ?
        no: 
            line-nr parse-command execute-command
        ;
        line-nr bump
    ; drop
    
    show-tops
    "\n" prints
    
    # part 2
    init-stacks
    "\n" prints
    
    stack-number-line 1 + while line-nr line-count < :
        # lines line-nr get-line prints "\n" prints
        line-nr get-line drop 0 = ?
        no: 
            line-nr parse-command execute-command-bulk
        ;
        line-nr bump
    ; drop
    
    show-tops
    "\n" prints
    
    init-stacks():
        stack-number-line 1 - while line-nr 0 >= :
            0 while stack-nr get-stack-count < :
                line-nr get-line
                stack-nr get-crate-slot load-byte
                dup is-slot-empty ?
                no:
                    dup stack-nr push-crate
                ;
                drop 
                drop
                stack-nr bump
            ; drop
            
            line-nr 1 -
        ; drop
    ;
;

is-slot-empty(int) bool : 0" " load-byte = ;

# 3 characters per stack + 1 space between. Adding 1 to length to account for the last stack not having a space after it.
find-stack-count(int ptr) int : drop 1 + 4 / ;

find-stack-number-line(int line-count)int:
    0 1 -
    0 while line-nr line-count < :
        line-nr get-line swap drop
        is-crate-number-line ?
        yes: drop line-nr line-count ;
        no: line-nr 1 + ;
    ; drop
;

pop-stack(int)int:
    dup dup get-stack-size 1 - swap set-stack-size
    dup get-stack-size get-stack-item
;

execute-command-bulk(int count int source int target):
    0 while i count < :
        source pop-crate
        target target get-stack-size count + 1 - i - get-stack-index
        store
        i bump
    ; drop 
    
    target get-stack-size count + target set-stack-size
;

execute-command(int count int source int target):
    0 while i count <:
        source pop-crate 
        target push-crate
        i bump
    ; drop
;

parse-command(int)int int int: 
    get-line words drop
     
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
    0 while stack-nr get-stack-count < :
        stack-nr 
        stack-nr get-stack-size 1 -
        get-stack-item printc
        stack-nr bump
    ;
    drop
;

pop-crate(int stack-nr)int:
    stack-nr get-stack-size
    1 -
    using new-size:
        new-size stack-nr set-stack-size
        stack-nr new-size get-stack-item
    ;
;

push-crate(int crate int stack-nr):
    stack-nr get-stack-size
    using stack-size:
        crate stack-nr stack-size get-stack-index store
        stack-size bump stack-nr set-stack-size 
    ;
;

set-stack-size(int int): get-stack-size-index store ;

get-stack-size(int)int: get-stack-size-index load ;

get-stack-size-index(int)ptr: 8 * stacksizes + ;

get-stack-item(int int)int: get-stack-index load ;

get-stack-index(int int)ptr: get-stack-count * + 8 * stacks + ;

get-crate-slot(ptr int)ptr: 4 * 1 + + ;

is-crate-number-line(ptr)int: 1 + load-byte 0"1" load-byte = ;

bump(int)int: 1 + ;

get-line(int line-nr) int ptr:
    line-nr 16 *     lines + load
    line-nr 16 * 8 + lines + load as ptr
;
