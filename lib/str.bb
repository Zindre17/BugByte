# string -> ptr to words, # of words
words(int ptr) ptr int:
    alloc[8] counter
    0 counter store
    alloc[8] lastword
    0 lastword store
    alloc[1600] words-array
    
    trim-start
    using size location:
        0 while i size < : 
            location i + is-space?
            yes: 
                i lastword load - size-index store
                location lastword load + word-start-index store
                i 1 + lastword store
                counter bump
            ;
            i 1 +
        ;
        drop # i
        size lastword load - size-index store
        location lastword load + word-start-index store
        counter bump
    ;
    words-array
    counter load
    
    bump(ptr): dup load 1 + swap store ;
    size-index() ptr: counter load 2 * 8 * words-array + ;
    word-start-index() ptr: counter load 2 * 8 * 8 + words-array + ;
;

trim-start(int ptr) int ptr:
    using size location:
        index-of-space dup  
        size swap - 
        swap location +

        index-of-space() int:
            0 while i size < : 
                location i + is-space?
                yes: i 1 + ;
                no: i size + ;
            ;
            size - 
        ;
    ;
;

is-space(ptr) int: load-byte 0" " load-byte = ;

