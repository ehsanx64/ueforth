: (   41 parse drop drop ; immediate

( Useful Basic Compound Words )
: 2drop ( n n -- ) drop drop ;
: 2dup ( a b -- a b a b ) over over ;
: nip ( a b -- b ) swap drop ;
: rdrop ( r: n n -- ) r> r> drop >r ;
: */ ( n n n -- n ) */mod nip ;
: * ( n n -- n ) 1 */ ;
: /mod ( n n -- n n ) 1 swap */mod ;
: / ( n n -- n ) /mod nip ;
: mod ( n n -- n ) /mod drop ;
: invert ( n -- ~n ) -1 xor ;
: negate ( n -- -n ) invert 1 + ;
: - ( n n -- n ) negate + ;
: rot ( a b c -- c a b ) >r swap r> swap ;
: -rot ( a b c -- b c a ) swap >r swap r> ;
: < ( a b -- a<b ) - 0< ;
: > ( a b -- a>b ) swap - 0< ;
: = ( a b -- a!=b ) - 0= ;
: <> ( a b -- a!=b ) = 0= ;
: bl 32 ;   : nl 10 ;
: 1+ 1 + ;   : 1- 1 - ;
: 2* 2 * ;   : 2/ 2 / ;
: 4* 4 * ;   : 4/ 4 / ;
: +! ( n a -- ) swap over @ + swap ! ;

( Cells )
: cell+ ( n -- n ) cell + ;
: cells ( n -- n ) cell * ;
: cell/ ( n -- n ) cell / ;

( System Variables )
: 'tib ( -- a ) 'sys 0 cells + ;
: #tib ( -- a ) 'sys 1 cells + ;
: >in ( -- a ) 'sys 2 cells + ;
: state ( -- a ) 'sys 3 cells + ;
: base ( -- a ) 'sys 4 cells + ;
: 'heap ( -- a ) 'sys 5 cells + ;
: current ( -- a ) 'sys 6 cells + ;
: 'context ( -- a ) 'sys 7 cells + ;  : context 'context @ ;
: 'notfound ( -- a ) 'sys 8 cells + ;

( Dictionary )
: here ( -- a ) 'heap @ ;
: allot ( n -- ) 'heap +! ;
: aligned ( a -- a ) cell 1 - dup >r + r> invert and ;
: align   here aligned here - allot ;
: , ( n --  ) here ! cell allot ;
: c, ( ch -- ) here c! 1 allot ;

( Compilation State )
: [ 0 state ! ; immediate
: ] -1 state ! ; immediate

( Quoting Words )
: ' bl parse 2dup find dup >r -rot r> 0= 'notfound @ execute 2drop ;
: ['] ' aliteral ; immediate
: char bl parse drop c@ ;
: [char] char aliteral ; immediate
: literal aliteral ; immediate

( Core Control Flow )
: begin   here ; immediate
: again   ['] branch , , ; immediate
: until   ['] 0branch , , ; immediate
: ahead   ['] branch , here 0 , ; immediate
: then   here swap ! ; immediate
: if   ['] 0branch , here 0 , ; immediate
: else   ['] branch , here 0 , swap here swap ! ; immediate
: while   ['] 0branch , here 0 , swap ; immediate
: repeat   ['] branch , , here swap ! ; immediate
: aft   drop ['] branch , here 0 , here swap ; immediate

( Compound words requiring conditionals )
: min 2dup < if drop else nip then ;
: max 2dup < if nip else drop then ;
: abs ( n -- +n ) dup 0< if negate then ;

( Dictionary Format )
: >name ( xt -- a n ) 3 cells - dup @ swap over aligned - swap ;
: >link& ( xt -- a ) 2 cells - ;   : >link ( xt -- a ) >link& @ ;
: >flags ( xt -- flags ) cell - ;
: >body ( xt -- a ) dup @ [ ' >flags @ ] literal = 2 + cells + ;

( Postpone - done here so we have ['] and IF )
: immediate? ( xt -- f ) >flags @ 1 and 0= 0= ;
: postpone ' dup immediate? if , else aliteral ['] , , then ; immediate

( Constants and Variables )
: constant ( n "name" -- ) create , does> @ ;
: variable ( "name" -- ) create 0 , ;

( Stack Convience )
sp@ constant sp0
rp@ constant rp0
: depth ( -- n ) sp@ sp0 - cell/ ;

( FOR..NEXT )
: for   postpone >r postpone begin ; immediate
: next   postpone donext , ; immediate

( DO..LOOP )
variable leaving
: leaving,   here leaving @ , leaving ! ;
: leaving(   leaving @ 0 leaving ! ;
: )leaving   leaving @ swap leaving !
             begin dup while dup @ swap here swap ! repeat drop ;
: (do) ( n n -- .. ) swap r> -rot >r >r >r ;
: do ( lim s -- ) leaving( postpone (do) here ; immediate
: (?do) ( n n -- n n f .. ) 2dup = if 2drop 0 else -1 then ;
: ?do ( lim s -- ) leaving( postpone (?do) postpone 0branch leaving,
                   postpone (do) here ; immediate
: unloop   postpone rdrop postpone rdrop ; immediate
: leave   postpone unloop postpone branch leaving, ; immediate
: (+loop) ( n -- f .. ) dup 0< swap r> r> rot + dup r@ < -rot >r >r xor 0= ;
: +loop ( n -- ) postpone (+loop) postpone until
                 postpone unloop )leaving ; immediate
: loop   1 aliteral postpone +loop ; immediate
: i ( -- n ) postpone r@ ; immediate
: j ( -- n ) rp@ 3 cells - @ ;

( Exceptions )
variable handler
: catch ( xt -- n )
  sp@ >r handler @ >r rp@ handler ! execute r> handler ! r> drop 0 ;
: throw ( n -- )
  dup if handler @ rp! r> handler ! r> swap >r sp! drop r> else drop then ;
' throw 'notfound !

( Values )
: value ( n -- ) create , does> @ ;
: to ( n -- ) state @ if postpone ['] postpone >body postpone !
                      else ' >body ! then ; immediate

( Deferred Words )
: defer ( "name" -- ) create 0 , does> @ dup 0= throw execute ;
: is ( xt "name -- ) postpone to ; immediate

( Defer I/O to platform specific )
defer type
defer key
defer bye
: emit ( n -- ) >r rp@ 1 type rdrop ;
: space bl emit ;   : cr nl emit ;

( Numeric Output )
variable hld
: pad ( -- a ) here 80 + ;
: digit ( u -- c ) 9 over < 7 and + 48 + ;
: extract ( n base -- n c ) u/mod swap digit ;
: <# ( -- ) pad hld ! ;
: hold ( c -- ) hld @ 1 - dup hld ! c! ;
: # ( u -- u ) base @ extract hold ;
: #s ( u -- 0 ) begin # dup while repeat ;
: sign ( n -- ) 0< if 45 hold then ;
: #> ( w -- b u ) drop hld @ pad over - ;
: str ( n -- b u ) dup >r abs <# #s r> sign #> ;
: hex ( -- ) 16 base ! ;   : octal ( -- ) 8 base ! ;
: decimal ( -- ) 10 base ! ;   : binary ( -- ) 2 base ! ;
: u. ( u -- ) <# #s #> type space ;
: . ( w -- ) base @ 10 xor if u. exit then str type space ;
: ? ( a -- ) @ . ;
: n. ( n -- ) base @ swap decimal <# #s #> type base ! ;

( Strings )
: parse-quote ( -- a n ) [char] " parse ;
: $place ( a n -- ) for aft dup c@ c, 1+ then next drop 0 c, align ;
: $@   r@ dup cell+ swap @ r> dup @ 1+ aligned + cell+ >r ;
: s"   parse-quote state @ if postpone $@ dup , $place
       else dup here swap >r >r $place r> r> then ; immediate
: ."   postpone s" state @ if postpone type else type then ; immediate
: z"   postpone s" state @ if postpone drop else drop then ; immediate
: r"   parse-quote state @ if swap aliteral aliteral then ; immediate
: r|   [char] | parse state @ if swap aliteral aliteral then ; immediate
: s>z ( a n -- z ) here >r $place r> ;
: z>s ( z -- a n ) 0 over begin dup c@ while 1+ swap 1+ swap repeat drop ;

( Fill, Move )
: cmove ( a a n -- ) for aft >r dup c@ r@ c! 1+ r> 1+ then next 2drop ;
: cmove> ( a a n -- ) for aft 2dup swap r@ + c@ swap r@ + c! then next 2drop ;
: fill ( a a n -- ) swap for swap aft 2dup c! 1 + then next 2drop ;

( Better Errors )
: notfound ( a n n -- )
   if cr ." ERROR: " type ."  NOT FOUND!" cr -1 throw then ;
' notfound 'notfound !

( Examine Dictionary )
: see. ( xt -- ) >name type space ;
: see-one ( xt -- xt+1 )
   dup cell+ swap @
   dup ['] DOLIT = if drop dup @ . cell+ exit then
   dup ['] $@ = if drop ['] s" see.
                   dup @ dup >r >r dup cell+ r> type cell+ r> aligned +
                   [char] " emit space exit then
   dup  ['] BRANCH =
   over ['] 0BRANCH = or
   over ['] DONEXT = or
       if see. cell+ exit then
   see. ;
: exit= ( xt -- ) ['] exit = ;
: see-loop   >body begin dup @ exit= 0= while see-one repeat drop ;
: see-xt ( xt -- )
        cr dup @ ['] see-loop @ <>
        if ." Unsupported word type: " see. cr exit then
        ['] : see.  dup see.  space see-loop   ['] ; see. cr ;
: see   ' see-xt ;

( Input )
: raw.s   depth 0 max for aft sp@ r@ cells - @ . then next ;
variable echo   -1 echo !
: ?echo ( n -- ) echo @ if emit else drop then ;
: ?echo-prompt   echo @ if >r >r raw.s r> r> ." --> " then ;
: accept ( a n -- n ) ?echo-prompt 0 swap begin 2dup < while
     key
     dup nl = if ?echo drop nip exit then
     dup 8 = over 127 = or if
       drop over if rot 1- rot 1- rot 8 ?echo bl ?echo 8 ?echo then
     else
       dup ?echo
       >r rot r> over c! 1+ -rot swap 1+ swap
     then
   repeat drop nip ;
200 constant input-limit
: tib ( -- a ) 'tib @ ;
create input-buffer   input-limit allot
: tib-setup   input-buffer 'tib ! ;
: refill   tib-setup tib input-limit accept #tib ! 0 >in ! -1 ;

( REPL )
: prompt   ."  ok" cr ;
: evaluate-buffer   begin >in @ #tib @ < while evaluate1 repeat ;
: evaluate ( a n -- ) 'tib @ >r #tib @ >r >in @ >r
                      #tib ! 'tib ! 0 >in ! evaluate-buffer
                      r> >in ! r> #tib ! r> 'tib ! ;
: quit    begin ['] evaluate-buffer catch
          if 0 state ! sp0 sp! rp0 rp! ." ERROR" cr then
          prompt refill drop again ;
: ok   ." uEForth" cr prompt refill drop quit ;
