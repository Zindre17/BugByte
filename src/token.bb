
struct token:
  file-path-length int
  file-path-start ptr
  value-length int
  value-start ptr
  line int
  column int
;

size-of-token()int: 48;

load-token(ptr p)token:
  p load
  p 8 + load
  p 16 + load
  p 24 + load
  p 32 + load
  p 40 + load
;

print-token(token t):
  t.file-path-length t.file-path-start prints
  ":" prints
  t.line print
  ":" prints
  t.column print
  "|" prints
  t.value-length t.value-start println
;
