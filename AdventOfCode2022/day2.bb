include "../lib/file.bb"

0"./day2.txt" read-file

alloc[800000] lines
lines get-lines

alloc[8] sum
0 sum store

alloc[8] sum2
0 sum2 store

using line-count:
    0 while linenr line-count < :
        linenr 16 *     lines + load
        linenr 16 * 8 + lines + load (ptr)
        over over "A X" == ? yes: 4 sum load + sum store 3 sum2 load + sum2 store;
        over over "A Y" == ? yes: 8 sum load + sum store 4 sum2 load + sum2 store;
        over over "A Z" == ? yes: 3 sum load + sum store 8 sum2 load + sum2 store;
        over over "B X" == ? yes: 1 sum load + sum store 1 sum2 load + sum2 store;
        over over "B Y" == ? yes: 5 sum load + sum store 5 sum2 load + sum2 store;
        over over "B Z" == ? yes: 9 sum load + sum store 9 sum2 load + sum2 store;
        over over "C X" == ? yes: 7 sum load + sum store 2 sum2 load + sum2 store;
        over over "C Y" == ? yes: 2 sum load + sum store 6 sum2 load + sum2 store;
        over over "C Z" == ? yes: 6 sum load + sum store 7 sum2 load + sum2 store;
        drop drop 
        linenr 1 + 
    ;
    drop
    "part1: " prints sum load print
    "part2: " prints sum2 load print
;
