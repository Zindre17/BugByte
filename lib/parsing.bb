# size ptr -> int
parse-number():
    alloc[8] number
    0 number store
    using size ptr :
        1 
        while dup size <=:
        using index:
            1
            index 1 -
            while dup 0 >:
                1 -
                swap 10 * swap
            ;
            drop
            ptr size + index - (ptr) load-byte 48 -
            dup 0 >=?
            yes: dup 10 <?
                yes:
                    over *
                    dup number load + number store
                ;
            ;
            drop
            drop
            index 1 +
        ;
        ;
        drop 
    ;
    number load
;