next(int int) int int: 
   over over + 0 =?
   yes: drop 1;
   no: swap over +;
;

alt-next(int prev int current) int int: 
   prev current + 0 =?
   yes: current 1;
   no: current prev current +;
;
 
0 
0 while current 100 <:
   current print
   current alt-next
;
drop drop
