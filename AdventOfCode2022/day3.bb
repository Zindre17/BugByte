include "./lib/file.bb"

alloc[80000] lines

0"./AdventOfCode2022/day3.txt" read-file
lines get-lines

alloc[8] sum
0 sum store

alloc[8] sum2
0 sum2 store

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
    
    0
    while dup line-count < :
        using index:
            index 0 + 16 *     lines + load
            index 0 + 16 * 8 + lines + load (ptr)
            index 1 + 16 *     lines + load
            index 1 + 16 * 8 + lines + load (ptr)
            index 2 + 16 *     lines + load
            index 2 + 16 * 8 + lines + load (ptr)
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
    ;
    drop
;

"part1: " prints sum load print
"part2: " prints sum2 load print

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
