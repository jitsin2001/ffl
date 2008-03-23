\ ==============================================================================
\
\               make - the 'make' source file for gforth
\
\               Copyright (C) 2005  Dick van Oudheusden
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
\  $Date: 2008-03-23 07:19:36 $ $Revision: 1.1 $
\
\ ==============================================================================

include ffl/config.fs

.( Forth Foundation Library: ) cr

ms@

unused

include ffl/ffl.fs

unused -


.( Compilation Size: ) . .( bytes) cr
  
ms@ swap -

.( Compilation Time: ) . .( msec) cr

\ ==============================================================================

