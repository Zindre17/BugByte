include "../lib/file.bb"

0"./day2.txt" read-file

str[80000] lines
lines get-lines

int sum
0 sum store

int sum2
0 sum2 store

using line-count:
    0 while linenr line-count < :
        linenr get-line
        over over "A X" == ? yes: 4 add-to-sum 3 add-to-sum2;
        over over "A Y" == ? yes: 8 add-to-sum 4 add-to-sum2;
        over over "A Z" == ? yes: 3 add-to-sum 8 add-to-sum2;
        over over "B X" == ? yes: 1 add-to-sum 1 add-to-sum2;
        over over "B Y" == ? yes: 5 add-to-sum 5 add-to-sum2;
        over over "B Z" == ? yes: 9 add-to-sum 9 add-to-sum2;
        over over "C X" == ? yes: 7 add-to-sum 2 add-to-sum2;
        over over "C Y" == ? yes: 2 add-to-sum 6 add-to-sum2;
                  "C Z" == ? yes: 6 add-to-sum 7 add-to-sum2;

        linenr 1 +
    ; drop

    "part1: " prints sum load print "\n" prints
    "part2: " prints sum2 load print "\n" prints
;

get-line(int) str: lines[] load ;

add-to-sum(int): sum load + sum store ;
add-to-sum2(int): sum2 load + sum2 store ;
