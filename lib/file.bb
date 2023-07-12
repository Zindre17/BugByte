aka close-id 3
# fd
close(): close-id syscall1;
# size buffer fd
read(): 0 syscall3;
# path
open(): 0 swap 0 swap 2 syscall3;

0"./empty.bb" open
dup < 0? yes: #"open failed: " prints dup print;
"open failed: " prints dup print;

close
print
