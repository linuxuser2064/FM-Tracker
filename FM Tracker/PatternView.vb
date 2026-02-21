Public Class PatternView
    Inherits Control
    Public Property ViewsToSync As New List(Of PatternView)
    Private scrollOffsetValue As Integer = 0
    Public Property ScrollOffset() As Integer
        Get
            Return scrollOffsetValue
        End Get
        Set(ByVal value As Integer)
            scrollOffsetValue = value
            Me.InvalidateVisual()
        End Set
    End Property

    Private showRowNumsValue As Boolean = False
    Public Property ShowRowNumbers() As Boolean
        Get
            Return showRowNumsValue
        End Get
        Set(ByVal value As Boolean)
            showRowNumsValue = value
            InvalidateVisual()
        End Set
    End Property
    Public Property SelectedOctave As Integer = 3
    Public Property SelectedInstrument As Integer = 0
    Public Property EditStep As Integer = 1
    '==============================
    ' Pattern storage
    '==============================
    Public Property Pattern As Note()
    Public Event PatternDataChanged(row As Integer, column As Integer)

    '==============================
    ' Selection
    '==============================
    Private selectedRowValue As Integer = 0
    Public Property SelectedRow() As Integer
        Get
            Return selectedRowValue
        End Get
        Set(value As Integer)
            selectedRowValue = Math.Max(0, Math.Min(RowCount - 1, value))
            EnsureRowVisible(value)
        End Set
    End Property
    Public Property SelectedSubCol As Integer = -1 ' 0=Note,1=Ins,2=Fx,3=FxData
    ' ==============================
    ' Block selection
    ' ==============================
    Private selectionStartRow As Integer = -1
    Private selectionEndRow As Integer = -1

    Private Function HasSelection() As Boolean
        Return selectionStartRow >= 0 AndAlso selectionEndRow >= 0 _
        AndAlso selectionStartRow <> selectionEndRow
    End Function

    Private Function SelectionMin() As Integer
        Return Math.Min(selectionStartRow, selectionEndRow)
    End Function

    Private Function SelectionMax() As Integer
        Return Math.Max(selectionStartRow, selectionEndRow)
    End Function
    Private ClipboardBuffer As List(Of Note) = Nothing

    '==============================
    ' Layout constants
    '==============================
    Private Const RowHeight As Integer = 16
    Private Const RowCount As Integer = 64

    Private Const ColNoteX As Integer = 4
    Private Const ColInsX As Integer = 34
    Private Const ColFxX As Integer = 64
    Private Const ColFxDataX As Integer = 74

    Private Const ColNoteWidth As Integer = 24
    Private Const ColInsWidth As Integer = 16
    Private Const ColFxWidth As Integer = 8
    Private Const ColFxDataWidth As Integer = 16

    '==============================
    ' Constructor
    '==============================
    Public Sub New()
        Me.Focusable = True
        Me.IsTabStop = True

        ' Initialize empty pattern
        ReDim Pattern(RowCount - 1)
        For i = 0 To RowCount - 1
            Pattern(i) = New Note("", 0) With {.EffectLetter = "."c, .EffectData = 0}
            'Pattern.Add(New Note(0) {})
            'Pattern(i)(0) = New Note("", 0) With {.EffectLetter = "."c, .EffectData = 0}
        Next
    End Sub
    Private Function GetColNoteX() As Integer
        Return ColNoteX + If(showRowNumsValue, 30, 3)
    End Function
    Private Function GetColInsX() As Integer
        Return ColInsX + If(showRowNumsValue, 30, 3)
    End Function
    Private Function GetColFxX() As Integer
        Return ColFxX + If(showRowNumsValue, 30, 3)
    End Function
    Private Function GetColFxDataX() As Integer
        Return ColFxDataX + If(showRowNumsValue, 30, 3)
    End Function
    '==============================
    ' Mouse selection
    '==============================
    Protected Overrides Sub OnMouseLeftButtonDown(e As MouseButtonEventArgs)
        MyBase.OnMouseLeftButtonDown(e)
        Me.Focus()

        Dim pos = e.GetPosition(Me)

        SelectedRow = Math.Max(0, Math.Min(RowCount - 1,
            Math.Floor((pos.Y - scrollOffsetValue) / RowHeight)))

        ' Column hit detection
        If pos.X >= GetColNoteX() AndAlso pos.X < GetColNoteX() + ColNoteWidth Then
            SelectedSubCol = 0
        ElseIf pos.X >= GetColInsX AndAlso pos.X < GetColInsX + ColInsWidth Then
            SelectedSubCol = 1
        ElseIf pos.X >= GetColFxX AndAlso pos.X < GetColFxX + ColFxWidth Then
            SelectedSubCol = 2
        ElseIf pos.X >= GetColFxDataX AndAlso pos.X < GetColFxDataX + ColFxDataWidth Then
            SelectedSubCol = 3
        End If
        EnteringSecondVal = False
        For Each x As PatternView In ViewsToSync
            x.SelectedRow = Me.SelectedRow
            x.SelectedSubCol = -1
            x.InvalidateVisual()
        Next
        e.Handled = True
        InvalidateVisual()
    End Sub
    Public Sub EnsureRowVisible(row As Integer)

        If row < 0 OrElse row >= RowCount Then Exit Sub
        If ActualHeight <= 0 Then Exit Sub

        Dim rowTop As Integer = row * RowHeight + scrollOffsetValue
        Dim rowBottom As Integer = rowTop + RowHeight

        ' If row is above visible area → scroll down
        If rowTop < 0 Then
            scrollOffsetValue -= rowTop
        End If

        ' If row is below visible area → scroll up
        If rowBottom > ActualHeight Then
            scrollOffsetValue -= (rowBottom - ActualHeight)
        End If

        ' Clamp to limits (same logic as mouse wheel)
        scrollOffsetValue = Math.Min(Me.ActualHeight / 2, scrollOffsetValue)
        scrollOffsetValue = Math.Max(-(RowCount * RowHeight - Me.ActualHeight / 2), scrollOffsetValue)

        ' Sync other views
        For Each x As PatternView In ViewsToSync
            x.scrollOffsetValue = scrollOffsetValue
            x.InvalidateVisual()
        Next

        InvalidateVisual()

    End Sub

    Public Shared ReadOnly KeyNoteMap As New Dictionary(Of Key, String) From {
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
    Public Shared ReadOnly KeyNoteMapHi As New Dictionary(Of Key, String) From {
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
    '==============================
    ' Keyboard navigation
    '==============================
    Private EnteringSecondVal As Boolean = False
    Private Function GetHexKeyValue(key As Key) As Integer?

        If key >= Key.D0 AndAlso key <= Key.D9 Then
            Return key - Key.D0
        End If

        If key >= Key.A AndAlso key <= Key.F Then
            Return key - Key.A + 10
        End If

        Return Nothing
    End Function
    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)
        e.Handled = True
        Select Case e.Key
            Case Key.Down
                If Keyboard.Modifiers = ModifierKeys.Shift Then
                    If selectionStartRow = -1 Then selectionStartRow = SelectedRow
                    SelectedRow = Math.Min(RowCount - 1, SelectedRow + 1)
                    selectionEndRow = SelectedRow
                Else
                    selectionStartRow = -1
                    selectionEndRow = -1
                    SelectedRow = Math.Min(RowCount - 1, SelectedRow + 1)
                End If
                EnteringSecondVal = False
            Case Key.PageDown
                If Keyboard.Modifiers = ModifierKeys.Shift Then
                    If selectionStartRow = -1 Then selectionStartRow = SelectedRow
                    SelectedRow = Math.Min(RowCount - 1, SelectedRow + 8)
                    selectionEndRow = SelectedRow
                Else
                    selectionStartRow = -1
                    selectionEndRow = -1
                    SelectedRow = Math.Min(RowCount - 1, SelectedRow + 8)
                End If
                EnteringSecondVal = False
            Case Key.Up
                If Keyboard.Modifiers = ModifierKeys.Shift Then
                    If selectionStartRow = -1 Then selectionStartRow = SelectedRow
                    SelectedRow = Math.Max(0, SelectedRow - 1)
                    selectionEndRow = SelectedRow
                Else
                    selectionStartRow = -1
                    selectionEndRow = -1
                    SelectedRow = Math.Max(0, SelectedRow - 1)
                End If
                EnteringSecondVal = False
            Case Key.PageUp
                If Keyboard.Modifiers = ModifierKeys.Shift Then
                    If selectionStartRow = -1 Then selectionStartRow = SelectedRow
                    SelectedRow = Math.Max(0, SelectedRow - 8)
                    selectionEndRow = SelectedRow
                Else
                    selectionStartRow = -1
                    selectionEndRow = -1
                    SelectedRow = Math.Max(0, SelectedRow - 8)
                End If
                EnteringSecondVal = False
            Case Key.Left
                SelectedSubCol = Math.Max(0, SelectedSubCol - 1)
                EnteringSecondVal = False
            Case Key.Right
                SelectedSubCol = Math.Min(3, SelectedSubCol + 1)
                EnteringSecondVal = False
            Case Key.Down, Key.Up, Key.Left, Key.Right, Key.PageDown, Key.PageUp
                For Each x As PatternView In ViewsToSync
                    x.SelectedRow = SelectedRow
                    x.InvalidateVisual()
                Next
        End Select

        If SelectedSubCol = 0 AndAlso Keyboard.Modifiers <> ModifierKeys.Control Then ' note
            If KeyNoteMap.ContainsKey(e.Key) Then
                Dim newNote As New Note($"{KeyNoteMap(e.Key)}{SelectedOctave}", SelectedInstrument, Pattern(SelectedRow).EffectLetter, Pattern(SelectedRow).EffectData)
                SetNoteValue(newNote)
                SelectedRow += EditStep
            End If
            If KeyNoteMapHi.ContainsKey(e.Key) Then
                Dim newNote As New Note($"{KeyNoteMapHi(e.Key)}{SelectedOctave + 1}", SelectedInstrument, Pattern(SelectedRow).EffectLetter, Pattern(SelectedRow).EffectData)
                SetNoteValue(newNote)
                SelectedRow += EditStep
            End If
            If e.Key = Key.D1 Then
                Dim newNote As New Note("OFF", -2, Pattern(SelectedRow).EffectLetter, Pattern(SelectedRow).EffectData)
                SetNoteValue(newNote)
                SelectedRow += EditStep
            End If
            If e.Key = Key.Delete Then
                Pattern(SelectedRow).NoteStr = "..."
                Pattern(SelectedRow).InstrumentNum = -1
            End If
            If e.Key = Key.Back Then
                For i = SelectedRow To RowCount - 2
                    Pattern(i) = Pattern(i + 1)
                Next
                Pattern(RowCount - 1) = New Note("...", -1)
            End If
        End If
        If SelectedSubCol = 1 Then ' instrument num
            If e.Key >= Key.D0 AndAlso e.Key <= Key.D9 Then
                Dim val As Integer = CInt(e.Key - Key.D0) ' funny trick commonly used in C string parsing
                If EnteringSecondVal Then
                    Pattern(SelectedRow).InstrumentNum *= 10
                    Pattern(SelectedRow).InstrumentNum += val
                    EnteringSecondVal = False
                    SelectedRow += EditStep
                Else
                    Pattern(SelectedRow).InstrumentNum = val
                    EnteringSecondVal = True
                End If
            End If
            If e.Key = Key.Delete Then
                Pattern(SelectedRow).InstrumentNum = -1
            End If
        End If
        If SelectedSubCol = 2 Then ' fx letter
            If e.Key >= Key.D0 AndAlso e.Key <= Key.D9 Then
                Dim letter = Chr(e.Key + 14)
                Pattern(SelectedRow).EffectLetter = letter
                SelectedRow += EditStep
            End If
            If e.Key >= Key.A AndAlso e.Key <= Key.Z Then
                Dim letter = Chr(e.Key + 21)
                Pattern(SelectedRow).EffectLetter = letter
                SelectedRow += EditStep
            End If
            If e.Key = Key.Delete Then
                Pattern(SelectedRow).EffectLetter = "."
                Pattern(SelectedRow).EffectData = 0
            End If
        End If
        If SelectedSubCol = 3 Then ' fx data
            Dim hexVal = GetHexKeyValue(e.Key)

            If hexVal.HasValue Then

                Dim row = SelectedRow   ' store original row
                Dim current = Pattern(row).EffectData

                If Not EnteringSecondVal Then
                    current = hexVal.Value
                    EnteringSecondVal = True
                Else
                    current = current * 16 + hexVal.Value
                    EnteringSecondVal = False
                End If

                Pattern(row).EffectData = current

                If Not EnteringSecondVal Then
                    SelectedRow += EditStep
                End If
            End If

            If e.Key = Key.Delete Then
                Pattern(SelectedRow).EffectLetter = "."
                Pattern(SelectedRow).EffectData = 0
            End If
        End If
        ' ==============================
        ' Copy
        ' ==============================
        If e.Key = Key.C AndAlso Keyboard.Modifiers = ModifierKeys.Control Then
            If HasSelection() Then
                ClipboardBuffer = New List(Of Note)
                For i = SelectionMin() To SelectionMax()
                    ClipboardBuffer.Add(Pattern(i).Clone())
                Next
                selectionStartRow = -1
                selectionEndRow = -1
            Else
                ClipboardBuffer = New List(Of Note)
                ClipboardBuffer.Add(Pattern(SelectedRow).Clone())
            End If
        End If

        ' ==============================
        ' Cut
        ' ==============================
        If e.Key = Key.X AndAlso Keyboard.Modifiers = ModifierKeys.Control Then
            If HasSelection() Then
                ClipboardBuffer = New List(Of Note)
                For i = SelectionMin() To SelectionMax()
                    ClipboardBuffer.Add(Pattern(i).Clone())
                    Pattern(i) = New Note("...", -1)
                Next
            Else
                ClipboardBuffer = New List(Of Note)
                ClipboardBuffer.Add(Pattern(SelectedRow).Clone())
                Pattern(SelectedRow) = New Note("...", -1)
            End If
        End If

        ' ==============================
        ' Paste
        ' ==============================
        If e.Key = Key.V AndAlso Keyboard.Modifiers = ModifierKeys.Control Then
            If ClipboardBuffer IsNot Nothing Then
                Dim pasteRow = SelectedRow
                For Each n In ClipboardBuffer
                    If pasteRow >= RowCount Then Exit For
                    Pattern(pasteRow) = n.Clone()
                    pasteRow += 1
                    SelectedRow = pasteRow
                Next
            End If
        End If
SkipIf:
        For Each x As PatternView In ViewsToSync
            x.SelectedRow = Me.SelectedRow
            x.SelectedSubCol = -1
            x.InvalidateVisual()
        Next
        InvalidateVisual()
    End Sub
    '==============================
    ' External note entry hook
    '==============================
    Public Sub SetNoteValue(note As Note)
        Pattern(SelectedRow) = note
        RaiseEvent PatternDataChanged(SelectedRow, SelectedSubCol)
        InvalidateVisual()
    End Sub
    Protected Overrides Sub OnMouseWheel(e As MouseWheelEventArgs)
        MyBase.OnMouseWheel(e)
        scrollOffsetValue += e.Delta / 3
        scrollOffsetValue = Math.Min(Me.ActualHeight / 2, scrollOffsetValue)
        scrollOffsetValue = Math.Max(-(RowCount * RowHeight - Me.ActualHeight / 2), scrollOffsetValue)
        Console.WriteLine(scrollOffsetValue)
        For Each x As PatternView In ViewsToSync
            x.scrollOffsetValue = scrollOffsetValue
            x.InvalidateVisual()
        Next
        InvalidateVisual()
    End Sub
    '==============================
    ' Rendering
    '==============================
    Protected Overrides Sub OnRender(dc As DrawingContext)

        dc.DrawRectangle(Brushes.Black, Nothing,
            New Rect(0, 0, ActualWidth, ActualHeight))

        Dim tf As New Typeface("Consolas")

        Dim firstVisibleRow As Integer = Math.Floor(-scrollOffsetValue / RowHeight)
        Dim lastVisibleRow As Integer = Math.Ceiling((-scrollOffsetValue + Me.ActualHeight) / RowHeight)

        ' Clamp to valid range
        firstVisibleRow = Math.Max(0, firstVisibleRow)
        lastVisibleRow = Math.Min(RowCount - 1, lastVisibleRow)

        For row = firstVisibleRow To lastVisibleRow

            Dim rowY = row * RowHeight + scrollOffsetValue

            ' Bar shading
            If row Mod 16 = 0 Then
                dc.DrawRectangle(
                    New SolidColorBrush(Color.FromArgb(60, 0, 128, 255)),
                    Nothing,
                    New Rect(0, rowY, ActualWidth, RowHeight))
            ElseIf row Mod 4 = 0 Then
                dc.DrawRectangle(
                    New SolidColorBrush(Color.FromArgb(25, 255, 255, 255)),
                    Nothing,
                    New Rect(0, rowY, ActualWidth, RowHeight))
            End If
            ' selection highlight
            If HasSelection() Then
                Dim selMin = SelectionMin()
                Dim selMax = SelectionMax()

                If row >= selMin AndAlso row <= selMax Then
                    dc.DrawRectangle(
            New SolidColorBrush(Color.FromArgb(40, 0, 255, 128)),
            Nothing,
            New Rect(0, rowY, ActualWidth, RowHeight))
                End If
            End If
            ' Row highlight
            If row = SelectedRow Then
                dc.DrawRectangle(
                    New SolidColorBrush(Color.FromArgb(60, 255, 0, 0)),
                    Nothing,
                    New Rect(0, rowY, ActualWidth, RowHeight))
                ' column highlight
                Select Case SelectedSubCol
                    Case 0
                        dc.DrawRectangle(
                            New SolidColorBrush(Color.FromArgb(255, 64, 64, 64)),
                            Nothing,
                            New Rect(GetColNoteX, rowY, ColNoteWidth, 16))
                    Case 1
                        dc.DrawRectangle(
                            New SolidColorBrush(Color.FromArgb(255, 64, 64, 64)),
                            Nothing,
                            New Rect(GetColInsX, rowY, ColInsWidth, 16))
                    Case 2
                        dc.DrawRectangle(
                            New SolidColorBrush(Color.FromArgb(255, 64, 64, 64)),
                            Nothing,
                            New Rect(GetColFxX, rowY, ColFxWidth, 16))
                    Case 3
                        dc.DrawRectangle(
                            New SolidColorBrush(Color.FromArgb(255, 64, 64, 64)),
                            Nothing,
                            New Rect(GetColFxDataX, rowY, ColFxDataWidth, 16))
                End Select
            End If
            ' Row number
            If showRowNumsValue Then
                Dim rowText = row.ToString("00")
                DrawText(dc, rowText, 5, rowY, tf, Brushes.Gray)
            End If

            ' Note data
            Dim note = Pattern(row)
            ' note
            DrawText(dc, note.NoteStr, GetColNoteX, rowY, tf,
                If(row = SelectedRow AndAlso SelectedSubCol = 0,
                   Brushes.Yellow, Brushes.White))
            ' instrument
            DrawText(dc, If(note.InstrumentNum >= 0,
                            note.InstrumentNum.ToString("00"), ".."),
                     GetColInsX, rowY, tf,
                     If(row = SelectedRow AndAlso SelectedSubCol = 1,
                        Brushes.Yellow, Brushes.White))
            ' fx letter
            DrawText(dc, note.EffectLetter.ToString(),
                     GetColFxX, rowY, tf,
                     If(row = SelectedRow AndAlso SelectedSubCol = 2,
                        Brushes.Yellow, Brushes.White))

            Dim fxDataStr = note.EffectData.ToString("X2")
            If note.EffectLetter = "."c Then fxDataStr = ".."
            ' fx data
            DrawText(dc, fxDataStr,
                     GetColFxDataX, rowY, tf,
                     If(row = SelectedRow AndAlso SelectedSubCol = 3,
                        Brushes.Yellow, Brushes.White))

        Next
        dc.DrawLine(New Pen(Brushes.DimGray, 1), New Point(0, 0), New Point(0, Me.ActualHeight - 1))
    End Sub

    Private Sub DrawText(dc As DrawingContext,
                         text As String,
                         x As Double,
                         y As Double,
                         tf As Typeface,
                         brush As Brush)

        Dim ft As New FormattedText(
            text,
            Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            tf,
            14,
            brush,
            1.0)

        dc.DrawText(ft, New Point(x, y))
    End Sub
End Class
