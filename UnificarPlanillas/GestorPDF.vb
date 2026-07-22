Imports System.IO
Imports System.Net
Imports System.Text.RegularExpressions
Imports CrystalDecisions.CrystalReports.Engine
Imports CrystalDecisions.Shared
Imports CrystalDecisions.ReportAppServer
Imports CrystalDecisions.ReportAppServer.ClientDoc
Imports CrystalDecisions.ReportAppServer.Controllers
Imports CrystalDecisions.ReportAppServer.ReportDefModel
Imports CrystalDecisions.ReportAppServer.CommonControls
Imports CrystalDecisions.ReportAppServer.CommLayer
Imports CrystalDecisions.ReportAppServer.CommonObjectModel
Imports CrystalDecisions.ReportAppServer.ObjectFactory
Imports CrystalDecisions.ReportAppServer.Prompting
Imports CrystalDecisions.ReportAppServer.DataSetConversion
Imports CrystalDecisions.ReportAppServer.DataDefModel
Imports CrystalDecisions.ReportSource
Imports CrystalDecisions.Windows.Forms
Imports CrystalDecisions.ReportAppServer.XmlSerialize
Imports iTextSharp.text
Imports iTextSharp.text.pdf

Public Class GestorPDF

    Public Shared pathTemporal As String = AppDomain.CurrentDomain.BaseDirectory & "\planillasBases"
    Public Shared pathTemporal2 As String = pathTemporal & "\planillasFTP"

    Private Shared ftpPath As String = "ftp://sgl.cafmiira.com/httpdocs/sgl/ficheros/Planos/"

    Private Shared ftpusername As String = "cafmiira"
    Private Shared ftppassword As String = "&zR6N@vmTgj2gvx2"

    Public Shared Function createPlanillaBasePDF(ByVal idPieza As String) As String
        Dim fileBase As String = ""
        If Not Directory.Exists(pathTemporal) Then
            Directory.CreateDirectory(pathTemporal)
        End If

        If Not Directory.Exists(pathTemporal2) Then
            Directory.CreateDirectory(pathTemporal2)
        End If

        Try
            Dim dt = Planillas.get_infoPieza(idPieza)
            Dim plano = "plano"
            Dim nSerie = "nSerie"
            If dt.Rows.Count > 0 Then
                plano = dt.Rows(0)("nombre")
                nSerie = dt.Rows(0)("nSerie")
            End If
            dt.Dispose()

            Dim dtRTP As DataTable = Planillas.get_planoRtpInterna_idPieza(idPieza) 'La base para el fichero
            Dim s As String = ""
            Dim fileDownload As String = ""
            If dtRTP.Rows.Count > 0 Then
                If dtRTP.Rows(0)("planillaInternaFicheroRPT") <> "" Then
                    s = ftpPath & dtRTP.Rows(0)("planillaInternaFicheroRPT")
                    Dim request As New WebClient()
                    request.Credentials = New NetworkCredential(ftpusername, ftppassword)
                    Dim bytes() As Byte = request.DownloadData(s)
                    Try
                        fileDownload = pathTemporal2 & "\" & dtRTP.Rows(0)("planillaInternaFicheroRPT")
                        Dim DownloadStream As FileStream = File.Create(fileDownload)

                        DownloadStream.Write(bytes, 0, bytes.Length)
                        DownloadStream.Close()
                    Catch ex As Exception
                        ' MessageBox.Show(ex.Message, "Error al descargar el fichero de la pieza", MessageBoxButtons.OK, MessageBoxIcon.Error)
                        SimpleLog.logInfo("Error al descargar el fichero de la pieza: " & ex.Message, 1)
                    End Try
                End If
            End If
            dtRTP.Dispose()

            If fileDownload <> "" Then
                Try
                    Dim crystalReport As ReportDocument
                    crystalReport = New ReportDocument()
                    crystalReport.Load(fileDownload)
                    ' ACTUALIZAR TODOS LOS VALORES DE CRYSTAL: primero datos base, luego histórico
                    ActualizarValoresCrystal_TODOS(CInt(idPieza))
                    Dim ldt As Data.DataTable = Planillas.crystal(idPieza) ' los datos a imprimir
                    'generar reporte
                    crystalReport.SetDataSource(ldt)
                    'se genera el PDF por pieza
                    ExportToPDF(crystalReport, pathTemporal & "/", plano & "-" & nSerie & ".pdf")
                    fileBase = pathTemporal & "/" & plano & "-" & nSerie & ".pdf"
                    File.Delete(fileDownload)
                    ldt.Dispose()
                Catch ex As Exception
                    ' MessageBox.Show(ex.Message, "Error al generar reporte de cristal", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    SimpleLog.logInfo("Error al generar reporte de cristal de " & plano & "-" & nSerie & ": " & ex.Message, 1)
                    Planillas.updatePlanillaUnida(idPieza)
                    SimpleLog.logInfo("Marcada como exportada la pieza con id " & idPieza & " después de localizar error en el .rpt", 1)
                End Try
            End If
        Catch ex As Exception
            ' MessageBox.Show(ex.Message, "Error al crear planilla base", MessageBoxButtons.OK, MessageBoxIcon.Error)
            SimpleLog.logInfo("Error al crear planilla base: " & ex.Message, 1)
        End Try

        Return fileBase
    End Function

    Protected Shared Function ExportToPDF(rpt As ReportDocument, ruta As String, NombreArchivo As String) As String
        Dim vFileName As String = Nothing
        Dim diskOpts As New DiskFileDestinationOptions()

        Try
            rpt.ExportOptions.ExportDestinationType = ExportDestinationType.DiskFile
            rpt.ExportOptions.ExportFormatType = ExportFormatType.PortableDocFormat

            'Este es la ruta donde se guardara tu archivo.

            vFileName = ruta & NombreArchivo
            If File.Exists(vFileName) Then
                File.Delete(vFileName)
            End If
            diskOpts.DiskFileName = vFileName
            rpt.ExportOptions.DestinationOptions = diskOpts
            rpt.Export()
            rpt.Dispose()
        Catch ex As Exception
            Throw ex
        End Try

        Return vFileName
    End Function

    Public Shared Function mergePDFPlanilla(ByVal idPieza As String, ByVal colada_numFile As String) As Boolean
        Dim existePDF As Boolean = False ' Flag para inicializar la creación del PDF si existe una de sus partes
        Dim reader As PdfReader = Nothing
        Dim pages As Integer = 0
        Dim sourceDocument As Document = Nothing
        Dim pdfCopyProvider As PdfCopy = Nothing
        Dim importedPage As PdfImportedPage
        Dim outputPdfPath As String = My.Settings.destino & "\" & colada_numFile & ".pdf"

        ' Creación y unión de fichero PDF base
        Dim fileBase As String = createPlanillaBasePDF(idPieza)

        If fileBase <> "" Then
            If File.Exists(outputPdfPath) Then
                File.Delete(outputPdfPath)
            End If
            sourceDocument = New Document()
            pdfCopyProvider = New PdfCopy(sourceDocument, New System.IO.FileStream(outputPdfPath, System.IO.FileMode.Create))
            sourceDocument.Open()
            pages = totalPageCount(fileBase)
            reader = New PdfReader(fileBase)
            For i As Integer = 1 To pages
                importedPage = pdfCopyProvider.GetImportedPage(reader, i)
                pdfCopyProvider.AddPage(importedPage)
            Next
            reader.Close()

            ' Unión de los demás ficheros PDFs (1 a 8)
            For orden As Integer = 1 To 8
                Dim origenes = My.Settings.origen.Split(";")
                For Each origen In origenes
                    Dim filePlanilla As String = origen & "\" & colada_numFile & "-" & orden & ".pdf"
                    If File.Exists(filePlanilla) Then
                        pages = totalPageCount(filePlanilla)
                        reader = New PdfReader(filePlanilla)

                        For i As Integer = 1 To pages
                            importedPage = pdfCopyProvider.GetImportedPage(reader, i)
                            pdfCopyProvider.AddPage(importedPage)
                        Next

                        reader.Close()
                    End If
                Next
            Next

            ' Unión de los ficheros PDFs posible (PD opcional, del 1 - 4)
            For nPD As Integer = 1 To 4
                Dim origenes = My.Settings.origen.Split(";")
                For Each origen In origenes
                    Dim filePlanillaPD As String = origen & "\" & colada_numFile & "PD-" & nPD & ".pdf"
                    If File.Exists(filePlanillaPD) Then
                        pages = totalPageCount(filePlanillaPD)
                        reader = New PdfReader(filePlanillaPD)

                        For i As Integer = 1 To pages
                            importedPage = pdfCopyProvider.GetImportedPage(reader, i)
                            pdfCopyProvider.AddPage(importedPage)
                        Next

                        reader.Close()
                    End If
                Next
            Next

            sourceDocument.Close()
            SimpleLog.logInfo("Creado documento en " & outputPdfPath)
            File.Delete(fileBase)
        End If

        Return fileBase <> ""
    End Function

    Public Shared Function totalPageCount(ByVal file As String) As Integer
        Using sr As StreamReader = New StreamReader(System.IO.File.OpenRead(file))
            Dim regex As Regex = New Regex("/Type\s*/Page[^s]")
            Dim matches As MatchCollection = regex.Matches(sr.ReadToEnd())
            Return matches.Count
        End Using
    End Function

    Public Shared Function ActualizarValoresCrystal_TODOS(ByVal idPieza As Integer) As Data.DataTable
        Try
            Dim s As New System.Text.StringBuilder

            s.AppendLine("		/* VARIABLES INICIALES */")
            s.AppendLine("		DECLARE @idPieza INT = " & idPieza)

            s.AppendLine("	UPDATE cry")
            s.AppendLine("	SET cry.NUMERO_CONSTRUCTOR = pins.nSerie")
            s.AppendLine("    FROM piezasNumerosSerie AS pins")
            s.AppendLine("    INNER JOIN piezas_Crystal AS cry ON cry.idPiezasNumerosSerie = pins.id")
            s.AppendLine("    WHERE cry.NUMERO_CONSTRUCTOR <> pins.nSerie")
            s.AppendLine("	UPDATE cry")
            s.AppendLine("	SET cry.NUMERO_CONSTRUCTOR = pins.nSerie")
            s.AppendLine("    FROM piezasNumerosSerie AS pins")
            s.AppendLine("    INNER JOIN piezas_Crystal_ejes AS cry ON cry.idPiezasNumerosSerie = pins.id")
            s.AppendLine("    WHERE cry.NUMERO_CONSTRUCTOR <> pins.nSerie")
            s.AppendLine("	UPDATE cry")
            s.AppendLine("	SET cry.NUMERO_CONSTRUCTOR = pins.nSerie")
            s.AppendLine("    FROM piezasNumerosSerie AS pins")
            s.AppendLine("    INNER JOIN piezas_Crystal_ruedas AS cry ON cry.idPiezasNumerosSerie = pins.id")
            s.AppendLine("    WHERE cry.NUMERO_CONSTRUCTOR <> pins.nSerie")
            s.AppendLine("	UPDATE cry")
            s.AppendLine("	SET cry.NUMERO_CLIENTE = pins.nSerieCliente")
            s.AppendLine("    FROM piezasNumerosSerie AS pins")
            s.AppendLine("    INNER JOIN piezas_Crystal AS cry ON cry.idPiezasNumerosSerie = pins.id")
            s.AppendLine("    WHERE cry.NUMERO_CLIENTE <> pins.nSerieCliente")
            s.AppendLine("  UPDATE cry")
            s.AppendLine("  SET cry.GMAO_CONJUNTOEJE = CONCAT(ISNULL(p.GMAOEjemontado, ''), TRY_CAST(pins.nSerieCliente AS INT))")
            s.AppendLine("    FROM piezasNumerosSerie AS pins")
            s.AppendLine("    INNER JOIN piezas_Crystal AS cry ON cry.idPiezasNumerosSerie = pins.id")
            s.AppendLine("    LEFT OUTER JOIN planos AS p ON p.id = pins.idPlano")
            s.AppendLine("    WHERE ISNULL(cry.GMAO_CONJUNTOEJE, '') <> CONCAT(ISNULL(p.GMAOEjemontado, ''), TRY_CAST(pins.nSerieCliente AS INT))")
            s.AppendLine("  	UPDATE cry")
            s.AppendLine("	SET cry.NUMERO_CLIENTE = pins.nSerieCliente")
            s.AppendLine("    FROM piezasNumerosSerie AS pins")
            s.AppendLine("    INNER JOIN piezas_Crystal_ejes AS cry ON cry.idPiezasNumerosSerie = pins.id")
            s.AppendLine("    WHERE cry.NUMERO_CLIENTE <> pins.nSerieCliente")
            s.AppendLine("	UPDATE cry")
            s.AppendLine("	SET cry.NUMERO_CLIENTE = pins.nSerieCliente")
            s.AppendLine("    FROM piezasNumerosSerie AS pins")
            s.AppendLine("    INNER JOIN piezas_Crystal_ruedas AS cry ON cry.idPiezasNumerosSerie = pins.id")
            s.AppendLine("    WHERE cry.NUMERO_CLIENTE <> pins.nSerieCliente")

            s.AppendLine("		DECLARE @dt TABLE (id INT NOT NULL PRIMARY KEY IDENTITY(1,1), nColumna VARCHAR(1000), valor VARCHAR(1000))")
            s.AppendLine("		DECLARE @tabla VARCHAR(500) = (SELECT TOP 1 CASE WHEN p.idAreaProductiva = 1 THEN 'piezas_Crystal_ejes'")
            s.AppendLine("														 WHEN p.idAreaProductiva = 13 THEN 'piezas_Crystal_ruedas'")
            s.AppendLine("														 ELSE 'piezas_Crystal'")
            s.AppendLine("													END AS tabla")
            s.AppendLine("										FROM piezasNumerosSerie AS pins")
            s.AppendLine("										INNER JOIN planos AS p ON p.id = pins.idPlano")
            s.AppendLine("										WHERE pins.id = @idPieza)")
            s.AppendLine("		/* CONSEGUIR LOS DATOS DEL HISTORICO */")
            s.AppendLine("		INSERT @dt")
            s.AppendLine("			SELECT prov.nColumna, hpv.valor ")
            s.AppendLine("			FROM historicoProduc AS hp")
            s.AppendLine("			INNER JOIN historicoProduc_valor AS hpv ON hpv.idHistoricoProduc = hp.id")
            s.AppendLine("			INNER JOIN planosRutaOperacionesValor AS prov ON prov.idRutaOperacion = hp.idOperacion AND prov.id = hpv.idPlanosRutaOperacionesValor")
            s.AppendLine("			WHERE prov.nColumna <> ''")
            s.AppendLine("			  AND hpv.valor <> ''")
            s.AppendLine("			  AND hp.cancelada = 0")
            s.AppendLine("			  AND hp.visibleEnHistorico = 1")
            s.AppendLine("			  AND hp.respuesta = 1")
            s.AppendLine("			  AND hp.idPieza = @idPieza")
            s.AppendLine("		UNION")
            s.AppendLine("			SELECT pro.nColumnaColadaOperacion, hp.colada")
            s.AppendLine("			FROM historicoProduc AS hp")
            s.AppendLine("			INNER JOIN planosRutasOperaciones AS pro ON pro.id = hp.idOperacion")
            s.AppendLine("			WHERE pro.nColumnaColadaOperacion <> ''")
            s.AppendLine("			  AND hp.colada <> ''")
            s.AppendLine("			  AND pro.solicitar_colada = 1")
            s.AppendLine("			  AND hp.cancelada = 0")
            s.AppendLine("			  AND hp.visibleEnHistorico = 1")
            s.AppendLine("			  AND hp.respuesta = 1")
            s.AppendLine("			  AND idPieza = @idPieza")
            s.AppendLine("		UNION ")
            s.AppendLine("			SELECT pro.nColumnaEdicionOperacion, hp.edicion")
            s.AppendLine("			FROM historicoProduc AS hp")
            s.AppendLine("			INNER JOIN planosRutasOperaciones AS pro ON pro.id = hp.idOperacion")
            s.AppendLine("			WHERE pro.nColumnaEdicionOperacion <> ''")
            s.AppendLine("			  AND hp.edicion <> ''")
            s.AppendLine("			  AND pro.solicitar_edicion = 1")
            s.AppendLine("			  AND hp.cancelada = 0")
            s.AppendLine("			  AND hp.visibleEnHistorico = 1")
            s.AppendLine("			  AND hp.respuesta = 1")
            s.AppendLine("			  AND idPieza = @idPieza")
            s.AppendLine("		UNION ")
            s.AppendLine("			SELECT pro.nColumnaFechaFabricacion, CONVERT(NVARCHAR, hp.fechaFab, 103) AS fechaFab")
            s.AppendLine("			FROM historicoProduc AS hp")
            s.AppendLine("			INNER JOIN planosRutasOperaciones AS pro ON pro.id = hp.idOperacion")
            s.AppendLine("			WHERE pro.nColumnaFechaFabricacion <> ''")
            s.AppendLine("			  AND hp.fechaFab <> ''")
            s.AppendLine("			  AND pro.solicitar_fechaFab = 1")
            s.AppendLine("			  AND hp.cancelada = 0")
            s.AppendLine("			  AND hp.visibleEnHistorico = 1")
            s.AppendLine("			  AND hp.respuesta = 1")
            s.AppendLine("			  AND idPieza = @idPieza")
            s.AppendLine("		UNION ")
            s.AppendLine("			SELECT pro.nColumnaFechaOperario, CONVERT(NVARCHAR, hp.inicio , 103) AS fecha")
            s.AppendLine("			FROM historicoProduc AS hp")
            s.AppendLine("			INNER JOIN planosRutasOperaciones AS pro ON pro.id = hp.idOperacion")
            s.AppendLine("			WHERE pro.nColumnaFechaOperario <> ''")
            s.AppendLine("			  AND pro.solicitar_operario = 1")
            s.AppendLine("			  AND hp.inicio <> ''")
            s.AppendLine("			  AND hp.cancelada = 0")
            s.AppendLine("			  AND idPieza = @idPieza")
            s.AppendLine("		UNION ")
            s.AppendLine("			SELECT pro.nColumnaMarcaOperacion, hp.marca")
            s.AppendLine("			FROM historicoProduc AS hp")
            s.AppendLine("			INNER JOIN planosRutasOperaciones AS pro ON pro.id = hp.idOperacion")
            s.AppendLine("			WHERE pro.nColumnaMarcaOperacion <> ''")
            s.AppendLine("			  AND pro.solicitar_marca = 1")
            s.AppendLine("			  AND hp.marca <> ''")
            s.AppendLine("			  AND hp.cancelada = 0")
            s.AppendLine("			  AND hp.visibleEnHistorico = 1")
            s.AppendLine("			  AND hp.respuesta = 1")
            s.AppendLine("			  AND idPieza = @idPieza")
            s.AppendLine("		UNION ")
            s.AppendLine("			SELECT pro.nColumnaPlanoOperacion, hp.plano")
            s.AppendLine("			FROM historicoProduc AS hp")
            s.AppendLine("			INNER JOIN planosRutasOperaciones AS pro ON pro.id = hp.idOperacion")
            s.AppendLine("			WHERE pro.nColumnaPlanoOperacion <> ''")
            s.AppendLine("			  AND pro.solicitar_plano = 1")
            s.AppendLine("			  AND hp.plano <> ''")
            s.AppendLine("			  AND hp.cancelada = 0")
            s.AppendLine("			  AND hp.visibleEnHistorico = 1")
            s.AppendLine("			  AND hp.respuesta = 1")
            s.AppendLine("			  AND idPieza = @idPieza")
            s.AppendLine("		UNION ")
            s.AppendLine("			SELECT pro.nColumnaNserieOperacion, hp.Nserie")
            s.AppendLine("			FROM historicoProduc AS hp")
            s.AppendLine("			INNER JOIN planosRutasOperaciones AS pro ON pro.id = hp.idOperacion")
            s.AppendLine("			WHERE pro.nColumnaNserieOperacion <> ''")
            s.AppendLine("			  AND hp.Nserie <> ''")
            s.AppendLine("			  AND pro.solicitar_n_Serie = 1")
            s.AppendLine("			  AND hp.cancelada = 0")
            s.AppendLine("			  AND hp.visibleEnHistorico = 1")
            s.AppendLine("			  AND hp.respuesta = 1")
            s.AppendLine("			  AND idPieza = @idPieza")
            s.AppendLine("		UNION ")
            s.AppendLine("			SELECT pro.nColumnaOperario, CONVERT(NVARCHAR, CASE WHEN @tabla = 'piezas_Crystal_ejes' THEN o.nombre ELSE CAST(o.codigo AS VARCHAR(1000)) END) AS operario")
            s.AppendLine("			FROM historicoProduc AS hp")
            s.AppendLine("			INNER JOIN planosRutasOperaciones AS pro ON pro.id = hp.idOperacion")
            s.AppendLine("			INNER JOIN operarios AS o ON o.id = hp.idOperario")
            s.AppendLine("			WHERE pro.nColumnaOperario <> ''")
            s.AppendLine("			  AND hp.operario <> ''")
            s.AppendLine("			  AND pro.solicitar_operario = 1")
            s.AppendLine("			  AND hp.cancelada = 0")
            s.AppendLine("			  AND hp.visibleEnHistorico = 1")
            s.AppendLine("			  AND hp.respuesta = 1")
            s.AppendLine("			  AND idPieza = @idPieza")
            s.AppendLine("		UNION ")
            s.AppendLine("			SELECT pro.nColumnaOrdenFabOperacion, hp.ordenFab")
            s.AppendLine("			FROM historicoProduc AS hp")
            s.AppendLine("			INNER JOIN planosRutasOperaciones AS pro ON pro.id = hp.idOperacion")
            s.AppendLine("			WHERE pro.nColumnaOrdenFabOperacion <> ''")
            s.AppendLine("			  AND hp.ordenFab <> ''")
            s.AppendLine("			  AND pro.solicitar_OF = 1")
            s.AppendLine("			  AND hp.cancelada = 0")
            s.AppendLine("			  AND hp.visibleEnHistorico = 1")
            s.AppendLine("			  AND hp.respuesta = 1")
            s.AppendLine("			  AND idPieza = @idPieza")
            s.AppendLine("		/* VARIABLES PARA EL LOOP */")
            s.AppendLine("        DECLARE @iIlara INT = 1")
            s.AppendLine("		DECLARE @Zenbat INT = (SELECT COUNT(id) FROM @dt)")
            s.AppendLine("		/* LOOP */")
            s.AppendLine("        WHILE (@iIlara < @Zenbat + 1)")
            s.AppendLine("        BEGIN")
            s.AppendLine("			/* VARIABLES POR CADA VALOR CON NCOLUMNA EN CRYSTAL */")
            s.AppendLine("        	DECLARE @Col VARCHAR(1000) = (SELECT nColumna FROM @dt WHERE id = @iIlara)")
            s.AppendLine("        	DECLARE @Val VARCHAR(1000) = (SELECT REPLACE(valor, ',', '.') FROM @dt WHERE id = @iIlara)")
            s.AppendLine("			/* CREAR EL UPDATE DE CRYSTAL */")
            s.AppendLine("       		DECLARE @SQLLAG VARCHAR(3000) = 'UPDATE ' + @tabla + ' SET [' + @Col + '] = ''' + @Val + ''' ' + ' WHERE (idPiezasNumerosSerie = ' + CAST(@idPieza as nvarchar) + ')'")
            s.AppendLine("			/* EJECUTAR EL UPDATE QUE HEMOS GENERADO */")
            s.AppendLine("       		EXEC(@SQLLAG)")
            s.AppendLine("			SET @iIlara += 1")
            s.AppendLine("        END")
            s.AppendLine("SELECT 'OK' AS resultado")

            Return funciones.cargar_todos(s.ToString())

        Catch ex As Exception
            SimpleLog.logInfo("Error en ActualizarValoresCrystal_TODOS para idPieza " & idPieza & ": " & ex.Message, 1)
            Return Nothing
        End Try
    End Function

End Class
