# string -> ptr to words, # of words
words(int ptr) ptr int:
    int counter
    0 counter store
    int lastword
    0 lastword store
    str[100] words-array
    
    trim-start
    using size location:
        0 while i size < : 
            location i + is-space?
            yes: 
                i get-length-since-last-space
                get-start-position-of-next-word
                store-word
                
                i 1 + lastword store
                counter bump
            ;
            i 1 +
        ; drop
        
        size lastword load - 
        location lastword load + 
        store-word
        counter bump
    ;
    words-array
    counter load
    
    bump(ptr): dup load 1 + swap store ;
    get-length-since-last-space(int) int: lastword load - ;
    get-start-position-of-next-word() ptr: location lastword load + ;
    get-next-size-position() ptr: counter load 16 * words-array + ;
    get-next-word-start-position() ptr: counter load 16 * 8 + words-array + ;
    store-word(int ptr):
        get-next-word-start-position store
        get-next-size-position store
    ;
;

trim-start(int size ptr location) int ptr:
    index-of-first-non-space 
    trim-start-by

    index-of-first-non-space() int:
        0 while i size < : 
            location i + is-space?
            yes: i 1 + ;
            no: i size + ;
        ;
        size - 
    ;
    
    trim-start-by(int amount) int ptr:
        size amount -
        location amount +
    ;
;

is-space(ptr) int: load-byte 0" " load-byte = ;

