Imports System.Text

' =============================================================================
' Traduccion de la parte de ATORNILLADO de la planilla Crystal de EJES MONTADOS.
' Mismo criterio que la web SGL (CAF_MiiRA_A_R\AppCode\Maestras\traduccionesInformes.vb:
' Get_IdiomasPieza / Traducir / Get_PlanillasPieza), copiado aqui para no referenciar
' AppCode.dll, igual que CrystalHistorico.
'
' >>> MANTENER SINCRONIZADO con el web <<<
'
' Diferencia con el web: en la web, si el cliente no tiene idiomas se imprime sin
' traducir. Aqui (decision CAF) solo se generan las planillas de los idiomas del
' cliente: todos los clientes tienen castellano, que hace de "sin traducir".
' Si un cliente no tiene idiomas (o no es de ejes montados) se sigue generando
' como siempre, sin traducir y con el nombre de siempre.
' =============================================================================
Public Class TraduccionesInformes

    ' Unica columna que se traduce: datosAtornilladoEgoki.Descripcion (subconsulta "b" de Planillas.crystal, Case 2)
    Public Const COLUMNA_TRADUCIBLE As String = "Descripcion"

    ''' <summary>
    ''' Una planilla a generar: los datos (traducidos si hay idioma) y el idioma ("" = sin traducir).
    ''' </summary>
    Public Class PlanillaCrystal
        Public idIdioma As Integer
        Public idioma As String
        Public datos As DataTable
        Public faltan As List(Of String)
    End Class

    ''' <summary>
    ''' Idiomas en los que hay que sacar la planilla de la pieza: los del cliente de su pedido
    ''' (pedido.cliente es texto: se busca en la maestra por nombre, prefiriendo el cliente de
    ''' la misma area que el comodin). Solo para planos de EJES MONTADOS (area 2).
    ''' </summary>
    Public Shared Function Get_IdiomasPieza(ByVal idPieza As Integer) As DataTable
        Dim s As New StringBuilder
        s.AppendLine("SELECT i.id, i.nombre")
        s.AppendLine("FROM idiomasInformes AS i")
        s.AppendLine("INNER JOIN clientesIdiomasInformes AS ci ON ci.idIdioma = i.id")
        s.AppendLine("WHERE ci.idCliente = (")
        s.AppendLine("    SELECT TOP 1 c.id")
        s.AppendLine("    FROM piezasNumerosSerie AS pins")
        s.AppendLine("    INNER JOIN planos AS pl ON pl.id = pins.idPlano AND pl.idAreaProductiva = 2")
        s.AppendLine("    INNER JOIN pedidoLineasPiezas AS plp ON plp.id = pins.idPedidoLineasPiezas")
        s.AppendLine("    LEFT OUTER JOIN pedidoLineas AS pel ON pel.id = plp.idPedidoLinea")
        s.AppendLine("    LEFT OUTER JOIN pedidoPlanos AS pp ON pp.id = plp.idPedidoPlano")
        s.AppendLine("    INNER JOIN pedido AS p ON p.id = ISNULL(pel.idPedido, pp.idPedido)")
        s.AppendLine("    INNER JOIN clientes AS c ON c.nombre = LTRIM(RTRIM(p.cliente))")
        s.AppendLine("        AND (c.idAreasProductivas = p.idAreaProductiva OR c.idAreasProductivas IS NULL)")
        s.AppendLine("    WHERE pins.id = " & idPieza)
        s.AppendLine("    ORDER BY CASE WHEN c.idAreasProductivas IS NULL THEN 1 ELSE 0 END, c.id")
        s.AppendLine(")")
        s.AppendLine("ORDER BY i.nombre ASC")
        Dim dt = funciones.cargar_todos(s.ToString())
        If dt Is Nothing Then
            dt = New DataTable()
            dt.Columns.Add("id", GetType(Integer))
            dt.Columns.Add("nombre", GetType(String))
        End If
        Return dt
    End Function

    ''' <summary>
    ''' Traduce la parte de atornillado de los datos de la planilla al idioma indicado.
    ''' Devuelve los textos que no tienen traduccion (se quedan como estaban).
    ''' </summary>
    Public Shared Function Traducir(ByVal dtCrystal As DataTable, ByVal idIdioma As Integer) As List(Of String)
        Dim faltan As New List(Of String)
        If dtCrystal Is Nothing OrElse Not dtCrystal.Columns.Contains(COLUMNA_TRADUCIBLE) Then
            Return faltan
        End If

        ' clave (sin distinguir mayusculas ni espacios de los extremos) -> texto traducido
        Dim textos As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        Dim dt = funciones.cargar_todos("SELECT t.clave, tt.texto FROM traduccionesInformes AS t " &
                                        "INNER JOIN traduccionesInformesTextos AS tt ON tt.idTraduccion = t.id " &
                                        "WHERE tt.idIdioma = " & idIdioma)
        If dt IsNot Nothing Then
            For Each row As DataRow In dt.Rows
                textos(row("clave").ToString().Trim()) = row("texto").ToString()
            Next
        End If

        Dim columna = dtCrystal.Columns(COLUMNA_TRADUCIBLE)
        columna.ReadOnly = False
        If columna.MaxLength > 0 Then columna.MaxLength = -1
        For Each row As DataRow In dtCrystal.Rows
            If IsDBNull(row(columna)) Then Continue For
            Dim original As String = row(columna).ToString().Trim()
            If original = "" Then Continue For
            If textos.ContainsKey(original) Then
                row(columna) = textos(original)
            ElseIf Not faltan.Contains(original) Then
                faltan.Add(original)
            End If
        Next
        Return faltan
    End Function

    ''' <summary>
    ''' Planillas de una pieza a partir de los datos de Planillas.crystal: una por cada idioma
    ''' de su cliente (cada una con su copia traducida), o una sin traducir si no tiene idiomas.
    ''' </summary>
    Public Shared Function Get_PlanillasPieza(ByVal idPieza As Integer, ByVal datos As DataTable) As List(Of PlanillaCrystal)
        Dim planillas As New List(Of PlanillaCrystal)
        Dim dtIdiomas = Get_IdiomasPieza(idPieza)
        If dtIdiomas.Rows.Count = 0 Then
            planillas.Add(New PlanillaCrystal With {.idIdioma = 0, .idioma = "", .datos = datos, .faltan = New List(Of String)})
            Return planillas
        End If
        For Each idioma As DataRow In dtIdiomas.Rows
            Dim copia = If(datos Is Nothing, Nothing, datos.Copy())
            Dim faltan = Traducir(copia, CInt(idioma("id")))
            planillas.Add(New PlanillaCrystal With {.idIdioma = CInt(idioma("id")), .idioma = idioma("nombre").ToString(), .datos = copia, .faltan = faltan})
        Next
        Return planillas
    End Function

    ''' <summary>
    ''' Nombre de fichero con el idioma al final: nombre_Idioma ("" = el nombre de siempre).
    ''' </summary>
    Public Shared Function NombreFichero(ByVal nombre As String, ByVal idioma As String) As String
        If String.IsNullOrEmpty(idioma) Then
            Return nombre
        End If
        Return nombre & "_" & idioma
    End Function

End Class
