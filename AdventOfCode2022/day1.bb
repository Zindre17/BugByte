include "./lib/file.bb"
include "./lib/parsing.bb"

alloc[80000] lines

0"./AdventOfCode2022/day1.txt" read-file 
"size of file: " prints over print
lines get-lines

"lines in file: " prints dup print

alloc[8] max
alloc[8] current

0 max store
0 current store

using line-count:
    0 while dup line-count <:
        dup 16 * lines + load
        dup 0 =?
        yes:
            current load 
            dup max load >?
            yes: dup max store;
            drop 0 current store
        ;
        no:
            over 16 * lines + 8 + load
            over swap parse-number
            current load + current store
        ;
        drop 
        1 +
    ; 
    "max: " prints max load print
    drop 
;

