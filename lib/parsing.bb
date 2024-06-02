parse-number(int size ptr pointer) int:
    int number
    0 number store
    
    0 while index size <:
        index get-nth-character-from-end char-to-number
        
        dup is-between-0-and-9?
        yes: index get-base * increment-number ;
        no: drop ;
        
        index 1 +
        
        get-nth-character-from-end(int) int: end-of-string swap - load-byte ;
    ; drop 

    number load

    end-of-string() ptr: pointer size + 1 - ;
    
    increment-number(int): number load + number store ;
    
    get-base(int digit) int:
        1 
        digit while j 0 >:
            10 *
            j 1 -
        ; drop
    ;
    
    is-between-0-and-9(int) bool : dup 0 >= swap 10 < + ;
;

char-to-number(int) int: 48 - ;
