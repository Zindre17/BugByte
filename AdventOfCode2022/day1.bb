include "../lib/file.bb"
include "../lib/parsing.bb"

str[8000] lines

0"./day1.txt" read-file 
lines get-lines

int max
int current

int two
int three

0 max store
0 current store
0 two store
0 three store

using line-count :
    0 while linenr line-count < :
        linenr is-line-empty?
        yes:
            current load
            dup max load > ? yes: set-new-max ;
            no:
                dup two load > ? yes: set-new-number-two ;
                no:
                    dup three load >? yes: set-new-number-three ;
                    no: drop ;
                ;
            ;

            0 current store
        ;
        no:
            linenr get-line
            parse-number
            add-to-current
        ;

        linenr 1 +
    ; drop

    "max (part 1): " prints max load print "\n" prints
    "top 3 (part 2): " prints max load two load + three load + print "\n" prints
;

set-new-max(int):
    max load set-new-number-two
    max store
;

set-new-number-two(int):
    two load set-new-number-three
    two store
;

set-new-number-three(int):
    three store
;

add-to-current(int) : current load + current store ;

get-line-size(int) int: lines[].length load ;
get-line(int) str: lines[] load ;

is-line-empty(int) bool: get-line-size 0 = ;
