Imports System.IO
Imports System.IO.Compression
Imports System.Text.Json
Imports Microsoft.Win32
Imports NAudio.Wave
Imports NAudio.Wave.SampleProviders
Class MainWindow
    Dim PlayTimer As New AccurateTimer(20, AddressOf PlayTimer_Tick)

    Private WithEvents wo As New WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Shared, 1)
    Public WithEvents FMSynth1 As New FMSynthProvider With {.Volume = 0.125}
    Public WithEvents FMSynth2 As New FMSynthProvider With {.Volume = 0.125}
    Public WithEvents FMSynth3 As New FMSynthProvider With {.Volume = 0.125}
    Public WithEvents FMSynth4 As New FMSynthProvider With {.Volume = 0.125}
    Public WithEvents FMSynth5 As New FMSynthProvider With {.Volume = 0.125}
    Public WithEvents FMSynth6 As New FMSynthProvider With {.Volume = 0.125}
    Public WithEvents FMSynth7 As New FMSynthProvider With {.Volume = 0.125}
    Public WithEvents FMSynth8 As New FMSynthProvider With {.Volume = 0.125}
    Public Mixer As New MixingSampleProvider(FMSynth1.WaveFormat)

    Public CurrentPlayRow As Int32 = 0
    Public CurrentPattern As Int32 = 0
    Public EditEnabled As Boolean = False
    Public IsPlaying As Boolean = False
    Public WaitCounter = 0
    Public LoopCurrentPattern As Boolean = False

    Public Patterns1 As New List(Of Note())
    Public Patterns2 As New List(Of Note())
    Public Patterns3 As New List(Of Note())
    Public Patterns4 As New List(Of Note())
    Public Patterns5 As New List(Of Note())
    Public Patterns6 As New List(Of Note())
    Public Patterns7 As New List(Of Note())
    Public Patterns8 As New List(Of Note())
    Public ChannelStatus1 As New ChannelStatus
    Public ChannelStatus2 As New ChannelStatus
    Public ChannelStatus3 As New ChannelStatus
    Public ChannelStatus4 As New ChannelStatus
    Public ChannelStatus5 As New ChannelStatus
    Public ChannelStatus6 As New ChannelStatus
    Public ChannelStatus7 As New ChannelStatus
    Public ChannelStatus8 As New ChannelStatus

    Public Instruments As New List(Of Instrument)

    Public PreviousSelectedPattern As Integer = 0

    Dim editor As New InstrumentEditor
    Public Shared Function Clamp(val, min, max)
        If val < min Then Return min
        If val > max Then Return max
        Return val
    End Function
    Public Shared Sub SetChannelInstrument(syn As FMSynthProvider, ins As Instrument)
        syn.CarrierMultiplier = ins.CMultiplier
        syn.ModulatorMultiplier = ins.MMultiplier
        syn.ModulationDepth = ins.MTL
        syn.Volume = ins.CTL * 0.125
        syn.attackTime = ins.CAttack
        syn.decayTime = ins.CDecay
        syn.sustainLevel = ins.CSustain
        syn.releaseTime = ins.CRelease
        syn.attackTimeMod = ins.MAttack
        syn.decayTimeMod = ins.MDecay
        syn.sustainLevelMod = ins.MSustain
        syn.releaseTimeMod = ins.MRelease
        syn.PitchChangePerRead = ins.PitchMacro ' should be in playback timer
        syn.FeedbackAmount = ins.Feedback
    End Sub
    Public Sub EffectSetup(ByRef note As Note, status As ChannelStatus)
        Select Case note.EffectLetter
            Case "0" ' arpeggio
                Dim data1 = note.EffectData And &HF
                Dim data2 = (note.EffectData And &HF0) >> 4
                status.ArpeggioPhase = 0
                If data1 = 0 AndAlso data2 = 0 Then
                    status.ArpeggioActive = False
                    status.ArpeggioNote1 = 0
                    status.ArpeggioNote2 = 0
                    status.ArpeggioBaseNote = ""
                    Exit Select
                End If
                status.ArpeggioActive = True
                status.ArpeggioBaseNote = note.NoteStr
                status.ArpeggioNote1 = data2
                status.ArpeggioNote2 = data1
            Case "1" ' pitch slide up
                status.PitchSlideActive = note.EffectData <> 0
                status.PitchSlideSpeed = note.EffectData
            Case "2" ' pitch slide down
                status.PitchSlideActive = note.EffectData <> 0
                status.PitchSlideSpeed = -note.EffectData
            Case "4"
                Dim vibdepth = note.EffectData And &HF
                Dim vibspeed = (note.EffectData And &HF0) >> 4
                status.VibratoPhase = 0
                If note.EffectData = 0 Then
                    status.VibratoActive = False
                    status.VibratoDepth = 0
                    status.VibratoSpeed = 0
                Else
                    status.VibratoActive = True
                    status.VibratoDepth = vibdepth
                    status.VibratoSpeed = vibspeed
                End If
            Case "L" ' legato
                status.LegatoActive = note.EffectData > 0
            Case "R"
                status.DontStartEnvelope = note.EffectData > 0
        End Select
    End Sub
    Public Sub EffectProcess(ch As FMSynthProvider, status As ChannelStatus)
        If status.ArpeggioActive Then
            Dim pitch As Double = Note.GetNoteFrequency(status.ArpeggioBaseNote)
            If status.ArpeggioPhase = 1 Then pitch = Note.GetNoteFrequency(status.ArpeggioBaseNote, status.ArpeggioNote1)
            If status.ArpeggioPhase = 2 Then pitch = Note.GetNoteFrequency(status.ArpeggioBaseNote, status.ArpeggioNote2)
            ch.CarrierFrequency = pitch
            ch.ModulatorFrequency = pitch
            status.ArpeggioPhase = (status.ArpeggioPhase + 1) Mod 3
        End If
        If status.PitchSlideActive Then
            ch.CarrierFrequency += status.PitchSlideSpeed
            ch.ModulatorFrequency += status.PitchSlideSpeed
        End If
        If status.VibratoActive Then
            status.VibratoPhase += status.VibratoSpeed / 10
            status.VibratoPhase = status.VibratoPhase Mod (Math.PI * 2)
            Dim val = Math.Sin(status.VibratoPhase)
            ch.CarrierFrequency = status.OriginalFrequency + val * (status.VibratoDepth * 2)
            ch.ModulatorFrequency = status.OriginalFrequency + val * (status.VibratoDepth * 2)
        End If
    End Sub
    Public Sub NewNote(ch As FMSynthProvider, status As ChannelStatus, ptns As List(Of Note()))
        ' process channel 1
        Dim note = ptns(CurrentPattern)(CurrentPlayRow)
        If note.EffectLetter = "B" Then ' jump to ptn
            CurrentPattern = note.EffectData
            CurrentPlayRow = 0
            GoToNewPattern(CurrentPattern, True)
        End If
        If note.EffectLetter = "D" Then ' jump to next ptn
            CurrentPlayRow = 0
            CurrentPattern += 1
            If CurrentPattern = ptns.Count Then CurrentPattern = 0
            GoToNewPattern(CurrentPattern, True)
        End If
        EffectSetup(note, status)
        If note.InstrumentNum >= 0 Then
            ' new note
            SetChannelInstrument(ch, Instruments(note.InstrumentNum))
            ch.CarrierFrequency = note.Frequency
            ch.ModulatorFrequency = note.Frequency
            status.OriginalFrequency = note.Frequency
            If Not status.LegatoActive Then
                If status.DontStartEnvelope Then ch.StartNoteModulator() Else ch.StartNote()
                'ch.StartNote()
            End If
        End If
        If note.EffectLetter = "C" Then
            Dim vol = note.EffectData / 255
            ch.Volume = vol * 0.125
        End If
        If note.InstrumentNum = -2 Then ch.ReleaseNote() ' make a blank instrument or something for full stop
    End Sub
    Public Sub PlayTimer_Tick()
        Dispatcher.Invoke(
        Sub()

            WaitCounter += 1
            ' --------
            ' New note
            ' --------
            If WaitCounter = SpeedBox.Value Then
                NewNote(FMSynth1, ChannelStatus1, Patterns1)
                NewNote(FMSynth2, ChannelStatus2, Patterns2)
                NewNote(FMSynth3, ChannelStatus3, Patterns3)
                NewNote(FMSynth4, ChannelStatus4, Patterns4)
                NewNote(FMSynth5, ChannelStatus5, Patterns5)
                NewNote(FMSynth6, ChannelStatus6, Patterns6)
                NewNote(FMSynth7, ChannelStatus7, Patterns7)
                NewNote(FMSynth8, ChannelStatus8, Patterns8)
                PatternCh1.SelectedRow = CurrentPlayRow
                PatternCh2.SelectedRow = CurrentPlayRow
                PatternCh3.SelectedRow = CurrentPlayRow
                PatternCh4.SelectedRow = CurrentPlayRow
                PatternCh5.SelectedRow = CurrentPlayRow
                PatternCh6.SelectedRow = CurrentPlayRow
                PatternCh7.SelectedRow = CurrentPlayRow
                PatternCh8.SelectedRow = CurrentPlayRow
                CurrentPlayRow += 1
                If CurrentPlayRow = 64 Then
                    CurrentPlayRow = 0
                    If Not LoopCurrentPattern Then CurrentPattern += 1
                    If CurrentPattern = Patterns1.Count Then CurrentPattern = 0
                    GoToNewPattern(CurrentPattern, True)
                End If
                RefreshViews()
                WaitCounter = 0
            End If
            EffectProcess(FMSynth1, ChannelStatus1)
            EffectProcess(FMSynth2, ChannelStatus2)
            EffectProcess(FMSynth3, ChannelStatus3)
            EffectProcess(FMSynth4, ChannelStatus4)
            EffectProcess(FMSynth5, ChannelStatus5)
            EffectProcess(FMSynth6, ChannelStatus6)
            EffectProcess(FMSynth7, ChannelStatus7)
            EffectProcess(FMSynth8, ChannelStatus8)
        End Sub)
    End Sub
    Public Sub RefreshViews()
        PatternCh1.InvalidateVisual()
        PatternCh2.InvalidateVisual()
        PatternCh3.InvalidateVisual()
        PatternCh4.InvalidateVisual()
        PatternCh5.InvalidateVisual()
        PatternCh6.InvalidateVisual()
        PatternCh7.InvalidateVisual()
        PatternCh8.InvalidateVisual()
    End Sub
    Public Sub SetInstrument(idx As Integer)
        PatternCh1.SelectedInstrument = idx
        PatternCh2.SelectedInstrument = idx
        PatternCh3.SelectedInstrument = idx
        PatternCh4.SelectedInstrument = idx
        PatternCh5.SelectedInstrument = idx
        PatternCh6.SelectedInstrument = idx
        PatternCh7.SelectedInstrument = idx
        PatternCh8.SelectedInstrument = idx
    End Sub
    Public Sub SetOctave(idx As Integer)
        PatternCh1.SelectedOctave = idx
        PatternCh2.SelectedOctave = idx
        PatternCh3.SelectedOctave = idx
        PatternCh4.SelectedOctave = idx
        PatternCh5.SelectedOctave = idx
        PatternCh6.SelectedOctave = idx
        PatternCh7.SelectedOctave = idx
        PatternCh8.SelectedOctave = idx
    End Sub

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        Mixer.AddMixerInput(FMSynth1)
        Mixer.AddMixerInput(FMSynth2)
        Mixer.AddMixerInput(FMSynth3)
        Mixer.AddMixerInput(FMSynth4)
        Mixer.AddMixerInput(FMSynth5)
        Mixer.AddMixerInput(FMSynth6)
        Mixer.AddMixerInput(FMSynth7)
        Mixer.AddMixerInput(FMSynth8)
        wo.Init(Mixer)
        wo.Play()
        PatternCh1.ViewsToSync.AddRange({PatternCh2, PatternCh3, PatternCh4, PatternCh5, PatternCh6, PatternCh7, PatternCh8})
        PatternCh2.ViewsToSync.AddRange({PatternCh1, PatternCh3, PatternCh4, PatternCh5, PatternCh6, PatternCh7, PatternCh8})
        PatternCh3.ViewsToSync.AddRange({PatternCh1, PatternCh2, PatternCh4, PatternCh5, PatternCh6, PatternCh7, PatternCh8})
        PatternCh4.ViewsToSync.AddRange({PatternCh1, PatternCh2, PatternCh3, PatternCh5, PatternCh6, PatternCh7, PatternCh8})
        PatternCh5.ViewsToSync.AddRange({PatternCh1, PatternCh2, PatternCh3, PatternCh4, PatternCh6, PatternCh7, PatternCh8})
        PatternCh6.ViewsToSync.AddRange({PatternCh1, PatternCh2, PatternCh3, PatternCh4, PatternCh5, PatternCh7, PatternCh8})
        PatternCh7.ViewsToSync.AddRange({PatternCh1, PatternCh2, PatternCh3, PatternCh4, PatternCh5, PatternCh6, PatternCh8})
        PatternCh8.ViewsToSync.AddRange({PatternCh1, PatternCh2, PatternCh3, PatternCh4, PatternCh5, PatternCh6, PatternCh7})
        PatternCh1.SelectedSubCol = 0
        PatternCh1.InvalidateVisual()
        AddBlankPattern()
        GoToNewPattern(0, True)
        AddNewInstrument()
        UpdateInstrumentBox()
        InstrumentBox.SelectedIndex = 0
    End Sub
    Public Sub AddNewInstrument()
        Instruments.Add(New Instrument("", 0.5, 1, 1, 0, New ADSR(0, 0, 1, 0), New ADSR(0, 0.3, 0, 0)))
    End Sub
    ' returns index of new pattern
    Public Function AddBlankPattern() As Integer
        Dim NoteList(64) As Note
        For i = 0 To 63
            NoteList(i) = New Note("", 0)
        Next
        Patterns1.Add(NoteList.Clone)
        Patterns2.Add(NoteList.Clone)
        Patterns3.Add(NoteList.Clone)
        Patterns4.Add(NoteList.Clone)
        Patterns5.Add(NoteList.Clone)
        Patterns6.Add(NoteList.Clone)
        Patterns7.Add(NoteList.Clone)
        Patterns8.Add(NoteList.Clone)
        UpdatePatternBox()
        Return Patterns1.Count - 1
    End Function
    Public Sub RemovePattern(index As Integer)
        Patterns1.RemoveAt(index)
        Patterns2.RemoveAt(index)
        Patterns3.RemoveAt(index)
        Patterns4.RemoveAt(index)
        Patterns5.RemoveAt(index)
        Patterns6.RemoveAt(index)
        Patterns7.RemoveAt(index)
        Patterns8.RemoveAt(index)
        UpdatePatternBox()
    End Sub
    Public Function CopyPattern(originalIndex As Integer, copyIndex As Integer) As Integer
        Dim NoteList = Patterns1(originalIndex)
        Patterns1.Insert(copyIndex, NoteList.Clone)
        NoteList = Patterns2(originalIndex)
        Patterns2.Insert(copyIndex, NoteList.Clone)
        NoteList = Patterns3(originalIndex)
        Patterns3.Insert(copyIndex, NoteList.Clone)
        NoteList = Patterns4(originalIndex)
        Patterns4.Insert(copyIndex, NoteList.Clone)
        NoteList = Patterns5(originalIndex)
        Patterns5.Insert(copyIndex, NoteList.Clone)
        NoteList = Patterns6(originalIndex)
        Patterns6.Insert(copyIndex, NoteList.Clone)
        NoteList = Patterns7(originalIndex)
        Patterns7.Insert(copyIndex, NoteList.Clone)
        NoteList = Patterns8(originalIndex)
        Patterns8.Insert(copyIndex, NoteList.Clone)
        UpdatePatternBox()
        Return copyIndex
    End Function
    Public Function CopyPatternToEnd(originalIndex As Integer)
        Return CopyPattern(originalIndex, Patterns1.Count - 1)
    End Function
    Public Sub GoToNewPattern(index As Integer, Optional SetPatternBox As Boolean = False, Optional SaveOldPattern As Boolean = True)
        ' save old pattern
        If SaveOldPattern Then
            Patterns1(PreviousSelectedPattern) = PatternCh1.Pattern.Clone
            Patterns2(PreviousSelectedPattern) = PatternCh2.Pattern.Clone
            Patterns3(PreviousSelectedPattern) = PatternCh3.Pattern.Clone
            Patterns4(PreviousSelectedPattern) = PatternCh4.Pattern.Clone
            Patterns5(PreviousSelectedPattern) = PatternCh5.Pattern.Clone
            Patterns6(PreviousSelectedPattern) = PatternCh6.Pattern.Clone
            Patterns7(PreviousSelectedPattern) = PatternCh7.Pattern.Clone
            Patterns8(PreviousSelectedPattern) = PatternCh8.Pattern.Clone
        End If

        ' set new pattern
        PatternCh1.Pattern = Patterns1(index)
        PatternCh2.Pattern = Patterns2(index)
        PatternCh3.Pattern = Patterns3(index)
        PatternCh4.Pattern = Patterns4(index)
        PatternCh5.Pattern = Patterns5(index)
        PatternCh6.Pattern = Patterns6(index)
        PatternCh7.Pattern = Patterns7(index)
        PatternCh8.Pattern = Patterns8(index)
        CurrentPattern = index
        RefreshViews()
        PreviousSelectedPattern = index

        If SetPatternBox Then PatternBox.SelectedIndex = index
    End Sub
    Public Sub UpdatePatternBox()
        PatternBox.Items.Clear()
        For i = 0 To Patterns1.Count - 1 : PatternBox.Items.Add(i) : Next
    End Sub
    Public Sub UpdateInstrumentBox()
        Dim olindex = InstrumentBox.SelectedIndex
        InstrumentBox.Items.Clear()
        For i = 0 To Instruments.Count - 1 : InstrumentBox.Items.Add($"{i}: {Instruments(i).Name}") : Next
        InstrumentBox.SelectedIndex = Math.Min(Instruments.Count - 1, olindex)
    End Sub
    Private Sub PatternBox_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) _
    Handles PatternBox.SelectionChanged
        If PatternBox.SelectedIndex < 0 Then Exit Sub
        GoToNewPattern(PatternBox.SelectedIndex)
    End Sub
    Private Sub AddPatternButton_Click(sender As Object, e As RoutedEventArgs) Handles AddPatternButton.Click
        RemoveHandler PatternBox.SelectionChanged, AddressOf PatternBox_SelectionChanged
        GoToNewPattern(AddBlankPattern(), True)
        AddHandler PatternBox.SelectionChanged, AddressOf PatternBox_SelectionChanged
        e.Handled = True
    End Sub
    Private Sub RemovePatternButton_Click(sender As Object, e As RoutedEventArgs) Handles RemovePatternButton.Click
        RemoveHandler PatternBox.SelectionChanged, AddressOf PatternBox_SelectionChanged
        RemovePattern(PatternBox.SelectedIndex)
        GoToNewPattern(Clamp(PatternBox.SelectedIndex, 0, Patterns1.Count - 1), True, False)
        AddHandler PatternBox.SelectionChanged, AddressOf PatternBox_SelectionChanged
    End Sub
    Private Sub CopyPatternButton_Click(sender As Object, e As RoutedEventArgs) Handles CopyPatternButton.Click
        CopyPatternToEnd(PatternBox.SelectedIndex)
    End Sub
    Private Sub EditCheckBox_Checked(sender As Object, e As RoutedEventArgs) Handles EditCheckBox.Checked
        If PatternCh1 Is Nothing Then Exit Sub
        PatternCh1.Focusable = True
        PatternCh2.Focusable = True
        PatternCh3.Focusable = True
        PatternCh4.Focusable = True
        PatternCh5.Focusable = True
        PatternCh6.Focusable = True
        PatternCh7.Focusable = True
        PatternCh8.Focusable = True
    End Sub
    Private Sub EditCheckBox_Unchecked(sender As Object, e As RoutedEventArgs) Handles EditCheckBox.Unchecked
        PatternCh1.Focusable = False
        PatternCh2.Focusable = False
        PatternCh3.Focusable = False
        PatternCh4.Focusable = False
        PatternCh5.Focusable = False
        PatternCh6.Focusable = False
        PatternCh7.Focusable = False
        PatternCh8.Focusable = False
    End Sub
    Private Sub InstrumentBox_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs) Handles InstrumentBox.MouseDoubleClick
        editor = New InstrumentEditor
        editor.InstrumentsList = Me.Instruments
        editor.SelectedInstrument = InstrumentBox.SelectedIndex
        editor.FMSynth = FMSynth1
        AddHandler editor.Closed, Sub() UpdateInstrumentBox()
        editor.Show()
        editor.UpdateUI()
    End Sub
    Private Sub InstrumentBox_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles InstrumentBox.SelectionChanged
        SetInstrument(InstrumentBox.SelectedIndex)
    End Sub
    Private Sub AddInstrumentButton_Click(sender As Object, e As RoutedEventArgs) Handles AddInstrumentButton.Click
        AddNewInstrument()
        UpdateInstrumentBox()
    End Sub
    Private Sub RemoveInstrumentButton_Click(sender As Object, e As RoutedEventArgs) Handles RemoveInstrumentButton.Click
        Instruments.RemoveAt(InstrumentBox.SelectedIndex)
        UpdateInstrumentBox()
    End Sub
    Private Sub CopyInstrumentButton_Click(sender As Object, e As RoutedEventArgs) Handles CopyInstrumentButton.Click
        Instruments.Add(Instruments(InstrumentBox.SelectedIndex).Clone)
        UpdateInstrumentBox()
    End Sub

    Private Sub OctaveBox_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Integer))
        If PatternCh1 Is Nothing Then Exit Sub
        SetOctave(e.NewValue)
    End Sub

    Private Sub Window_Closing(sender As Object, e As ComponentModel.CancelEventArgs)
        editor.Close()
        Application.Current.Shutdown()
    End Sub

    Private Sub PlayStopButton_Click(sender As Object, e As RoutedEventArgs) Handles PlayStopButton.Click
        If IsPlaying Then
            PlayTimer.Stop()
            FMSynth1.StopNote()
            FMSynth2.StopNote()
            FMSynth3.StopNote()
            FMSynth4.StopNote()
            FMSynth5.StopNote()
            FMSynth6.StopNote()
            FMSynth7.StopNote()
            FMSynth8.StopNote()
            ChannelStatus1.Reset()
            ChannelStatus2.Reset()
            ChannelStatus3.Reset()
            ChannelStatus4.Reset()
            ChannelStatus5.Reset()
            ChannelStatus6.Reset()
            ChannelStatus7.Reset()
            ChannelStatus8.Reset()
            IsPlaying = False
            PlayStopButton.Content = ""
        Else
            CurrentPlayRow = 0
            WaitCounter = SpeedBox.Value - 1
            PlayTimer.Start()
            IsPlaying = True
            PlayStopButton.Content = ""
        End If
    End Sub

    Private Sub LoopPatternButton_Click(sender As Object, e As RoutedEventArgs) Handles LoopPatternButton.Click
        LoopCurrentPattern = Not LoopCurrentPattern
        If LoopCurrentPattern Then
            LoopPatternButton.Background = New SolidColorBrush(Color.FromArgb(255, 32, 128, 32))
        Else
            LoopPatternButton.Background = New SolidColorBrush(Color.FromArgb(255, 32, 32, 32))
        End If
    End Sub


    Private Sub Window_PreviewKeyDown(sender As Object, e As KeyEventArgs)
        If e.IsRepeat Then Exit Sub
        If e.Key = Key.Escape Then
            FMSynth1.StopNote()
            FMSynth2.StopNote()
            FMSynth3.StopNote()
            FMSynth4.StopNote()
            FMSynth5.StopNote()
            FMSynth6.StopNote()
            FMSynth7.StopNote()
            FMSynth8.StopNote()
            ChannelStatus1.Reset()
            ChannelStatus2.Reset()
            ChannelStatus3.Reset()
            ChannelStatus4.Reset()
            ChannelStatus5.Reset()
            ChannelStatus6.Reset()
            ChannelStatus7.Reset()
            ChannelStatus8.Reset()
        End If
        If PatternView.KeyNoteMap.ContainsKey(e.Key) Then
            MainWindow.SetChannelInstrument(FMSynth1, Instruments(InstrumentBox.SelectedIndex))
            Dim note As New Note($"{PatternView.KeyNoteMap(e.Key)}{OctaveBox.Value}", InstrumentBox.SelectedIndex)
            FMSynth1.CarrierFrequency = note.Frequency
            FMSynth1.ModulatorFrequency = note.Frequency
            FMSynth1.StartNote()
        End If
        If PatternView.KeyNoteMapHi.ContainsKey(e.Key) Then
            MainWindow.SetChannelInstrument(FMSynth1, Instruments(InstrumentBox.SelectedIndex))
            Dim note As New Note($"{PatternView.KeyNoteMapHi(e.Key)}{OctaveBox.Value + 1}", InstrumentBox.SelectedIndex)
            FMSynth1.CarrierFrequency = note.Frequency
            FMSynth1.ModulatorFrequency = note.Frequency
            FMSynth1.StartNote()
        End If
    End Sub

    Private Sub Window_PreviewKeyUp(sender As Object, e As KeyEventArgs)
        If PatternView.KeyNoteMap.ContainsKey(e.Key) Or PatternView.KeyNoteMapHi.ContainsKey(e.Key) Then
            FMSynth1.ReleaseNote()
        End If
    End Sub

    Private Sub MenuItem_Click(sender As Object, e As RoutedEventArgs) ' open song
        Dim dlg As New OpenFileDialog()

        dlg.Title = "Open song"
        dlg.Filter = "FM Tracker songs|*.fms|All files|*"
        dlg.InitialDirectory = Environment.CurrentDirectory
        dlg.Multiselect = False

        Dim result As Boolean? = dlg.ShowDialog()

        If result = True Then
            Dim deserialized As Song
            Using fs As FileStream = File.OpenRead(dlg.FileName)
                Using gz As New GZipStream(fs, CompressionMode.Decompress)
                    deserialized = JsonSerializer.Deserialize(Of Song)(gz)
                End Using
            End Using
            defines.CalculateNewNoteMap(deserialized.Tuning)
            TuningBox.Value = deserialized.Tuning
            Me.Instruments.Clear()
            Me.Patterns1.Clear()
            Me.Patterns2.Clear()
            Me.Patterns3.Clear()
            Me.Patterns4.Clear()
            Me.Patterns5.Clear()
            Me.Patterns6.Clear()
            Me.Instruments.AddRange(deserialized.Instruments)
            Me.Patterns1.AddRange(deserialized.Patterns1)
            Me.Patterns2.AddRange(deserialized.Patterns2)
            Me.Patterns3.AddRange(deserialized.Patterns3)
            Me.Patterns4.AddRange(deserialized.Patterns4)
            Me.Patterns5.AddRange(deserialized.Patterns5)
            Me.Patterns6.AddRange(deserialized.Patterns6)
            If deserialized.Patterns7 Is Nothing AndAlso deserialized.Patterns8 Is Nothing Then
                MsgBox("Converting 6-channel file into 8-channel", MsgBoxStyle.Information, "FM Tracker")
                Dim NoteList(64) As Note
                For i = 0 To 63
                    NoteList(i) = New Note("", 0)
                Next
                For i = 0 To Patterns1.Count - 1
                    Me.Patterns7.Add(NoteList.Clone)
                Next
                For i = 0 To Patterns1.Count - 1
                    Me.Patterns8.Add(NoteList.Clone)
                Next
            Else
                Me.Patterns7.AddRange(deserialized.Patterns7)
                Me.Patterns8.AddRange(deserialized.Patterns8)
            End If
            Me.SpeedBox.Value = deserialized.Speed
            Me.SongNameBox.Text = deserialized.Name
            Me.CurrentPattern = 0
            UpdateInstrumentBox()
            UpdatePatternBox()
            GoToNewPattern(0, True, False)
            PreviousSelectedPattern = 0
        End If
    End Sub

    Private Sub MenuItem_Click_1(sender As Object, e As RoutedEventArgs) ' save song
        Dim dlg As New SaveFileDialog()

        dlg.Title = "Save song"
        dlg.Filter = "FM Tracker songs|*.fms|All files|*"
        dlg.InitialDirectory = Environment.CurrentDirectory

        Dim result As Boolean? = dlg.ShowDialog()
        If result = True Then
            ' save patterns
            Patterns1(PatternBox.SelectedIndex) = PatternCh1.Pattern.Clone
            Patterns2(PatternBox.SelectedIndex) = PatternCh2.Pattern.Clone
            Patterns3(PatternBox.SelectedIndex) = PatternCh3.Pattern.Clone
            Patterns4(PatternBox.SelectedIndex) = PatternCh4.Pattern.Clone
            Patterns5(PatternBox.SelectedIndex) = PatternCh5.Pattern.Clone
            Patterns6(PatternBox.SelectedIndex) = PatternCh6.Pattern.Clone
            Patterns7(PatternBox.SelectedIndex) = PatternCh7.Pattern.Clone
            Patterns8(PatternBox.SelectedIndex) = PatternCh8.Pattern.Clone
            Dim newsong As New Song(
                SongNameBox.Text,
                SpeedBox.Value,
                Instruments.ToArray,
                Patterns1, Patterns2, Patterns3, Patterns4, Patterns5, Patterns6, Patterns7, Patterns8,
TuningBox.Value)
            'Dim serialized = JsonSerializer.Serialize(Of Song)(newsong)
            Using fs = File.Create(dlg.FileName)
                Using gz = New IO.Compression.GZipStream(fs, IO.Compression.CompressionLevel.Optimal)
                    System.Text.Json.JsonSerializer.Serialize(gz, newsong)
                End Using
            End Using
            'File.WriteAllText(dlg.FileName, serialized)
        End If

    End Sub

    Private Sub EditStepBox_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Integer))
        If PatternCh1 Is Nothing Then Exit Sub
        PatternCh1.EditStep = EditStepBox.Value
        PatternCh2.EditStep = EditStepBox.Value
        PatternCh3.EditStep = EditStepBox.Value
        PatternCh4.EditStep = EditStepBox.Value
        PatternCh5.EditStep = EditStepBox.Value
        PatternCh6.EditStep = EditStepBox.Value
        PatternCh7.EditStep = EditStepBox.Value
        PatternCh8.EditStep = EditStepBox.Value
    End Sub

    Private Sub TuningBox_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Integer))
        defines.CalculateNewNoteMap(e.NewValue)
    End Sub
End Class
