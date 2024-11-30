include "../lib/syscalls.bb"

# path -> size ptr
read-file(0str path) str :
    stat-data statbuf
    
    0 0 path open
    dup 0 < ? yes: "Error opening: " prints dup print dup exit;
    
    using fd:
        statbuf fd fstat
        dup 0 < ? yes: "Error statting: " prints dup print dup exit;
        drop
        
        0 fd MAP_PRIVATE PROT_READ statbuf.st_size load 0 as ptr mmap
        dup as int 0 < ? yes: "Error mmaping: " prints dup print dup as int exit;
        
        fd close
        dup 0 < ? yes: "Error closing: " prints dup print dup exit;
        drop
        
        statbuf.st_size load swap 
    ;
;

# size ptr buffer -> count
get-lines(int size ptr file ptr lines) int:
    int count
    int prev
    0 count store
    0 prev store
    
    0 while index size < :
        index file + is-newline ?
        yes: 
            index get-length-since-last-newline
            get-start-position-of-next-line
            store-line

            index 1 + prev store
            bump-count
        ; 

        index 1 +
    ;

    get-length-since-last-newline
    get-start-position-of-next-line
    store-line

    bump-count

    count load

    is-newline(ptr) bool: load-byte 0"\n" load-byte = ;
    
    store-line(str):
        count load 16 * 8 + lines + store
        count load 16 * lines + store
    ;
    
    bump-count(): count load 1 + count store ;
    get-start-position-of-next-line() ptr: prev load file + ;
    get-length-since-last-newline(int) int : prev load - ;
;
