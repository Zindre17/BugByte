100 repeat i : 
    i 15 % 0 =? 
    yes: "fizzbuzz\n" prints;
    no: 
        i 5 % 0 =? 
        yes: "buzz\n" prints; 
        no: 
            i 3 % 0 =? 
            yes: "fizz\n" prints; 
            no: i print;
        ;
    ;
;
