Imports System.Threading

Public Class Principal

    Dim threadSecundario As Threading.Thread
    Dim WithEvents opts As Opciones

    Private Sub OpcionesToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles opcionesToolStripMenuItem.Click
        Try
            threadSecundario.Abort()
            GC.Collect()
            GC.WaitForPendingFinalizers()
            GC.Collect()
        Catch ex As Exception
        End Try
        opts = New Opciones
        opts.Show()
    End Sub

    Private Sub Opciones_FormClosed(sender As Object, e As FormClosedEventArgs) Handles opts.FormClosed
        ReinicializaThread()
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ReinicializaThread()
    End Sub

    Private Sub Form1_Resize(sender As Object, e As EventArgs) Handles MyBase.Resize
        If My.Settings.minimizar Then
            If FormWindowState.Minimized = Me.WindowState Then
                NotifyIcon1.Visible = True
                NotifyIcon1.ShowBalloonTip(100, "Aplicación minimizada en bandeja del sistema", "Para restaurar, haga doble click sobre el icono.", ToolTipIcon.Info)
                Me.Hide()
            ElseIf FormWindowState.Normal = Me.WindowState Then
                NotifyIcon1.Visible = False
            End If
        End If
    End Sub

    Private Sub NotifyIcon1_MouseDoubleClick(sender As Object, e As MouseEventArgs) Handles NotifyIcon1.MouseDoubleClick
        Me.Show()
        Me.WindowState = FormWindowState.Normal
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles actualizarBtn.Click
        Try
            threadSecundario.Abort()
            GC.Collect()
            GC.WaitForPendingFinalizers()
            GC.Collect()
        Catch ex As Exception
        End Try
        Actualizar()
        ReinicializaThread()
    End Sub

    Private Sub Actualizar()
        Try
            If InvokeRequired Then
                Invoke(New MethodInvoker(Function()
                                             cuentaAtrasLbl.Text = "00:00:00"
                                             actualizarBtn.Enabled = False
                                             opcionesToolStripMenuItem.Enabled = False
                                             estadoLbl.Text = "Actualizando... "
                                         End Function))
            Else
                cuentaAtrasLbl.Text = "00:00:00"
                actualizarBtn.Enabled = False
                opcionesToolStripMenuItem.Enabled = False
                estadoLbl.Text = "Actualizando... "
            End If

            Dim planillasPorUnir As DataTable = Planillas.getPlanillasSinUnir
            For i As Integer = 0 To planillasPorUnir.Rows.Count - 1
                Dim row As DataRow = planillasPorUnir.Rows(i)
                Dim estadoPlanilla As String = "Uniendo planillas (" & (i + 1) & "/" & planillasPorUnir.Rows.Count & ")..."
                If InvokeRequired Then
                    Invoke(New MethodInvoker(Function()
                                                 estadoLbl.Text = estadoPlanilla
                                             End Function))
                Else
                    estadoLbl.Text = estadoPlanilla
                End If
                Dim planilla As String = row("colada") & "-" & row("nSerie")

                If GestorPDF.mergePDFPlanilla(row("idPieza"), planilla) Then
                    Planillas.updatePlanillaUnida(row("idPieza"))
                End If
            Next

            If InvokeRequired Then
                Invoke(New MethodInvoker(Function()
                                             actualizarBtn.Enabled = True
                                             opcionesToolStripMenuItem.Enabled = True
                                             estadoLbl.Text = "Actualizadas planillas con éxito!"
                                         End Function))
            Else
                actualizarBtn.Enabled = True
                opcionesToolStripMenuItem.Enabled = True
                estadoLbl.Text = "Actualizadas planillas con éxito!"
            End If
        Catch ex As Exception
            ' MessageBox.Show(ex.Message, "Error al actualizar", MessageBoxButtons.OK, MessageBoxIcon.Error)
            SimpleLog.logInfo("Error al actualizar: " & ex.Message, 1)
            If InvokeRequired Then
                Invoke(New MethodInvoker(Function()
                                             actualizarBtn.Enabled = True
                                             opcionesToolStripMenuItem.Enabled = True
                                             estadoLbl.Text = "No se han actualizado las planillas adecuadamente"
                                         End Function))
            Else
                actualizarBtn.Enabled = True
                opcionesToolStripMenuItem.Enabled = True
                estadoLbl.Text = "No se han actualizado las planillas adecuadamente"
            End If
        End Try
        GC.Collect()
        GC.WaitForPendingFinalizers()
        GC.Collect()
    End Sub

    Private Sub EsperaYActualiza()
        Try
            While True
                For secondsPassed = 0 To My.Settings.tiempoAct * 60
                    Dim timeToWait As TimeSpan = New TimeSpan(0, 0, (My.Settings.tiempoAct * 60) - secondsPassed)

                    If InvokeRequired Then
                        Invoke(New MethodInvoker(Function()
                                                     cuentaAtrasLbl.Text = String.Format("{0:00}:{1:00}:{2:00}", timeToWait.Hours, timeToWait.Minutes, timeToWait.Seconds)
                                                 End Function))
                    End If

                    Threading.Thread.Sleep(1000)
                Next

                Actualizar()
            End While
        Catch ex As Exception
        End Try
    End Sub

    Private Sub ReinicializaThread()
        threadSecundario = New Threading.Thread(AddressOf EsperaYActualiza)
        threadSecundario.IsBackground = True
        threadSecundario.Start()
    End Sub
End Class
