include "../lib/file.bb"

alloc[80000] lines

0"./day3.txt" read-file
lines get-lines

alloc[8] sum
0 sum store

alloc[8] sum2
0 sum2 store

using line-count:
    0 while linenr line-count < :
        linenr 16 *     lines + load 
        linenr 16 * 8 + lines + load as ptr
        using size pointer:
            size 2 /
            dup pointer + 
            using half-size second-half:
                half-size pointer get-items-bitwise 
                half-size second-half get-items-bitwise 
                &
                get-priority-sum 
                sum load + sum store
            ;
        ;
        linenr 1 +
    ;
    drop
    
    0 while index line-count < :
        index 0 + 16 *     lines + load
        index 0 + 16 * 8 + lines + load as ptr
        index 1 + 16 *     lines + load
        index 1 + 16 * 8 + lines + load as ptr
        index 2 + 16 *     lines + load
        index 2 + 16 * 8 + lines + load as ptr
        using size1 ptr1 size2 ptr2 size3 ptr3:
            size1 ptr1 get-items-bitwise 
            size2 ptr2 get-items-bitwise 
            size3 ptr3 get-items-bitwise 
            &
            &
            get-priority-sum 
            sum2 load + sum2 store
        ;
        index 3 +
    ;
    drop
;

"part1: " prints sum load print
"part2: " prints sum2 load print

# size ptr -> number
get-items-bitwise(int ptr) int:
    alloc[8] items
    0 items store
    using size pointer:
        0 while i size <:
            i pointer + load-byte
            dup 97 < ? yes: 
                # Uppercase (27 - 52)
                64 - 26 +
            ;
            no:
                # Lowercase (1 - 26)
                96 - 
            ;
            1 swap << 
            items load |
            items store
            i 1 +
        ;
        drop
    ;
    items load
;

# number -> number
get-priority-sum(int) int:
    alloc[8] score   
    0 score store
    using items:
        0 while i 64 < :
            1 i << 
            items &
            0 > ? yes: i score load + score store;
            i 1 +
        ;
        drop
    ;
    score load
;
