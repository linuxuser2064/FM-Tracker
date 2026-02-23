Imports System.Runtime.InteropServices

Public Module defines
    Public NoteMap As New Dictionary(Of String, Double) From {
        {"C-", 261.62556530059862}, {"C#", 277.18263097687208}, {"D-", 293.66476791740757}, {"D#", 311.12698372208092}, {"E-", 329.62755691286992}, {"F-", 349.22823143300388}, {"F#", 369.9944227116344}, {"G-", 391.99543598174927}, {"G#", 415.30469757994513}, {"A-", 440}, {"A#", 466.16376151808993}, {"B-", 494.88330125612413}}
    Public Sub CalculateNewNoteMap(Optional tuning As Double = 440.0)

        Dim semitoneOffsets As New Dictionary(Of String, Integer) From {
            {"C-", -9}, {"C#", -8}, {"D-", -7}, {"D#", -6},
            {"E-", -5}, {"F-", -4}, {"F#", -3}, {"G-", -2},
            {"G#", -1}, {"A-", 0}, {"A#", 1}, {"B-", 2}
        }

        NoteMap.Clear()

        For Each kvp In semitoneOffsets
            Dim frequency As Double = tuning * Math.Pow(2, kvp.Value / 12.0)
            NoteMap(kvp.Key) = frequency
        Next

    End Sub
End Module
Public Class Instrument
    Public Property Name As String
    Public Property Feedback As Single
    Public Property PitchMacro As Double
    Public Property CAttack As Single
    Public Property CDecay As Single
    Public Property CSustain As Single
    Public Property CRelease As Single
    Public Property CMultiplier As Int32
    Public Property CTL As Single
    Public Property MAttack As Single
    Public Property MDecay As Single
    Public Property MSustain As Single
    Public Property MRelease As Single
    Public Property MMultiplier As Int32
    Public Property MTL As Single
    Public Sub New()

    End Sub
    Public Function Clone() As Instrument
        Return New Instrument(Name, MTL, CMultiplier, MMultiplier, Feedback, New ADSR(CAttack, CDecay, CSustain, CRelease), New ADSR(MAttack, MDecay, MSustain, MRelease), CTL, PitchMacro)
    End Function
    Public Sub New(iName As String, iTL As Single, iCarrierMult As Single, iModMult As Int32, iFeedback As Double, Optional Volume As Double = 1)
        MTL = iTL
        MMultiplier = iModMult
        CMultiplier = iCarrierMult
        Feedback = iFeedback
        CTL = Volume
        Name = iName
        CAttack = 0  ' some default values
        CDecay = 0
        CSustain = 1
        CRelease = 0
        MAttack = 0
        MDecay = 0
        MSustain = 1
        MRelease = 0
        PitchMacro = 0
    End Sub
    Public Sub New(iName As String, iTL As Single, iCarrierMult As Single, iModMult As Int32, iFeedback As Double, carrierADSR As ADSR, modulatorADSR As ADSR, Optional Volume As Double = 1, Optional PitchMacro As Double = 0)
        MTL = iTL
        MMultiplier = iModMult
        CMultiplier = iCarrierMult
        Feedback = iFeedback
        CTL = Volume
        Name = iName
        CAttack = carrierADSR.A
        CDecay = carrierADSR.D
        CSustain = carrierADSR.S
        CRelease = carrierADSR.R
        MAttack = modulatorADSR.A
        MDecay = modulatorADSR.D
        MSustain = modulatorADSR.S
        MRelease = modulatorADSR.R
        Me.PitchMacro = PitchMacro
    End Sub
End Class
Public Structure ADSR
    Public A As Single
    Public D As Single
    Public S As Single
    Public R As Single
    Public Sub New(cA, cD, cS, cR)
        A = cA
        D = cD
        S = cS
        R = cR
    End Sub
End Structure
Public Structure Note
    Public Property NoteStr As String
    Public Property Frequency As Int32
    Public Property InstrumentNum As Int32
    Public Property EffectLetter As Char
    Public Property EffectData As Byte
    ' note to self dont make blank sub new cuz this is a struct
    Public Shared Function GetNoteFrequency(noteStr As String, Optional offset As Integer = 0) As Double
        If noteStr = "" Or noteStr = "..." Then Return 0
        If noteStr = "OFF" Then Return 0

        Dim noteLetter As String = noteStr.Substring(0, 2)
        Dim octave As Integer = Val(noteStr.Substring(2, 1))

        If Not NoteMap.ContainsKey(noteLetter) Then Return 0

        ' Get base frequency for octave 4
        Dim freq As Double = NoteMap(noteLetter)

        ' Adjust for octave
        Dim octaveDiff As Integer = octave - 4
        freq *= Math.Pow(2, octaveDiff)

        ' Apply optional semitone offset
        If offset <> 0 Then
            freq *= Math.Pow(2, offset / 12.0)
        End If

        Return freq
    End Function
    Public Function Clone() As Note
        Return New Note(Me.NoteStr,
                    Me.InstrumentNum,
                    Me.EffectLetter,
                    Me.EffectData)
    End Function
    Public Sub New(noteStr As String, instrumentNumber As Int32, Optional fxLetter As String = ".", Optional fxData As Byte = 0)
        Me.NoteStr = noteStr
        Me.EffectLetter = fxLetter
        Me.EffectData = fxData
        If noteStr = "" Or noteStr = "..." Then
            Me.NoteStr = "..."
            Frequency = 0
            InstrumentNum = -1
        ElseIf noteStr = "OFF" Then
            Frequency = 0
            InstrumentNum = -2
        Else
            Dim NoteLetter = noteStr.Remove(2)
            Dim Octave = Val(noteStr.ToCharArray()(2))
            Frequency = NoteMap(NoteLetter)
            If Octave = 1 Then Frequency \= 8
            If Octave = 2 Then Frequency \= 4
            If Octave = 3 Then Frequency \= 2
            ' nothing to do for octave 4 (root octave)
            If Octave = 5 Then Frequency *= 2
            If Octave = 6 Then Frequency *= 4
            If Octave = 7 Then Frequency *= 8
            InstrumentNum = instrumentNumber
        End If
    End Sub
