\ ==============================================================================
\
\            zif - the gzip file inflate module in the ffl
\
\               Copyright (C) 2008  Dick van Oudheusden
\  
\ This library is free software; you can redistribute it and/or
\ modify it under the terms of the GNU General Public
\ License as published by the Free Software Foundation; either
\ version 2 of the License, or (at your option) any later version.
\
\ This library is distributed in the hope that it will be useful,
\ but WITHOUT ANY WARRANTY; without even the implied warranty of
\ MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
\ General Public License for more details.
\
\ You should have received a copy of the GNU General Public
\ License along with this library; if not, write to the Free
\ Software Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
\
\ ==============================================================================
\ 
\  $Date: 2008-10-19 06:06:23 $ $Revision: 1.2 $
\
\ ==============================================================================


include ffl/config.fs


[UNDEFINED] zif.version [IF]

include ffl/gzf.fs
include ffl/gzi.fs

( zif = gzip file reader )
( The zif module implements a gzip file reader. The header information from  )
( the file can be accessed by the offset zif>gzp after zif-read-header.      )


1 constant zif.version


( Default buffer size )

2048 value zif.size        ( -- n = Default input buffer size )


( zif structure )

begin-structure zif%       ( -- n = Get the required space for a zif variable )
  gzi%
  +field  zif>gzi            \ the gzip file reader extends the gzip inflator
  gzf%
  +field  zif>gzf            \ the gzip file header
  field:  zif>file           \ the current file
  field:  zif>eof            \ is the end of file reached for the current file ?
  field:  zif>buffer         \ the input buffer
  field:  zif>result         \ Result
end-structure


( GZip file reader variable creation, initialisation and destruction )

: zif-init         ( zif -- = Initialise the GZip file reader variable )
  dup  zif>gzi    gzi-init
  dup  zif>gzf    gzf-init
  dup  zif>file   0!
  dup  zif>eof    off
  zif.size allocate throw
  over zif>buffer !
  dup  zif>result 0!
  drop
\ ToDo
;


: zif-(free)       ( zif -- = Free the internal, private variables from the heap )
  dup zif>buffer @ free throw
  dup zif>gzi      gzi-(free)
  dup zif>gzf      gzf-(free)
  drop
  \ ToDo
;


: zif-create       ( "<spaces>name" -- ; -- zif = Create a named GZip file reader variable in the dictionary )
  create   here   zif% allot   zif-init
;


: zif-new          ( -- zif = Create a new GZip file reader variable on the heap )
  zif% allocate  throw  dup zif-init
;


: zif-free         ( zif -- = Free the variable from the heap )
  dup zif-(free)             \ Free the internal, private variables from the heap

  free throw                 \ Free the zif
;


( Private words )

: zif-file@           ( zif -- fileid = Get the file id of the gzip file )
  zif>file @
;


: zif-read            ( zif -- n = Read another block of data from the file )
  >r
  r@ zif>eof @ IF
    exp-no-data
  ELSE
    r@ zif>buffer @  zif.size  r@ zif-file@  read-file ?dup IF \ ToDo zif.size
      nip
    ELSE
      ?dup IF                                    \ If data available Then
        dup zif.size < r@ zif>eof !              \   Not all available -> eof
        r@ zif>buffer @ swap  r@ bis-set         \   Setup buffer in gzp module
        gzi.ok
      ELSE                                       \ Else end of file
        r@ zif>eof on
        exp-no-data
      THEN
    THEN
  THEN
  rdrop
;


( Private header words )

: zif-do-crc       ( zif -- n = Skip CRC )
  dup zif>gzf gzf-flags@ 2 AND IF
    2 swap bis-read-bytes IF 
      drop
      gzi.done                         \ Header is succesfull processed
    ELSE
      gzi.more
    THEN
  ELSE
    drop gzi.done                      \ No CRC -> done
  THEN
