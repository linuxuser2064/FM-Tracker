Public Class InstrumentEditor
    Public Property InstrumentsList As List(Of Instrument)
    Public Property SelectedInstrument As Integer = 0
    Public Property FMSynth As FMSynthProvider
    Private ReadOnly KeyNoteMap As New Dictionary(Of Key, String) From {
    {Key.Z, "C-"},
    {Key.S, "C#"},
    {Key.X, "D-"},
    {Key.D, "D#"},
    {Key.C, "E-"},
    {Key.V, "F-"},
    {Key.G, "F#"},
    {Key.B, "G-"},
    {Key.H, "G#"},
    {Key.N, "A-"},
    {Key.J, "A#"},
    {Key.M, "B-"}
}
    Private ReadOnly KeyNoteMapHi As New Dictionary(Of Key, String) From {
    {Key.Q, "C-"},
    {Key.D2, "C#"},
    {Key.W, "D-"},
    {Key.D3, "D#"},
    {Key.E, "E-"},
    {Key.R, "F-"},
    {Key.D5, "F#"},
    {Key.T, "G-"},
    {Key.D6, "G#"},
    {Key.Y, "A-"},
    {Key.D7, "A#"},
    {Key.U, "B-"}
}
    Dim previewOctave As Integer = 3
    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        For i = 0 To InstrumentsList.Count - 1
            Dim x = InstrumentsList(i)
            InstrumentBox.Items.Add($"{i}: {x.Name}")
        Next
        UpdateUI()
    End Sub
    Public Sub UpdateUI()
        Dim ins = InstrumentsList(SelectedInstrument)
        FeedbackSlider.Value = ins.Feedback * 16
        MASlider.Value = ins.MAttack
        MDSlider.Value = ins.MDecay
        MSSlider.Value = ins.MSustain
        MRSlider.Value = ins.MRelease
        MTLSlider.Value = ins.MTL
        MMultiplierSlider.Value = ins.MMultiplier
        CASlider.Value = ins.CAttack
        CDSlider.Value = ins.CDecay
        CSSlider.Value = ins.CSustain
        CRSlider.Value = ins.CRelease
        CTLSlider.Value = ins.CTL
        CMultiplierSlider.Value = ins.CMultiplier
        InstrumentNameBox.Text = ins.Name
    End Sub
    Private Sub FeedbackSlider_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double))
        FeedbackLabel.Content = FeedbackSlider.Value / 16
        InstrumentsList(SelectedInstrument).Feedback = FeedbackSlider.Value / 16
    End Sub
    Private Sub MASlider_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double))
        MALabel.Text = Math.Round(MASlider.Value, 4)
        InstrumentsList(SelectedInstrument).MAttack = Math.Round(MASlider.Value, 4)
    End Sub
    Private Sub MDSlider_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double))
        MDLabel.Text = Math.Round(MDSlider.Value, 4)
        InstrumentsList(SelectedInstrument).MDecay = Math.Round(MDSlider.Value, 4)
    End Sub
    Private Sub MSSlider_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double))
        MSLabel.Text = Math.Round(MSSlider.Value, 4)
        InstrumentsList(SelectedInstrument).MSustain = Math.Round(MSSlider.Value, 4)
    End Sub
    Private Sub MRSlider_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double))
        MRLabel.Text = Math.Round(MRSlider.Value, 4)
        InstrumentsList(SelectedInstrument).MRelease = Math.Round(MRSlider.Value, 4)
    End Sub
    Private Sub MTLSlider_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double))
        MTLLabel.Text = Math.Round(MTLSlider.Value, 4)
        InstrumentsList(SelectedInstrument).MTL = Math.Round(MTLSlider.Value, 4)
    End Sub
    Private Sub CASlider_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double))
        CALabel.Text = Math.Round(CASlider.Value, 4)
        InstrumentsList(SelectedInstrument).CAttack = Math.Round(CASlider.Value, 4)
    End Sub
    Private Sub CDSlider_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double))
        CDLabel.Text = Math.Round(CDSlider.Value, 4)
        InstrumentsList(SelectedInstrument).CDecay = Math.Round(CDSlider.Value, 4)
    End Sub
    Private Sub CSSlider_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double))
        CSLabel.Text = Math.Round(CSSlider.Value, 4)
        InstrumentsList(SelectedInstrument).CSustain = Math.Round(CSSlider.Value, 4)
    End Sub
    Private Sub CRSlider_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double))
        CRLabel.Text = Math.Round(CRSlider.Value, 4)
        InstrumentsList(SelectedInstrument).CRelease = Math.Round(CRSlider.Value, 4)
    End Sub
    Private Sub CTLSlider_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double))
        CTLLabel.Text = Math.Round(CTLSlider.Value, 4)
        InstrumentsList(SelectedInstrument).CTL = Math.Round(CTLSlider.Value, 4)
    End Sub
    Private Sub InstrumentNameBox_TextChanged(sender As Object, e As TextChangedEventArgs) Handles InstrumentNameBox.TextChanged
        InstrumentsList(SelectedInstrument).Name = InstrumentNameBox.Text
    End Sub

    Private Sub CMultiplierSlider_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double))
        InstrumentsList(SelectedInstrument).CMultiplier = CMultiplierSlider.Value
    End Sub

    Private Sub MMultiplierSlider_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double))
        InstrumentsList(SelectedInstrument).MMultiplier = MMultiplierSlider.Value
    End Sub

    Private Sub Window_PreviewKeyDown(sender As Object, e As KeyEventArgs)
        If e.IsRepeat Then Exit Sub
        If e.Key = Key.OemComma Then previewOctave -= 1
        If e.Key = Key.OemPeriod Then previewOctave += 1
        If KeyNoteMap.ContainsKey(e.Key) Then
            MainWindow.SetChannelInstrument(FMSynth, InstrumentsList(SelectedInstrument))
            Dim note As New Note($"{KeyNoteMap(e.Key)}{previewOctave}", SelectedInstrument)
            FMSynth.CarrierFrequency = note.Frequency
            FMSynth.ModulatorFrequency = note.Frequency
            FMSynth.StartNote()
        End If
        If KeyNoteMapHi.ContainsKey(e.Key) Then
            MainWindow.SetChannelInstrument(FMSynth, InstrumentsList(SelectedInstrument))
            Dim note As New Note($"{KeyNoteMapHi(e.Key)}{previewOctave + 1}", SelectedInstrument)
            FMSynth.CarrierFrequency = note.Frequency
            FMSynth.ModulatorFrequency = note.Frequency
            FMSynth.StartNote()
        End If
    End Sub

    Private Sub Window_PreviewKeyUp(sender As Object, e As KeyEventArgs)
        FMSynth.ReleaseNote()
    End Sub

    Private Sub PitchMacroSlider_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double))
        InstrumentsList(SelectedInstrument).PitchMacro = e.NewValue
    End Sub
End Class
