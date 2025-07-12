# string -> ptr to words, # of words
words(str) ptr int:
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
    store-word(str): counter load words-array[] store ;
;

trim-start(int size ptr location) str:
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

    trim-start-by(int amount) str:
        size amount -
        location amount +
    ;
;

is-space(ptr) int: load-byte 0" " load-byte = ;
is-space-char(int) int: 0" " load-byte = ;

is-newline(ptr)int: load-byte 0"\n" load-byte =;
find-next-newline(str)int: 0"\n" index-of;

index-of(str text 0str char)int:
  0 1 -
  char load-byte
  0 while i text.length <:
    dup text.start i + load-byte =?
      yes: swap drop i swap text.length i +;
      no: i 1 +;
  ;drop drop
;

0str-to-str(0str start)str:
  0 while i start + load-byte 0 !=:i 1 +;
  start
;

