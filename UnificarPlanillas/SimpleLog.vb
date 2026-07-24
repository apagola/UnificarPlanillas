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

    ' ---------------------------------------------------------------------------------------------
    ' Traza unificada (mismo estilo que ErisEnlaceERP): fichero diario dedicado
    ' Logs/<nombreFichero>_log_yyyy-MM-dd.txt con lineas "dd/MM/yyyy HH:mm:ss ==> <msg>".
    ' La escritura de la traza nunca debe romper el proceso principal (Try/Catch mudo).
    ' ---------------------------------------------------------------------------------------------
    Public Const ANCHO_BANNER As Integer = 106

    Public Shared Sub WriteLog(ByVal nombreFichero As String, ByVal msg As String)
        Try
            If Not IO.Directory.Exists(logPath) Then
                IO.Directory.CreateDirectory(logPath)
            End If
            Dim ruta As String = logPath & "/" & nombreFichero & "_log_" & Now.ToString("yyyy-MM-dd") & ".txt"
            Dim linea As String = Now.ToShortDateString() & " " & Now.ToString("HH:mm:ss") & " ==> " & msg & vbCrLf
            IO.File.AppendAllText(ruta, linea)
        Catch
        End Try
    End Sub

    ' Linea solida de banner, p.ej. New String("*"c, ANCHO_BANNER).
    Public Shared Function BannerLinea(ByVal caracter As Char) As String
        Return New String(caracter, ANCHO_BANNER)
    End Function

    ' Titulo centrado entre marcadores, p.ej. "***            TEXTO            ***".
    Public Shared Function BannerTitulo(ByVal texto As String, ByVal marcador As String) As String
        Dim interior As Integer = ANCHO_BANNER - (marcador.Length * 2)
        Dim t As String = texto.Trim()
        If t.Length > interior Then
            t = t.Substring(0, interior)
        End If
        Dim relleno As Integer = interior - t.Length
        Dim izq As Integer = relleno \ 2
        Dim der As Integer = relleno - izq
        Return marcador & New String(" "c, izq) & t & New String(" "c, der) & marcador
    End Function

End Class
