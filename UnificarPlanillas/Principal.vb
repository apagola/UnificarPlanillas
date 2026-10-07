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

            ' ===== Segundo paso: JSON SAP de ruedas de hierro pendientes (generarJson = 1) =====
            ' Patron reclamar -> escribir -> marcar: si la escritura falla NO se marca nada (se reintenta).
            Try
                Dim idsRuedas As List(Of Integer) = PlanillasSAP.getRuedasPendientesJSON()
                If idsRuedas IsNot Nothing AndAlso idsRuedas.Count > 0 Then
                    ' ===== Traza unificada del "imprimir JSON SAP" (fichero dedicado, estilo ErisEnlaceERP) =====
                    ' Solo se traza cuando hay nSerie pendientes (evita spam de la vuelta del timer).
                    Dim inicioSAP As DateTime = Now
                    SimpleLog.WriteLog("trazaUnificada", SimpleLog.BannerLinea("*"c))
                    SimpleLog.WriteLog("trazaUnificada", SimpleLog.BannerTitulo("IMPRIMIR JSON SAP START", "***"))
                    SimpleLog.WriteLog("trazaUnificada", SimpleLog.BannerLinea("*"c))
                    SimpleLog.WriteLog("trazaUnificada", "1. JSON SAP RUEDAS")
                    SimpleLog.WriteLog("trazaUnificada", "    1.1. Reclamar cola (generarJson=1): " & idsRuedas.Count & " nSerie pendientes")

                    Dim estadoSAP As String = "Generando JSON SAP de ruedas (" & idsRuedas.Count & " nSerie)..."
                    If InvokeRequired Then
                        Invoke(New MethodInvoker(Function()
                                                     estadoLbl.Text = estadoSAP
                                                 End Function))
                    Else
                        estadoLbl.Text = estadoSAP
                    End If

                    Dim jsonSAP As String = PlanillasSAP.generarJSON(idsRuedas)
                    Dim resultadoSAP As String = "OK"

                    If String.IsNullOrEmpty(jsonSAP) OrElse jsonSAP = "[]" Then
                        ' Nada que exportar: no se escribe fichero vacio en la carpeta de SAP, se registra y se limpia la cola
                        SimpleLog.WriteLog("trazaUnificada", "    1.2. Generar JSON: 0 registros -> no se escribe fichero")
                        PlanillasSAP.desencolar(idsRuedas)
                        SimpleLog.WriteLog("trazaUnificada", "    1.3. Desencolar (sin exportar): " & idsRuedas.Count & " nSerie")
                        SimpleLog.logInfo("JSON SAP ruedas: 0 operaciones pendientes para " & idsRuedas.Count & " nSerie marcadas. No se escribe fichero.", 0)
                        resultadoSAP = "OK (vacio)"
                    Else
                        SimpleLog.WriteLog("trazaUnificada", "    1.2. Generar JSON: " & PlanillasSAP.contarRegistros(jsonSAP) & " registros")
                        Dim nombreFichero As String = "ruedasSAP_" & Now.ToString("yyyyMMdd_HHmmss") & ".json"
                        ' Escribir PRIMERO; solo si no lanza, marcar *_exportado y limpiar generarJson
                        Dim ruta As String = PlanillasSAP.escribirEnCarpetaSAP(My.Settings.carpetaSAP, jsonSAP, nombreFichero)
                        SimpleLog.WriteLog("trazaUnificada", "    1.3. Escribir fichero: " & nombreFichero)
                        PlanillasSAP.marcarExportadasYDesencolar(idsRuedas)
                        SimpleLog.WriteLog("trazaUnificada", "    1.4. Marcar exportadas y desencolar: " & idsRuedas.Count & " nSerie")
                        SimpleLog.logInfo("JSON SAP ruedas escrito en " & ruta & " (" & idsRuedas.Count & " nSerie).", 0)
                    End If

                    Dim durMs As Long = CLng((Now - inicioSAP).TotalMilliseconds)
                    SimpleLog.WriteLog("trazaUnificada", SimpleLog.BannerLinea("*"c))
                    SimpleLog.WriteLog("trazaUnificada", SimpleLog.BannerTitulo("IMPRIMIR JSON SAP END (" & resultadoSAP & ", " & durMs & " ms)", "***"))
                    SimpleLog.WriteLog("trazaUnificada", SimpleLog.BannerLinea("*"c))
                End If
            Catch exSAP As Exception
                ' Un fallo aqui NO marca exportado ni limpia la cola -> se reintenta en la siguiente vuelta
                SimpleLog.WriteLog("trazaUnificada", SimpleLog.BannerLinea("!"c))
                SimpleLog.WriteLog("trazaUnificada", SimpleLog.BannerTitulo("ERROR", "!!!"))
                SimpleLog.WriteLog("trazaUnificada", SimpleLog.BannerLinea("!"c))
                SimpleLog.WriteLog("trazaUnificada", exSAP.ToString())
                SimpleLog.logInfo("Error al generar/escribir el JSON SAP de ruedas: " & exSAP.Message, 1)
            End Try

            ' ===== Tercer paso: valores del historico a Crystal (una vez al dia, desde horaCrystal) =====
            ActualizarCrystal()

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

    ' Pasa a Crystal los valores del historico de las piezas con actividad desde la ultima pasada
    ' correcta (ultimaActCrystal). Se lanza una vez al dia, en la primera vuelta a partir de horaCrystal.
    ' Tiene su propio Try: un fallo aqui no afecta a las planillas ni al JSON SAP.
    Private Sub ActualizarCrystal()
        Try
            Dim inicioCry As DateTime = Now
            Dim horaHoy As DateTime = inicioCry.Date.AddHours(My.Settings.horaCrystal)
            Dim marca As DateTime = My.Settings.ultimaActCrystal
            ' Ya se hizo hoy (o aun no es la hora)
            If inicioCry < horaHoy OrElse marca >= horaHoy Then
                Return
            End If
            ' Primera vez (setting por defecto): desde hoy a las 00:00
            Dim desde As DateTime = If(marca.Year < 2001, inicioCry.Date, marca)

            Const TRAZA As String = "trazaUnificada"
            SimpleLog.WriteLog(TRAZA, SimpleLog.BannerLinea("*"c))
            SimpleLog.WriteLog(TRAZA, SimpleLog.BannerTitulo("VALORES CRYSTAL START", "***"))
            SimpleLog.WriteLog(TRAZA, SimpleLog.BannerLinea("*"c))
            SimpleLog.WriteLog(TRAZA, "1. VALORES DEL HISTORICO A CRYSTAL")
            SimpleLog.WriteLog(TRAZA, "    Desde: " & desde.ToString("dd/MM/yyyy HH:mm:ss") & If(marca.Year < 2001, " (primera vez: hoy 00:00)", " (ultima pasada correcta)") &
                               "   Hasta: " & inicioCry.ToString("dd/MM/yyyy HH:mm:ss") & "   horaCrystal: " & My.Settings.horaCrystal & ":00")
            MostrarEstado("Pasando valores a Crystal: buscando piezas...")

            Dim t As DateTime = Now
            Dim idsPiezas As List(Of Integer) = CrystalHistorico.getPiezasConActividad(desde)
            SimpleLog.WriteLog(TRAZA, "    1.1. Piezas con actividad: " & idsPiezas.Count & " (" & CLng((Now - t).TotalMilliseconds) & " ms)")

            t = Now
            MostrarEstado("Pasando valores a Crystal: repaso de numeros de serie...")
            CrystalHistorico.actualizarNumerosSerie()
            SimpleLog.WriteLog(TRAZA, "    1.2. Repaso de numeros de serie (todas las piezas): " & CLng((Now - t).TotalMilliseconds) & " ms")

            SimpleLog.WriteLog(TRAZA, "    1.3. Valores por pieza:")
            Dim errores As Integer = 0
            Dim tiempos As New List(Of KeyValuePair(Of Integer, Long))
            ' Mensaje de error -> piezas que lo dan (p.ej. "Invalid column name 'X'": nColumna mal configurado)
            Dim erroresPorMensaje As New Dictionary(Of String, List(Of Integer))
            Dim ancho As Integer = idsPiezas.Count.ToString().Length
            t = Now
            For i As Integer = 0 To idsPiezas.Count - 1
                Dim idPieza As Integer = idsPiezas(i)
                MostrarEstado("Pasando valores a Crystal (" & (i + 1) & "/" & idsPiezas.Count & ")...")
                Dim tPieza As DateTime = Now
                Dim mensajes As New List(Of String)
                Try
                    CrystalHistorico.actualizarPieza(idPieza)
                Catch exSql As SqlClient.SqlException
                    For Each e As SqlClient.SqlError In exSql.Errors
                        If Not mensajes.Contains(e.Message) Then mensajes.Add(e.Message)
                    Next
                Catch exPieza As Exception
                    mensajes.Add(exPieza.Message)
                End Try
                Dim msPieza As Long = CLng((Now - tPieza).TotalMilliseconds)
                tiempos.Add(New KeyValuePair(Of Integer, Long)(idPieza, msPieza))

                Dim cabecera As String = "        [" & (i + 1).ToString().PadLeft(ancho) & "/" & idsPiezas.Count & "] pieza " & idPieza.ToString().PadRight(8) &
                                         (If(mensajes.Count = 0, "OK   ", "ERROR")) & msPieza.ToString().PadLeft(7) & " ms"
                SimpleLog.WriteLog(TRAZA, cabecera)
                If mensajes.Count > 0 Then
                    errores += 1
                    For Each m As String In mensajes
                        SimpleLog.WriteLog(TRAZA, "            ! " & m)
                        If Not erroresPorMensaje.ContainsKey(m) Then erroresPorMensaje(m) = New List(Of Integer)
                        erroresPorMensaje(m).Add(idPieza)
                    Next
                    SimpleLog.logInfo("Error al pasar a Crystal la pieza " & idPieza & ": " & String.Join(" | ", mensajes), 1)
                End If
            Next
            Dim msPiezas As Long = CLng((Now - t).TotalMilliseconds)

            ' ----- Resumen -----
            SimpleLog.WriteLog(TRAZA, "    1.4. Resumen")
            SimpleLog.WriteLog(TRAZA, "        Piezas OK      : " & (idsPiezas.Count - errores) & " / " & idsPiezas.Count)
            SimpleLog.WriteLog(TRAZA, "        Piezas con error: " & errores)
            If idsPiezas.Count > 0 Then
                SimpleLog.WriteLog(TRAZA, "        Tiempo piezas  : " & TimeSpan.FromMilliseconds(msPiezas).ToString("hh\:mm\:ss") & " (media " & (msPiezas \ idsPiezas.Count) & " ms/pieza)")
                SimpleLog.WriteLog(TRAZA, "        Mas lentas     : " & String.Join(", ", tiempos.OrderByDescending(Function(kv) kv.Value).Take(5).Select(Function(kv) kv.Key & " (" & kv.Value & " ms)")))
            End If
            If erroresPorMensaje.Count > 0 Then
                SimpleLog.WriteLog(TRAZA, "        Errores agrupados (los valores validos de esas piezas SI se actualizan):")
                For Each kv In erroresPorMensaje.OrderByDescending(Function(x) x.Value.Count)
                    SimpleLog.WriteLog(TRAZA, "          - " & kv.Key & " -> " & kv.Value.Count & " pieza(s): " & String.Join(", ", kv.Value.Take(20)) & If(kv.Value.Count > 20, ", ...", ""))
                Next
            End If

            ' Solo si no ha habido excepcion general: la siguiente pasada parte de aqui
            My.Settings.ultimaActCrystal = inicioCry
            My.Settings.Save()
            SimpleLog.WriteLog(TRAZA, "    1.5. Marca ultimaActCrystal = " & inicioCry.ToString("dd/MM/yyyy HH:mm:ss"))
            SimpleLog.logInfo("Valores Crystal: " & idsPiezas.Count & " piezas, " & errores & " con error.", 0)

            Dim dur As TimeSpan = Now - inicioCry
            Dim resultadoCry As String = If(errores = 0, "OK", "OK con " & errores & " piezas con error")
            SimpleLog.WriteLog(TRAZA, SimpleLog.BannerLinea("*"c))
            SimpleLog.WriteLog(TRAZA, SimpleLog.BannerTitulo("VALORES CRYSTAL END (" & resultadoCry & ", " & dur.ToString("hh\:mm\:ss") & ")", "***"))
            SimpleLog.WriteLog("trazaUnificada", SimpleLog.BannerLinea("*"c))
        Catch exCry As Exception
            ' No se mueve la marca: la siguiente vuelta lo reintenta desde la misma fecha
            SimpleLog.WriteLog("trazaUnificada", SimpleLog.BannerLinea("!"c))
            SimpleLog.WriteLog("trazaUnificada", SimpleLog.BannerTitulo("ERROR VALORES CRYSTAL", "!!!"))
            SimpleLog.WriteLog("trazaUnificada", SimpleLog.BannerLinea("!"c))
            SimpleLog.WriteLog("trazaUnificada", exCry.ToString())
            SimpleLog.logInfo("Error al pasar los valores a Crystal: " & exCry.Message, 1)
        End Try
    End Sub

    Private Sub MostrarEstado(ByVal texto As String)
        If InvokeRequired Then
            Invoke(New MethodInvoker(Sub() estadoLbl.Text = texto))
        Else
            estadoLbl.Text = texto
        End If
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
