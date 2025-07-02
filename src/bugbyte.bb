take-args

include "../lib/file.bb"

using argv argc:
  argc 2 <?
    yes: "Please provide a filepath to compile\n" prints 1 exit;

  1 argv nth-arg read-file
;

using file-size file-start:
  ptr cursor
  int remaining-size
  file-start cursor store
  file-size remaining-size store

  remaining-size load
  while current 0 >:
    cursor load is-string-literal?
      yes:
        "found a string literal: " prints
        cursor load # checkpoint
        bump-cursor
        remaining-text complete-string-literal
        dup 0 1 - =?yes: "no closing double quote found" println 1 exit;
        move-cursor
        bump-cursor
        dup cursor load swap - swap println
      ;
    no:
    cursor load is-zero-string-literal?
      yes:
        "found a zero-string literal: " prints
        cursor load # checkpoint
        2 move-cursor
        remaining-text complete-string-literal
        dup 0 1 - =?yes: "No closing double quote found (0str)" println 1 exit;
        move-cursor
        bump-cursor
        dup cursor load swap - swap println
      ;
    no:
    remaining-text next-word
    using wlength wstart:
      wlength 0 =?
        yes: bump-cursor;
        no:
          wlength wstart println
          wlength move-cursor
        ;
    ;
    ;;
    remaining-size load
  ;drop

  remaining-text()int ptr:
    remaining-size load
    cursor load
  ;

  bump-cursor(): 1 move-cursor;

  move-cursor(int distance):
    cursor load distance + cursor store
    remaining-size load distance - remaining-size store
  ;
;

complete-string-literal(str text)int:
  int double-quote
  ascii-double-quote double-quote store
  text double-quote index-of
;

aka ascii-double-quote 34
is-string-literal(ptr)bool:
  load-byte ascii-double-quote =
;

is-zero-string-literal(ptr)bool:
  dup
  load-byte 0"0" load-byte =
  swap 1 + is-string-literal
  &
;

next-line(str)int: 0"\n" index-of;
println(str): prints "\n" prints;

aka separators "\n :;?[]()"
next-word(str text)int ptr:
  0 while i text.length <:
    separators text.start i + is-any-of?
      yes: i text.length +;
      no: i 1 +;
  ;
  text.length -
  text.start
;

index-of(str text 0str char)int:
  0 1 -
  0 while i text.length <:
    text.start i + load-byte char load-byte =?
      yes: drop i text.length i +;
      no: i 1 +;
  ;drop
;

is-any-of(str chars ptr text):
  text load-byte
  using char:
    no
    0 while i chars.length <:
      char chars.start i + load-byte =?
        yes: drop yes i chars.length +;
        no: i 1 +;
    ; drop
  ;
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
