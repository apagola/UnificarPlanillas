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
End Class
