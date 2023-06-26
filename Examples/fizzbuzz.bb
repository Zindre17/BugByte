1 while dup < 100 : 
    dup % 15 = 0 ? 
    yes: "fizzbuzz\n" prints ;
    no: 
        dup % 5 = 0 ? 
        yes: "buzz\n" prints ; 
        no: 
            dup % 3  = 0 ? 
            yes: "fizz\n" prints ; 
            no: dup print ;
        ;
    ;
    + 1
;
drop
