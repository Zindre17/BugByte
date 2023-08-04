include "./lib/file.bb"

0"./AdventOfCode2022/day2.txt" read-file

alloc[800000] lines
lines get-lines

alloc[8] sum
0 sum store

using line-count:
    0
    while dup line-count < :
        dup 16 *      lines + load
        over 16 * 8 + lines + load (ptr)
        over over "A X" == ? yes: 4 sum load + sum store;
        over over "A Y" == ? yes: 8 sum load + sum store;
        over over "A Z" == ? yes: 3 sum load + sum store;
        over over "B X" == ? yes: 1 sum load + sum store;
        over over "B Y" == ? yes: 5 sum load + sum store;
        over over "B Z" == ? yes: 9 sum load + sum store;
        over over "C X" == ? yes: 7 sum load + sum store;
        over over "C Y" == ? yes: 2 sum load + sum store;
        over over "C Z" == ? yes: 6 sum load + sum store;
        drop drop 
        1 + 
    ;
    drop
    "part1: " prints sum load print
;
