include "../lib/file.bb"
include "../lib/parsing.bb"

0"./day4.txt" read-file

str[8000] lines

lines get-lines

int sum
int sum2

bump(ptr): dup load 1 + swap store ;

using line-count:
    0 while linenr line-count < :
        linenr get-line parse-line 
        using a b c d:
            is-exact-overlap ? yes: sum bump sum2 bump ;
            no:
                is-completely-overlapping ? yes: sum bump sum2 bump;
                no:
                    is-partially-overlapping ? yes: sum2 bump ;
                ;
            ;

            is-exact-overlap() bool:
                a c =
                b d =
                +
            ;

            is-completely-overlapping() bool:
                a c <
                b d >
                + 2 =

                c a <
                d b >
                + 2 =

                +
            ;

            is-partially-overlapping() bool:
                a c <=
                b c >=
                + 2 =

                c a <=
                d a >=
                + 2 =

                +
            ;
        ;

        linenr 1 +
    ; drop
;

"part 1: " prints sum load print "\n" prints
"part 2: " prints sum2 load print "\n" prints

get-line(int) str: lines[] load ;

parse-line(int size ptr pointer) int int int int:
    is-line-empty?
    yes: 1 2 3 4 ;
    no:
        size pointer 0"," index-of
        using index:
            index pointer parse-elf-range
            size 1 - index - pointer 1 + index + parse-elf-range
        ;
    ;

    is-line-empty() bool: size 0 = ;
;

index-of(str 0str)int:
    load-byte
    using size pointer char:
        0 while i size < :
            i get-character-at char = ?
            yes: i size + ;
            no: i 1 + ;
        ;
        size -
    ;

    get-character-at(int) int: pointer + load-byte ;
;

parse-elf-range(int size ptr pointer) int int:
    size pointer 0"-" index-of
    using index:
        index pointer parse-number
        size 1 - index - pointer 1 + index + parse-number
    ;
;