End Structure
Public Class Song
    Public Property Name As String
    Public Property Speed As Integer
    Public Property Instruments As Instrument()
    Public Property Patterns1 As List(Of Note())
    Public Property Patterns2 As List(Of Note())
    Public Property Patterns3 As List(Of Note())
    Public Property Patterns4 As List(Of Note())
    Public Property Patterns5 As List(Of Note())
    Public Property Patterns6 As List(Of Note())
    Public Property Patterns7 As List(Of Note())
    Public Property Patterns8 As List(Of Note())
    Public Property Tuning As Double = 440.0
    Public Sub New()
    End Sub
    Public Sub New(songName As String, songSpeed As Integer, instruments As Instrument(),
                   Patterns1 As List(Of Note()), Patterns2 As List(Of Note()), Patterns3 As List(Of Note()),
                   Patterns4 As List(Of Note()), Patterns5 As List(Of Note()), Patterns6 As List(Of Note()),
                   Patterns7 As List(Of Note()), Patterns8 As List(Of Note()), tuning As Double)
        Name = songName
        Speed = songSpeed
        Me.Instruments = instruments
        Me.Patterns1 = Patterns1
        Me.Patterns2 = Patterns2
        Me.Patterns3 = Patterns3
        Me.Patterns4 = Patterns4
        Me.Patterns5 = Patterns5
        Me.Patterns6 = Patterns6
        Me.Patterns7 = Patterns7
        Me.Patterns8 = Patterns8
        Me.Tuning = tuning
    End Sub
End Class
Public Class ChannelStatus
    Public OriginalFrequency As Double = 0

    Public LegatoActive As Boolean = False

    Public VibratoActive As Boolean = False
    Public VibratoDepth As Double = 0
    Public VibratoSpeed As Double = 0
    Public VibratoPhase As Double = 0

    Public PitchSlideActive As Boolean = False
    Public PitchSlideSpeed As Double = 0 ' can go positive and negative for both directions

    Public ArpeggioActive As Boolean = False
    Public ArpeggioBaseNote As String = ""
    Public ArpeggioNote1 As Byte = 0 ' these are offsets btw
    Public ArpeggioNote2 As Byte = 0
    Public ArpeggioPhase As Byte = 0 ' 0 1 2 0 1 2 etc

    Public DontStartEnvelope As Boolean = False
    Public Sub Reset()
        OriginalFrequency = 0
        LegatoActive = False
        VibratoActive = False
        VibratoDepth = 0
        VibratoSpeed = 0
        VibratoPhase = 0

        PitchSlideActive = False
        PitchSlideSpeed = 0

        ArpeggioActive = False
        ArpeggioBaseNote = ""
        ArpeggioNote1 = 0
        ArpeggioNote2 = 0
        ArpeggioPhase = 0

        DontStartEnvelope = False
    End Sub
End Class
Public Class AccurateTimer
    Implements IDisposable

    Private Delegate Sub TimerEventDelegate(uTimerID As UInteger, uMsg As UInteger, dwUser As UIntPtr, dw1 As UIntPtr, dw2 As UIntPtr)

    <DllImport("winmm.dll")>
    Private Shared Function timeBeginPeriod(uMilliseconds As UInteger) As UInteger
    End Function

    <DllImport("winmm.dll")>
    Private Shared Function timeEndPeriod(uMilliseconds As UInteger) As UInteger
    End Function

    <DllImport("winmm.dll")>
    Private Shared Function timeSetEvent(
        uDelay As UInteger,
        uResolution As UInteger,
        lpTimeProc As TimerEventDelegate,
        dwUser As UIntPtr,
        fuEvent As UInteger
    ) As UInteger
    End Function

    <DllImport("winmm.dll")>
    Private Shared Function timeKillEvent(uTimerID As UInteger) As UInteger
    End Function

    Private Const TIME_PERIODIC As UInteger = 1
    Private Const TIME_CALLBACK_FUNCTION As UInteger = &H0

    Private ReadOnly _interval As UInteger
    Private ReadOnly _callback As Action
    Private _timerId As UInteger
    Private _timerProc As TimerEventDelegate
    Private _running As Boolean

    Public Sub New(intervalMs As UInteger, callback As Action)
        _interval = intervalMs
        _callback = callback
        _timerProc = AddressOf TimerTick
    End Sub

    Public Sub Start()
        If _running Then Return
        timeBeginPeriod(1) ' request 1 ms system timer resolution
        _timerId = timeSetEvent(_interval, 0, _timerProc, UIntPtr.Zero, TIME_PERIODIC Or TIME_CALLBACK_FUNCTION)
        _running = True
    End Sub

    Public Sub [Stop]()
        If Not _running Then Return
        timeKillEvent(_timerId)
        timeEndPeriod(1)
        _running = False
    End Sub

    Private Sub TimerTick(uTimerID As UInteger, uMsg As UInteger, dwUser As UIntPtr, dw1 As UIntPtr, dw2 As UIntPtr)
        _callback?.Invoke()
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        [Stop]()
    End Sub
End Class