Public Class SimpleLog

    Public Shared logPath As String = AppDomain.CurrentDomain.BaseDirectory & "/Logs"

    Public Shared Sub logInfo(ByVal text As String, Optional ByVal mode As Integer = 0)
        Dim logFile As String = Now.ToString("ddMMyyyy") & "_Log.txt"

        text = Now.ToString & " " & IIf(mode = 0, "INFO", "ERROR") & " ==> " & text & vbCrLf

        If Not IO.Directory.Exists(logPath) Then
            IO.Directory.CreateDirectory(logPath)
        End If



        IO.File.AppendAllText(logPath & "/" & logFile, text)
    End Sub

End Class