;

  
: zif-do-comment   ( zif -- n = Read the comment )
  dup zif>gzf gzf-flags@ 16 AND IF
    BEGIN
      1 over bis-read-bytes IF
        ?dup IF                             \ If not eos Then
          over zif>gzf gzf>comment str-append-char  \   Append char
          false                             \   Continue
        ELSE                                \ Else
          ['] zif-do-crc swap gzi-state!    \   Next skip crc
          gzi.ok true                       \   Done & okee
        THEN
      ELSE
        drop gzi.more true                  \ No data -> more
      THEN
    UNTIL
  ELSE
    ['] zif-do-crc swap gzi-state!          \ Next skip crc
    gzi.ok
  THEN
;

 
: zif-do-name      ( zif -- n = Read the filename )
  dup zif>gzf gzf-flags@ 8 AND IF
    BEGIN
      1 over bis-read-bytes IF
        ?dup IF                          \ If not eos Then
          over zif>gzf gzf>name str-append-char  \   Append char  
          false                          \   Continue
        ELSE                             \ Else
          ['] zif-do-comment swap gzi-state! \  Next: comment
          gzi.ok true                    \   Done & okee
        THEN
      ELSE
        drop gzi.more true               \ No data -> more
      THEN
    UNTIL
  ELSE
    ['] zif-do-comment swap gzi-state!  \ Next: comment
    gzi.ok
  THEN
;

      
: zif-do-extra     ( zif -- n = Skip the extra bytes )
  >r
  dup zif>gzf gzf>xlen @          \ Skip xlen byte
  BEGIN
    dup IF
      1 r@ bis-read-bytes
    ELSE
      false
    THEN
  WHILE
    1-
  REPEAT

  ?dup IF                         \ All bytes done ?
    r@ zif>gzf gzf>xlen !
    gzi.more                      \  No, need more data
  ELSE
    ['] zif-do-name r@ gzi-state! \  Yes, continu with next field
    gzi.ok
  THEN
  rdrop
;


: zif-do-xlen      ( zif -- n = Read the extra length )
  dup zif>gzf gzf-flags@ 4 AND IF
    2 over bis-read-bytes IF
      over zif>gzf gzf>xlen !
      ['] zif-do-extra swap gzi-state!  \ Next: extra bytes
      gzi.ok
    ELSE
      drop gzi.more
    THEN
  ELSE
    ['] zif-do-name swap gzi-state!  \ Next: name
    gzi.ok
  THEN
;


: zif-do-os        ( zif -- n = Save the Operating System )
  1 over bis-read-bytes IF
    over zif>gzf gzf-os!
    ['] zif-do-xlen swap gzi-state!  \ Next: extra fields
    gzi.ok
  ELSE
    drop gzi.more
  THEN
;


: zif-do-xflags    ( zif -- n = Check and save the extra flags )
  1 over bis-read-bytes IF
    over zif>gzf gzf>xflags ! \ ToDo Check
    ['] zif-do-os swap gzi-state!  \ Next: os
    gzi.ok
  ELSE
    drop gzi.more
  THEN
;


: zif-do-mtime     ( zif -- n = Check and process the modification time )
  4 over bis-read-bytes IF
    over zif>gzf gzf-mtime!
    ['] zif-do-xflags swap gzi-state!  \ Next: extra flags
    gzi.ok
  ELSE
    drop gzi.more
  THEN
;


: zif-do-flags     ( zif -- n = Check and process the flags )
  1 over bis-read-bytes IF
    2dup 1 AND 0<> swap zif>gzf gzf-text!
    over zif>gzf gzf-flags!
    ['] zif-do-mtime swap gzi-state!   \ Next: mtime
    gzi.ok
  ELSE
    drop gzi.more
  THEN
;

  
: zif-do-cm        ( zif -- n = Check the Compression Mode )
  1 over bis-read-bytes IF
    gzf.deflate = IF         \ Only support deflate
      ['] zif-do-flags swap gzi-state!  \ Next: flags
      gzi.ok
    ELSE
      drop exp-wrong-file-data
    THEN
  ELSE
    drop gzi.more
  THEN
;


: zif-do-id        ( zif -- n = Check the IDs from the gzip file )
  2 over bis-read-bytes IF
    35615 = IF
      ['] zif-do-cm swap gzi-state!   \ Next: check CM
      gzi.ok
    ELSE
      drop exp-wrong-file-type
    THEN
  ELSE
    drop gzi.more
  THEN
;


( File words )

: zif-open-file    ( c-addr u zif -- ior = Open an existing gzip file for reading with name c-addr u )
  >r
  r/o bin open-file ?dup IF
    nip
  ELSE
    r@ zif>file    !         \ Save file id
    r@ zif>eof     off      
    ['] zif-do-id            \ Header: check ids
    r@ gzi-state!            \ zif extends gzi (and bis)
    r@ bis-reset
    0
  THEN
  rdrop
;


: zif-read-header  ( zif -- ior = Read the [next] header from the gzip file )
  >r

  r@ zif>gzf gzf-reset            \ Reset the header

  gzi.ok r@ zif>result  !
  BEGIN
    r@ gzi-inflate                \ Do the next step in inflation: header reading
    dup gzi.more = IF             \ If more file data is needed Then
      drop
      r@ zif-read                 \   Read another buffer of data
    THEN
    ?dup
  UNTIL                           \ Continue until done or error
  dup gzi.done = IF
    r@ gzi-init-inflate           \ If done Then Start inflating and ..
    drop 0                        \ .. return okee
  THEN
  rdrop
;


0 [IF]
: zif-read-file    ( c-addr1 u1 zif -- u2 ior = Read/decompress maximum u1 bytes from the file and store those at c-addr1, return the actual read bytes )
  >r
  r@ zif>result @                 \ Inflate until u1 bytes and okee
  BEGIN
    gzi.ok = IF
      r@ zif>gzp gzp-get-length over <
    ELSE
      false
    THEN
  WHILE
    r@ zif>gzp gzp-inflate 
    dup gzi.more = IF             \ Read more data
      drop
      r@ zif-read
    THEN
  REPEAT
  dup r@ zif>result !

  dup gzi.done = IF               \ Inflate done
    drop
    r@ zif>gzp gzp-end-inflate
    gzi.ok
  THEN

  dup gzi.ok = IF
    drop
    dup r@ zif>gzp gzp-get ?dup IF
      rot min >r                  \ Min of requested and present length
      swap r@ move                \ Switch source and dest and move
      r>                          \ Return length
    ELSE
      2drop
      0
    THEN
    gzi.ok
  ELSE
    nip nip
    0 swap
  THEN
  rdrop
;


: zif-read-line    ( c-addr1 u1 zif -- u2 ior = Read/decompress till end of line or maximum u1 bytes from the file and store those at c-addr1, return the actual read bytes )
\ ToDo
;

[THEN]
: zif-close-file   ( zif -- ior = Close the file )
  zif-file@ close-file 
;

[THEN]

\ ==============================================================================
