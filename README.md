# FM Tracker
A software FM synth and a tracker to go with it.

(if anyone has a better name please let me know)

## The synth
The synth itself is a very basic 2op sine-only synth with feedback (like the OPL/YM3526). One "synth" is also only one channel.
This tracker uses eight synths for eight FM channels.

The unusual thing about this synth is that it's entirely linear. The envelope generators and modulation levels are all linear.
It also has seperate carrier and modulator frequencies (this tracker doesn't use that though).

*Note: This is still unfinished and there's a good chance I'll add more features in the future.*

## The tracker
The tracker is a pretty simple tracker with a note column, an instrument column, and an effect column.
The pattern view is a custom control. The rest of the control (apart from the numeric up/down ones) are all standard WPF controls.

This is also my first time using WPF for anything big.

## Libraries used
- NAudio for the audio output
- Dirkster.NumericUpDownLib for the numeric up/down controls
