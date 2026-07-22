Public Class funciones
    Public Const connectionString As String = "Data Source=cafmiira.database.windows.net;Initial Catalog=bdCAF_MiiRA_A_R;User ID=cafmiira;Password=DJRK30ipsl9&21;Current Language=Spanish"

#Region "PROBARCONEXION"
    Public Shared Function KonexioaProbatu(Optional ByVal conex As String = connectionString) As Boolean
        Try
            Dim con As System.Data.SqlClient.SqlConnection
            Try
                con = New System.Data.SqlClient.SqlConnection
            Catch ex As Exception
                Return False
            End Try
            Try
                con.ConnectionString = conex
                con.Open()
                con.Close()
                con.Dispose()
                Return True
            Catch ex As Exception
                con.Close()
                con.Dispose()
                Return False
            End Try
            Return False
        Catch
            Return False
        End Try
    End Function
#End Region

#Region "SELECT"
    Public Shared Function cargar_todos(ByVal queryString As String, Optional ByVal conex As String = connectionString) As System.Data.DataTable
        Dim dbConnection As System.Data.IDbConnection = New System.Data.SqlClient.SqlConnection(conex)
        Dim dbCommand As System.Data.IDbCommand = New System.Data.SqlClient.SqlCommand

        Try
            dbCommand.CommandText = queryString
            dbCommand.Connection = dbConnection
            dbCommand.CommandTimeout = 300

            Dim dataAdapter As System.Data.IDbDataAdapter = New System.Data.SqlClient.SqlDataAdapter
            dataAdapter.SelectCommand = dbCommand
            Dim dataSet As System.Data.DataSet = New System.Data.DataSet
            Dim irten As Boolean = False
            While Not irten
                dataAdapter.Fill(dataSet)
                irten = True
            End While

            Return dataSet.Tables(0)
        Catch ex As Exception
            Return Nothing
        Finally
            dbConnection.Close()
        End Try
    End Function

    ' Igual que cargar_todos pero devuelve el DataSet COMPLETO (todas las tablas del batch).
    ' Necesario para get_info_SAP_ruedas_hierro_nSeries, cuyo SQL devuelve varias tablas (START/END, ASSEMBLY, VALORES, TIEMPOS, SERIALNO, NO EXPORTABLES).
    Public Shared Function cargar_dataset(ByVal queryString As String, Optional ByVal conex As String = connectionString) As System.Data.DataSet
        Dim dbConnection As System.Data.IDbConnection = New System.Data.SqlClient.SqlConnection(conex)
        Dim dbCommand As System.Data.IDbCommand = New System.Data.SqlClient.SqlCommand

        Try
            dbCommand.CommandText = queryString
            dbCommand.Connection = dbConnection
            dbCommand.CommandTimeout = 300

            Dim dataAdapter As System.Data.IDbDataAdapter = New System.Data.SqlClient.SqlDataAdapter
            dataAdapter.SelectCommand = dbCommand
            Dim dataSet As System.Data.DataSet = New System.Data.DataSet
            dataAdapter.Fill(dataSet)

            Return dataSet
        Catch ex As Exception
            Return Nothing
        Finally
            dbConnection.Close()
        End Try
    End Function
#End Region
#Region "UPDATE"
    Public Shared Function Update(ByVal queryString As String, Optional ByVal conex As String = connectionString) As Integer
        Dim dbConnection As System.Data.IDbConnection = New System.Data.SqlClient.SqlConnection(conex)
        Dim dbCommand As System.Data.IDbCommand = New System.Data.SqlClient.SqlCommand
        dbCommand.CommandText = queryString
        dbCommand.Connection = dbConnection
        Dim rowsAffected As Integer = 0
        Dim irten As Boolean = False
        While Not irten
            Try
                dbConnection.Open()
                irten = True
            Catch ex As Exception
                If ex.Message.Contains("Error relacionado con la red") Or ex.Message.Contains("Valor de tiempo") Or ex.Message.Contains("transporte") Then
                    Threading.Thread.Sleep(10000)
                Else
                    Threading.Thread.Sleep(10000)
                End If
            End Try
        End While
        Try
            rowsAffected = dbCommand.ExecuteNonQuery
            Return rowsAffected
        Catch ex As Exception
            Return -1
        Finally
            dbConnection.Close()
        End Try
    End Function
#End Region

#Region "Formatos"
    Public Shared Function dateToDBString2(ByVal data As DateTime) As String
        Dim month As String = data.Month
        Dim day As String = data.Day
        Dim hour As String = data.Hour
        Dim minute As String = data.Minute
        Dim second As String = data.Second

        If data.Month < 10 Then
            month = "0" & month
        End If
        If data.Day < 10 Then
            day = "0" & day
        End If
        If data.Hour < 10 Then
            hour = "0" & hour
        End If
        If data.Minute < 10 Then
            minute = "0" & minute
        End If
        If data.Second < 10 Then
            second = "0" & second
        End If
        Return data.Year & "-" & day & "-" & month & " " & hour & ":" & minute & ":" & second
    End Function
#End Region

End Class
