include "../lib/file.bb"
include "../lib/parsing.bb"

0"./day4.txt" read-file

alloc[80000] lines

lines get-lines

alloc[8] sum
alloc[8] sum2
inc(ptr):
    dup load 1 + swap store
;

using line-count:
    0 while linenr line-count < :
        linenr lines parse-line 
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
        linenr 1 +
    ;
    drop
;

"part 1: " prints sum load print
"part 2: " prints sum2 load print

parse-line(int ptr) int int int int:
    using line-index lines:
        line-index 16 *     lines + load
        line-index 16 * 8 + lines + load as ptr
    ;
    using size pointer:
        size 0 = ? 
        yes: 1 2 3 4;
        no:
            size pointer "," index-of
            using index:
                index pointer parse-elf-range
                size 1 - index - pointer 1 + index + parse-elf-range
            ;
        ;
       
    ;
;

index-of(int ptr int ptr)int:
    load-byte swap drop
    using size pointer char:
        0 while i size < :
            i pointer + load-byte char = ?
            yes: i size + ;
            no: i 1 + ;
        ;
        size -
    ;
;

parse-elf-range(int ptr) int int:
    using size pointer:
        size pointer "-" index-of
        using index:
            index pointer parse-number
            size 1 - index - pointer 1 + index + parse-number
        ;
    ;
;
