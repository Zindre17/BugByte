include "../lib/file.bb"

alloc[80000] lines

0"./day3.txt" read-file
lines get-lines

int sum
0 sum store

int sum2
0 sum2 store

using line-count:
    0 while linenr line-count < :
        linenr get-line
        split-in-halfs

        get-items-bitwise
        
        using first-half :
            get-items-bitwise
            first-half
            &
            get-priority-sum
            add-to-sum
        ;
        linenr 1 +
    ; drop
    
    0 while linenr line-count < :
        linenr 0 + get-line get-items-bitwise
        linenr 1 + get-line get-items-bitwise
        &
        linenr 2 + get-line get-items-bitwise
        &
        
        get-priority-sum 
        add-to-sum2
        
        linenr 3 +
    ; drop
;

"part1: " prints sum load print
"part2: " prints sum2 load print

add-to-sum(int): sum load + sum store ;
add-to-sum2(int): sum2 load + sum2 store ;

# size ptr -> number
get-items-bitwise(int size ptr pointer) int:
    int items
    0 items store
    
    0 while i size <:
        i pointer + load-byte
        dup is-upper-case ? yes: 
            # Uppercase priority A - Z (27 - 52)
            64 - 26 +
        ;
        no:
            # Lowercase priority a - z (1 - 26)
            96 - 
        ;
        
        1 swap << 
        items load 
        |
        items store
        
        i 1 +
    ; drop
    
    items load
;

is-upper-case(int) bool: 97 < ;

# number -> number
get-priority-sum(int items) int:
    int score   
    0 score store
    
    0 while i 64 < :
        1 i << 
        items &
        0 > ? yes: i add-to-score;
        i 1 +
    ; drop
    
    score load
    
    add-to-score(int): score load + score store ;
;

get-line(int line-nr) int ptr:
    line-nr 16 * lines + load
    line-nr 16 * 8 + lines + load as ptr
;

split-in-halfs(int size ptr location) int ptr int ptr:
    size 2 /
    dup 
    dup location + swap 
    location 
;
