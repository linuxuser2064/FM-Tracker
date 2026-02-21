Imports NAudio.Wave
Public Class FMSynthProvider
    Implements ISampleProvider

    Private ReadOnly sampleRate As Integer = 48000
    Private phaseCarrier As Double = 0
    Private phaseModulator As Double = 0
    Private frequencyCarrier As Double = 440.0
    Private frequencyModulator As Double = 440.0
    Private modulationIndex As Double = 0.0
    Private maxModulationIndex As Double = 0.0
    Private multiplierCarrier As Double = 1.0
    Private multiplierModulator As Double = 1.0
    Private feedback As Double = 0.0
    Private maxVolumeValue As Double = 1
    Private volumeValue As Double = 0
    Private previousModulation As Double = 0.0

    ' ADSR envelope parameters for volume
    Public attackTime As Double = 0 ' seconds
    Public decayTime As Double = 0 ' seconds
    Public sustainLevel As Double = 1 ' 0 to 1
    Public releaseTime As Double = 0 ' seconds
    Private envelopeState As String = "off"
    Private noteOnTime As Double = 0

    ' ADSR envelope parameters for modulation depth
    Public attackTimeMod As Double = 0 ' seconds
    Public decayTimeMod As Double = 0.5 ' seconds
    Public sustainLevelMod As Double = 0 ' 0 to 1
    Public releaseTimeMod As Double = 0.2 ' seconds
    Private envelopeStateMod As String = "off"
    Private noteOnTimeMod As Double = 0

    ' Pitch adjustment parameters
    Private pitchChangePerReadVal As Double = 0.0
    Private initialFrequencyCarrier As Double = 440.0
    Public ReadOnly Property WaveFormat() As WaveFormat Implements ISampleProvider.WaveFormat
        Get
            Return WaveFormat.CreateIeeeFloatWaveFormat(48000, 1)
        End Get
    End Property
    Public Sub New()
        MyBase.New()
    End Sub

    Public Property Volume As Single
        Get
            Return volumeValue
        End Get
        Set(ByVal value As Single)
            volumeValue = value
            maxVolumeValue = value
        End Set
    End Property

    Public Property CarrierFrequency As Double
        Get
            Return frequencyCarrier
        End Get
        Set(value As Double)
            frequencyCarrier = value
            initialFrequencyCarrier = value
        End Set
    End Property

    Public Property ModulatorFrequency As Double
        Get
            Return frequencyModulator
        End Get
        Set(value As Double)
            frequencyModulator = value
        End Set
    End Property

    Public Property ModulationDepth As Double
        Get
            Return modulationIndex
        End Get
        Set(value As Double)
            modulationIndex = value
            maxModulationIndex = value
        End Set
    End Property

    Public Property CarrierMultiplier As Double
        Get
            Return multiplierCarrier
        End Get
        Set(value As Double)
            multiplierCarrier = value
            If multiplierCarrier < 0.5 Then multiplierCarrier = 0.5
        End Set
    End Property

    Public Property ModulatorMultiplier As Double
        Get
            Return multiplierModulator
        End Get
        Set(value As Double)
            multiplierModulator = value
            If multiplierModulator < 0.5 Then multiplierModulator = 0.5
        End Set
    End Property

    Public Property FeedbackAmount As Double
        Get
            Return feedback
        End Get
        Set(value As Double)
            feedback = value
        End Set
    End Property

    Public Property PitchChangePerRead As Double
        Get
            Return pitchChangePerReadVal
        End Get
        Set(value As Double)
            pitchChangePerReadVal = value
        End Set
    End Property

    ' ADSR envelope functions
    Public Sub StartNote()
        envelopeState = "attack"
        envelopeStateMod = "attack"
        noteOnTime = 0
        noteOnTimeMod = 0
        frequencyCarrier = initialFrequencyCarrier
        phaseCarrier = 0
        phaseModulator = 0
    End Sub
    Public Sub StartNoteCarrier()
        envelopeState = "attack"
        noteOnTime = 0
        frequencyCarrier = initialFrequencyCarrier
        phaseCarrier = 0
        phaseModulator = 0
    End Sub
    Public Sub StartNoteModulator()
        envelopeStateMod = "attack"
        noteOnTimeMod = 0
        frequencyCarrier = initialFrequencyCarrier
        phaseCarrier = 0
        phaseModulator = 0
    End Sub
    Public Sub ReleaseNote()
        envelopeState = "release"
        envelopeStateMod = "release"
        noteOnTime = 0
        noteOnTimeMod = 0
    End Sub

    Public Sub StopNote()
        envelopeState = "off"
        envelopeStateMod = "off"
        volumeValue = 0
        modulationIndex = 0
        frequencyCarrier = initialFrequencyCarrier
    End Sub

    Private Function GetEnvelopeValue() As Double
        Select Case envelopeState
            Case "attack"
                If noteOnTime < attackTime Then
                    Return noteOnTime / attackTime
                Else
                    envelopeState = "decay"
                    noteOnTime = 0
                    Return 1.0
                End If
            Case "decay"
                If noteOnTime < decayTime Then
                    Return 1.0 - (1.0 - sustainLevel) * (noteOnTime / decayTime)
                Else
                    envelopeState = "sustain"
                    Return sustainLevel
                End If
            Case "sustain"
                Return sustainLevel
            Case "release"
                If noteOnTime < releaseTime Then
                    Return sustainLevel * (1.0 - noteOnTime / releaseTime)
                Else
                    envelopeState = "off"
                    Return 0.0
                End If
            Case Else
                Return 0.0
        End Select
    End Function

    Private Function GetEnvelopeValueMod() As Double
        Select Case envelopeStateMod
            Case "attack"
                If noteOnTimeMod < attackTimeMod Then
                    Return noteOnTimeMod / attackTimeMod
                Else
                    envelopeStateMod = "decay"
                    noteOnTimeMod = 0
                    Return 1.0
                End If
            Case "decay"
                If noteOnTimeMod < decayTimeMod Then
                    Return 1.0 - (1.0 - sustainLevelMod) * (noteOnTimeMod / decayTimeMod)
                Else
                    envelopeStateMod = "sustain"
                    Return sustainLevelMod
                End If
            Case "sustain"
                Return sustainLevelMod
            Case "release"
                If noteOnTimeMod < releaseTimeMod Then
                    Return sustainLevelMod * (1.0 - noteOnTimeMod / releaseTimeMod)
                Else
                    envelopeStateMod = "off"
                    Return 0.0
                End If
            Case Else
                Return 0.0
        End Select
    End Function
    Public Function GetOplFeedback(value As Double) As Double
        ' Clamp input
        value = Math.Max(0.0, Math.Min(1.0, value))

        ' OPL feedback table
        Dim table() As Double = {
        0.0,
        Math.PI / 16.0,
        Math.PI / 8.0,
        Math.PI / 4.0,
        Math.PI / 2.0,
        Math.PI,
        2.0 * Math.PI,
        4.0 * Math.PI
    }

        ' Scale 0–1 into 0–7 range
        Dim scaled = value * (table.Length - 1)

        Dim index As Integer = CInt(Math.Floor(scaled))
        Dim frac As Double = scaled - index

        ' Prevent overflow at top edge
        If index >= table.Length - 1 Then
            Return table(table.Length - 1)
        End If

        ' Linear interpolate between steps
        Return table(index) + (table(index + 1) - table(index)) * frac
    End Function
    Public Function Read(buffer As Single(), offset As Integer, count As Integer) As Integer Implements ISampleProvider.Read

        For i As Integer = 0 To count - 1

            ' Update envelope
            noteOnTime += 1.0 / sampleRate
            noteOnTimeMod += 1.0 / sampleRate
            volumeValue = GetEnvelopeValue() * maxVolumeValue
            modulationIndex = GetEnvelopeValueMod() * maxModulationIndex

            ' Adjust pitch
            frequencyCarrier += pitchChangePerReadVal
            frequencyCarrier = Math.Min(sampleRate / 2, Math.Max(0, frequencyCarrier))
            frequencyModulator += pitchChangePerReadVal
            frequencyModulator = Math.Min(sampleRate / 2, Math.Max(0, frequencyModulator))
            ' === OPL-style feedback ===
            ' Average last two outputs (closer to YM3812 behavior)
            Dim feedbackInput As Double = previousModulation

            ' Apply feedback to phase (range 0–2π)
            Dim modPhase As Double = (phaseModulator * 2.0 * Math.PI) + (GetOplFeedback(feedback) * feedbackInput)

            ' Generate modulator output
            Dim modulatorSample As Double = Math.Sin(modPhase) * modulationIndex

            ' Shift stored feedback samples
            previousModulation = modulatorSample

            ' Advance modulator phase
            phaseModulator += (frequencyModulator * multiplierModulator) / sampleRate
            If phaseModulator >= 1.0 Then phaseModulator -= 1.0

            ' Carrier modulated by modulator
            Dim carrierPhase As Double = (phaseCarrier + modulatorSample) * 2.0 * Math.PI
            Dim carrierSample As Double = Math.Sin(carrierPhase)

            ' Advance carrier phase
            phaseCarrier += (frequencyCarrier * multiplierCarrier) / sampleRate
            If phaseCarrier >= 1.0 Then phaseCarrier -= 1.0

            buffer(offset + i) = CSng(carrierSample * volumeValue)

        Next

        Return count
    End Function
End Class