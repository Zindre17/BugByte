include "token.bb"
include "../lib/str.bb"

# takes filename and content of file and returns a pointer to a token array and how many tokens it contains.
tokenize(str filename str content)ptr int:
  token[10000] tokens
  int token-count
  get-token-count()int: token-count load;
  set-token-count(int): token-count store;

  content
  using file-size file-start:
    ptr cursor
    get-cursor()ptr: cursor load;
    set-cursor(ptr): cursor store;

    int remaining-size
    get-remaining-size()int: remaining-size load;
    set-remaining-size(int): remaining-size store;

    int linenr
    get-linenr()int: linenr load;
    set-linenr(int): linenr store;
    increment-linenr(): get-linenr 1 + set-linenr;

    int column
    get-column()int: column load;
    set-column(int): column store;
    increment-column(int): get-column + set-column;

    int remaining-line-length
    get-remaining-line-length()int: remaining-line-length load;
    set-remaining-line-length(int): remaining-line-length store;


    file-start set-cursor
    file-size set-remaining-size
    1 set-linenr

    get-remaining-text find-next-newline
    while next-newline-pos 0 >=:
      1 set-column
      next-newline-pos set-remaining-line-length
      get-remaining-line-length 0 =?
        yes:bump-cursor;
        no:
          get-remaining-line-length while _it 0 >:
            get-remaining-line find-next-word-start move-cursor

            get-cursor is-string-literal?
              yes:
                get-remaining-line-length 1 -
                get-cursor 1 +
                find-closing-quote
                dup 0 1 - =?yes: "no closing double quote found" println get-remaining-line println 1 exit;
                2 +
              ;
            no:
            get-cursor is-zero-string-literal?
              yes:
                get-remaining-line-length 2 -
                get-cursor 2 +
                find-closing-quote
                dup 0 1 - =?yes: "No closing double quote found (0str)" println 1 exit;
                3 +
              ;
            no:
            get-cursor is-line-comment?
              yes: get-remaining-line-length;
            no:
            specialcharacters get-cursor is-any-of?
              yes: 1;
            no:
            get-remaining-line find-length-of-next-word
            ;;;;

            using word-length:
              filename
              word-length get-cursor
              get-linenr
              get-column

              word-length move-cursor
            ;
            get-token-count tokens[] store
            get-token-count 1 + set-token-count

            get-remaining-line-length
          ;drop
          get-cursor is-newline?
            yes: bump-cursor;
        ;

      increment-linenr

      get-remaining-text find-next-newline

      get-remaining-line()str: get-remaining-line-length get-cursor;
    ;drop

    get-remaining-text()str:
      get-remaining-size
      get-cursor
    ;

    bump-cursor(): 1 move-cursor;

    move-cursor(int distance):
      get-cursor distance + set-cursor
      get-remaining-size distance - set-remaining-size
      get-column distance + set-column
      get-remaining-line-length distance - set-remaining-line-length
    ;
  ;
  tokens get-token-count

  find-next-word-start(str text)int:
    0 while i get-char-at is-space-char: i 1 +;

    get-char-at(int)int: text.start + load-byte;
  ;

  find-closing-quote(str text)int:
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

  is-line-comment(ptr)bool: load-byte 0"#" load-byte =;

  is-new-line(ptr)bool: load-byte 0"\n" load-byte =;

  println(str): prints "\n" prints;

  aka specialcharacters ":;?[]()"
  aka whitespace " \n"

  find-length-of-next-word(str text)int:
    text.length
    0 while i text.length <:
      specialcharacters text.start i + is-any-of
      whitespace text.start i + is-any-of | ?
        yes: drop i text.length;
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
;
