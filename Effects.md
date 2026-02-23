# Current effects:
- 0xy: Arpeggio. Switches between the base note and two other notes every tick.
  - x: First note of the arpeggio (offset from base note)
  - y: Second note of the arpeggio (offset from base note)
- 1xx: Pitch slide up.
  - x: The pitch slide's speed.
- 2xx: Pitch slide down. (see 1xx for parameter desc.)
- 4xy: Vibrato
  - x: Vibrato speed
  - y: Vibrato depth
- 5xy: Detune
  - x: Detune for modulator (0-F, 8 is center/none)
  - y: Detune for carrier (0-F, 8 is center/none)
- Bxx: Jump to pattern.
  - x: Pattern to jump to
- Cxx: Set volume
  - x: Volume to set (00-FF)
- Dxx: Jump to next pattern
- Lxx: Legato
  - x: 1 turns on, 0 turns off.
- Rxx: Don't (re)start carrier envelope. (if enabled only the envelope on the modulator will trigger)
  - x: 1 turns on, 0 turns off.
 
More to come
