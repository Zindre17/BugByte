take-args

include "../lib/file.bb"
include "../lib/str.bb"
include "lexer.bb"

str current-file
get-current-file()str: current-file load;
set-current-file(str): current-file store;

using argv argc:
  argc 2 <?
    yes: "Please provide a filepath to compile\n" prints 1 exit;

  1 argv nth-arg
;

dup 0str-to-str set-current-file
get-current-file println

read-file
using size start:
  get-current-file size start tokenize
  over over
  "Tokens found: " prints print "." println "Starting at: " prints print "." println
;

using start count:
  count repeat i:
    i size-of-token * start + load-token print-token
  ;
;

println(str): prints "\n" prints;

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

