# fd -> 0 | error
aka close_id 3
close(int)int: close_id syscall1;

# size buffer fd -> size
aka read_id 0
read(int ptr int)int: read_id syscall3;

# mode flags path -> fd | error
aka open_id 2
open(int int ptr)int: open_id syscall3;

struct stat:
    st_dev 8
    st_ino 8
    st_nlink 8
    st_mode 4
    st_uid 4
    st_gid 4
    st_pad0 4
    st_rdev 8
    st_size 8
    st_blksize 8
    st_blocks 8
    st_atim.tv_sec 8
    st_atim.tv_nsec 8
    st_mtim.tv_sec 8
    st_mtim.tv_nsec 8
    st_ctim.tv_sec 8
    st_ctim.tv_nsec 8
    __unused 24
;

# statbuf path -> 0 | error
aka stat_id 4
stat(ptr ptr)int: stat_id syscall2;

# statbuf fd -> 0 | error
aka fstat_id 5
fstat(ptr int)int: fstat_id syscall2;

aka mmap_id 9
aka MAP_PRIVATE 2
aka PROT_READ 1
#  offset fd flags prot len addr  -> addr | error
mmap(int int int int int ptr)ptr: mmap_id syscall6 as ptr;

# path -> size ptr
read-file(ptr)int ptr:
    alloc[stat] statbuf
    
    0 swap 0 swap open
    dup 0 < ? yes: "Error opening: " prints dup print dup exit;
    
    using fd:
        statbuf fd fstat
        dup 0 < ? yes: "Error statting: " prints dup print dup exit;
        drop
        
        0 fd MAP_PRIVATE PROT_READ statbuf stat.st_size load 0 as ptr mmap
        dup as int 0 < ? yes: "Error mmaping: " prints dup print dup as int exit;
        
        fd close
        dup 0 < ? yes: "Error closing: " prints dup print dup exit;
        drop
        
        statbuf stat.st_size load swap 
    ;
;

# size ptr buffer -> count
get-lines(int ptr ptr) int:
    alloc[8] count
    alloc[8] prev
    0 count store
    0 prev store
    using size file lines:  
        0 
        while index size < :
            1 index file + "\n" == ? 
            yes: 
                index prev load - 
                count load 16 * lines + store
                prev load file +
                count load 16 * 8 + lines + store
                index 1 + prev store
                count load 1 + count store
            ; 
                
            index 1 +
        ;
        dup prev load -
        count load 16 * lines + store
        prev load file +
        count load 16 * 8 + lines + store
        count load 1 + count store
        drop
    ;
    count load
;
