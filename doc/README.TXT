please note:

- RS232 cable connection to a PC (recommended):

	9-pin (DC500)	9-pin (PC)  or	25-pin (PC)
	      2		     3		    2
	      3		     2		    3
	      5		     5		    7
	   7 <-> 8
	1 <-> 4 <-> 6
			  7 <-> 8
		       1 <-> 4 <-> 6
					 4 <-> 5
				      6 <-> 8 <-> 20

  The pins marked "<->" are short-circuits of the handshake signals on the same plug
  and may not be connected to the second plug.

- the IEEE version supports only National Instruments GPIB - please modify the source
  code and recompile if you have another GPIB interface
