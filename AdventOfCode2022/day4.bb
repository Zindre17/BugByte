include "./lib/file.bb"
include "./lib/parsing.bb"

0"./AdventOfCode2022/day4.txt" read-file

alloc[80000] lines

lines get-lines

alloc[8] sum
alloc[8] sum2
inc():
    dup load 1 + swap store
;

using line-count:
    0
    while dup line-count < :
        dup lines parse-line 
        using a b c d:
            a c = 
            b d =
            + ?
            yes: 
                sum inc
                sum2 inc
            ;
            no:
                a c < 
                b d >
                + 2 = ? yes: sum inc ;
                
                c a <
                d b >
                + 2 = ? yes: sum inc ;
                
                a c <=
                b c >=
                + 2 = ? yes: sum2 inc ;
            
                c a <=
                d a >=
                + 2 = ? yes: sum2 inc ;
            ;
        ;
        1 +
    ;
    drop
;

"part 1: " prints sum load print
"part 2: " prints sum2 load print

parse-line():
    using line-index lines:
        line-index 16 *     lines + load
        line-index 16 * 8 + lines + load (ptr)
    ;
    using size ptr:
        size 0 = ? 
        yes: 1 2 3 4;
        no:
            size ptr "," index-of
            using index:
                index ptr parse-elf-range
                size 1 - index - ptr 1 + index + parse-elf-range
            ;
        ;
       
    ;
;

index-of():
    load-byte swap drop
    using size ptr char:
        0 
        while dup size < :
            dup ptr + load-byte char = ?
            yes: size + ;
            no: 1 + ;
        ;
        size -
    ;
;

parse-elf-range():
    using size ptr:
        size ptr "-" index-of
        using index:
            index ptr parse-number
            size 1 - index - ptr 1 + index + parse-number
        ;
    ;
;
