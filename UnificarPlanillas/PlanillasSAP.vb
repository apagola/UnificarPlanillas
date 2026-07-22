Imports System.IO

' =============================================================================
' Exportacion del JSON SAP de ruedas de hierro desde la miniapp de planillas.
'
' OPCION B (self-contained): la generacion del JSON y el acceso a BD se hacen con
'        codigo LOCAL (RuedasHierroSAP + capa 'funciones'), usando la connString de
'        PRODUCCION de la miniapp. NO se referencian las DLLs del web (AppCode/Funciones)
'        para no arrastrar Telerik/Crystal ni depender de la connString compilada en
'        AppCode.dll (modulo globales es Friend y apunta a la BD copia).
'        RuedasHierroSAP.vb es copia del codigo del web -> JSON byte a byte identico.
'
' Patron seguro reclamar -> escribir -> marcar: se obtiene el JSON SIN marcar exportado,
'        se escribe el fichero en la carpeta de SAP y SOLO si la escritura va bien se
'        marca *_exportado y se limpia generarJson. Asi un fallo de escritura no provoca
'        perdida silenciosa (la rueda se reintenta en la siguiente vuelta).
' =============================================================================
Public Class PlanillasSAP

    ' nSerie marcados desde la web con generarJson = 1 (la cola).
    Public Shared Function getRuedasPendientesJSON() As List(Of Integer)
        Return RuedasHierroSAP.getNSeriesPendientesGenerarJson()
    End Function

    ' Genera UN unico JSON por vuelta con todas las nSerie pendientes, SIN marcar exportado.
    Public Shared Function generarJSON(ByVal ids As List(Of Integer)) As String
        Return RuedasHierroSAP.crear_JSON_SAP_idHPns(ids, False, marcarExportado:=False)
    End Function

    ' Marca *_exportado = 1 (reusando el UPDATE de get_info) y limpia generarJson = 0.
    ' Llamar SOLO tras escribir el fichero en la carpeta de SAP correctamente.
    Public Shared Sub marcarExportadasYDesencolar(ByVal ids As List(Of Integer))
        ' get_info con marcarExportado:=True ejecuta el UPDATE de *_exportado; el DataSet devuelto se descarta.
        RuedasHierroSAP.get_info_SAP_ruedas_hierro_nSeries(ids, False, True)
        RuedasHierroSAP.Update_generarJson(ids, False)
    End Sub

    ' Limpia la cola sin marcar exportado (cuando el JSON sale vacio: no habia nada que exportar).
    Public Shared Sub desencolar(ByVal ids As List(Of Integer))
        RuedasHierroSAP.Update_generarJson(ids, False)
    End Sub

    ' Escribe el JSON en la carpeta de SAP. Devuelve la ruta del fichero escrito (lanza si falla).
    Public Shared Function escribirEnCarpetaSAP(ByVal carpetaSAP As String, ByVal jsonContent As String, ByVal nombreFichero As String) As String
        If String.IsNullOrWhiteSpace(carpetaSAP) Then
            Throw New Exception("La carpeta de SAP no esta configurada (Opciones).")
        End If
        If Not Directory.Exists(carpetaSAP) Then
            Throw New Exception("La carpeta de SAP no existe o no es accesible: " & carpetaSAP)
        End If
        Dim ruta As String = Path.Combine(carpetaSAP, nombreFichero)
        File.WriteAllText(ruta, jsonContent, New Text.UTF8Encoding(False))
        Return ruta
    End Function

End Class
