take-args

include "../lib/file.bb"

using argv argc:
  argc 2 <?
    yes: "Please provide a filepath to compile\n" prints 1 exit;

  1 argv nth-arg print-zero-str
;

nth-arg(int n ptr argstart)0str:
  ptr current-arg
  argstart current-arg store

  0 while i n <:
    current-arg load
    find-next current-arg store
    i 1 +
  ;

  drop
  current-arg load

  find-next(ptr argx)ptr:
    0 while i argx + load-byte 0 !=:
      i 1 +
    ;
    argx 1 + +
  ;
;

print-zero-str(0str zstr):
  0 while i zstr + as ptr load-byte 0 !=:
    zstr i + as ptr load-byte printc
    i 1 +
  ;
  drop
  0"\n" load-byte printc
;
