include "./lib/file.bb"

alloc[80000] lines

0"./AdventOfCode2022/day3.txt" read-file
lines get-lines

alloc[8] sum
0 sum store

using line-count:
    0
    while dup line-count < :
        dup 16 *     lines + load 
        over 16 * 8 + lines + load (ptr)
        using size ptr:
            size 2 /
            dup ptr + 
            using half-size second-half:
                half-size ptr get-items-bitwise 
                half-size second-half get-items-bitwise 
                &
                get-priority-sum 
                sum load + sum store
            ;
        ;
        1 +
    ;
    drop
;

"part1: " prints sum load print

# size ptr -> number
get-items-bitwise():
    alloc[8] items
    0 items store
    using size ptr:
        0 while dup size <:
            dup ptr + load-byte
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
            1 +
        ;
        drop
    ;
    items load
;

# number -> number
get-priority-sum():
    alloc[8] score   
    0 score store
    using items:
        0
        while dup 64 < :
            1 over << 
            items &
            0 > ? yes: dup score load + score store;
            1 +
        ;
        drop
    ;
    score load
;
