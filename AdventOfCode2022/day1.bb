include "./lib/file.bb"
include "./lib/parsing.bb"

alloc[80000] lines

0"./AdventOfCode2022/day1.txt" read-file 
lines get-lines

alloc[8] max
alloc[8] current

alloc[8] two
alloc[8] three

0 max store
0 current store
0 two store
0 three store

using line-count:
    0 while dup line-count <:
        dup 16 * lines + load
        dup 0 =?
        yes:
            current load 
            dup max load >?
            yes: 
                two load three store
                max load two store
                dup max store
            ;
            no: 
                dup two load >?
                yes: 
                    two load three store
                    dup two store
                ;
                no:
                    dup three load >?
                    yes: 
                        dup three store
                    ;
                ;
            ;
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
    "max (part 1): " prints max load print
    "top 3 (part 2): " prints max load two load three load + + print
    drop 
;

