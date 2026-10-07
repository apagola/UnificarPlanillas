Imports System.Text
Imports System.Data
Imports System.Data.SqlClient

' =============================================================================
' Paso nocturno: pasar a Crystal los valores del historico de produccion de las
' piezas con actividad. Es lo mismo que hace la web SGL con "Generar reporte" en
' Gestion de Calidad (historicoProduc.actualizarValoresCrystal_numerosSerie una
' vez + actualizarValoresCrystal_soloPieza por cada pieza), pero automatico.
'
' Igual que RuedasHierroSAP: se copia aqui el SQL de la web para NO referenciar
' AppCode.dll / Funciones.dll, y se ejecuta con la capa local 'funciones'
' (connString de PRODUCCION de la miniapp).
'
' >>> MANTENER SINCRONIZADO con el web <<<
'   - sqlValoresCrystal_numerosSerie     : CAF_MiiRA_A_R\AppCode\Maestras\historicoProduc.vb, region "Crystal" (~L1062)
'   - sqlValoresCrystal_pieza            : idem (~L1115)
'   - sqlValoresCrystal_operacionesPieza : idem (~L1289)
' Las tres funciones se copian tal cual (SQL logicamente identico). Unicos cambios:
'   las dos primeras son Public (para poder medirlas desde fuera), y se ejecutan
'   con funciones.ejecutar, que SI lanza la excepcion (BD.GetDatatable del web la
'   traga y devuelve Nothing) para poder registrar el error de cada pieza.
' =============================================================================
Public Class CrystalHistorico

    ' Piezas con actividad (inicio o fin) desde la marca indicada.
    Public Shared Function getPiezasConActividad(ByVal desde As DateTime) As List(Of Integer)
        Dim s As New StringBuilder
        s.AppendLine("SELECT DISTINCT hp.idPieza")
        s.AppendLine("FROM historicoProduc AS hp")
        s.AppendLine("WHERE hp.cancelada = 0")
        s.AppendLine("  AND hp.respuesta = 1")
        s.AppendLine("  AND hp.idPieza IS NOT NULL")
        s.AppendLine("  AND (hp.fin >= @desde OR hp.inicio >= @desde)")
        s.AppendLine("ORDER BY hp.idPieza")

        Dim ids As New List(Of Integer)
        Dim dt As DataTable = funciones.cargar_fecha(s.ToString(), "@desde", desde)
        For Each r As DataRow In dt.Rows
            ids.Add(CInt(r("idPieza")))
        Next
        Return ids
    End Function

    ' Repaso de numeros de serie de TODAS las piezas: una vez por pasada.
    Public Shared Sub actualizarNumerosSerie()
        funciones.ejecutar(sqlValoresCrystal_numerosSerie())
    End Sub

    ' Valores del historico de UNA pieza a su tabla de Crystal.
    Public Shared Sub actualizarPieza(ByVal idPieza As Integer)
        funciones.ejecutar(sqlValoresCrystal_pieza(idPieza))
    End Sub

#Region "SQL copiado del web (historicoProduc.vb, region Crystal)"
    Public Shared Function sqlValoresCrystal_numerosSerie() As String
        Dim s As New StringBuilder

        ' SOLUCION RAPIDA!
        ' Para solucionar el problema con los Nserie sin actualizar de los ejes y ruedas
        'cada vez que se actualiza se actualizan todos los datos de las piezas que no sean correctas, esta funcion es rapida a dia de hoy (04/10/2022)
        ' Como cuando solicitan datos como NSerie_CLIENTE en produccion, este update de datos basicos los machaca. Se haceia update de produccion y luego de datos basicos. ahora se ha dado la vuelta y se hace basico y luego produccion (2026/05/13)
        s.AppendLine("	UPDATE cry")
        s.AppendLine("	SET cry.NUMERO_CONSTRUCTOR = pins.nSerie -- SELECT * ")
        s.AppendLine("    FROM piezasNumerosSerie AS pins")
        s.AppendLine("    INNER JOIN piezas_Crystal AS cry ON cry.idPiezasNumerosSerie = pins.id")
        s.AppendLine("    WHERE cry.NUMERO_CONSTRUCTOR <> pins.nSerie")
        s.AppendLine("	UPDATE cry")
        s.AppendLine("	SET cry.NUMERO_CONSTRUCTOR = pins.nSerie -- SELECT * ")
        s.AppendLine("    FROM piezasNumerosSerie AS pins")
        s.AppendLine("    INNER JOIN piezas_Crystal_ejes AS cry ON cry.idPiezasNumerosSerie = pins.id")
        s.AppendLine("    WHERE cry.NUMERO_CONSTRUCTOR <> pins.nSerie")
        s.AppendLine("	UPDATE cry")
        s.AppendLine("	SET cry.NUMERO_CONSTRUCTOR = pins.nSerie -- SELECT * ")
        s.AppendLine("    FROM piezasNumerosSerie AS pins")
        s.AppendLine("    INNER JOIN piezas_Crystal_ruedas AS cry ON cry.idPiezasNumerosSerie = pins.id")
        s.AppendLine("    WHERE cry.NUMERO_CONSTRUCTOR <> pins.nSerie")
        s.AppendLine("	/*")
        s.AppendLine("	UPDATE cry")
        s.AppendLine("	SET cry.NUMERO_CONSTRUCTOR = pins.nSerie -- SELECT * ")
        s.AppendLine("    FROM piezasNumerosSerie AS pins")
        s.AppendLine("    INNER JOIN piezas_Crystal_Txindoki AS cry ON cry.idPiezasNumerosSerie = pins.id")
        s.AppendLine("    WHERE cry.NUMERO_CONSTRUCTOR <> pins.nSerie")
        s.AppendLine("  */")
        s.AppendLine("	UPDATE cry")
        s.AppendLine("	SET cry.NUMERO_CLIENTE = pins.nSerieCliente -- SELECT * ")
        s.AppendLine("    FROM piezasNumerosSerie AS pins")
        s.AppendLine("    INNER JOIN piezas_Crystal AS cry ON cry.idPiezasNumerosSerie = pins.id")
        s.AppendLine("    WHERE cry.NUMERO_CLIENTE <> pins.nSerieCliente")
        s.AppendLine("  UPDATE cry")
        s.AppendLine("  SET cry.GMAO_CONJUNTOEJE = CONCAT(ISNULL(p.GMAOEjemontado, ''), TRY_CAST(pins.nSerieCliente AS INT)) -- SELECT * ")
        s.AppendLine("    FROM piezasNumerosSerie AS pins")
        s.AppendLine("    INNER JOIN piezas_Crystal AS cry ON cry.idPiezasNumerosSerie = pins.id")
        s.AppendLine("    LEFT OUTER JOIN planos AS p ON p.id = pins.idPlano")
        s.AppendLine("    WHERE ISNULL(cry.GMAO_CONJUNTOEJE, '') <> CONCAT(ISNULL(p.GMAOEjemontado, ''), TRY_CAST(pins.nSerieCliente AS INT))")
        s.AppendLine("  	UPDATE cry")
        s.AppendLine("	SET cry.NUMERO_CLIENTE = pins.nSerieCliente -- SELECT * ")
        s.AppendLine("    FROM piezasNumerosSerie AS pins")
        s.AppendLine("    INNER JOIN piezas_Crystal_ejes AS cry ON cry.idPiezasNumerosSerie = pins.id")
        s.AppendLine("    WHERE cry.NUMERO_CLIENTE <> pins.nSerieCliente")
        s.AppendLine("	UPDATE cry")
        s.AppendLine("	SET cry.NUMERO_CLIENTE = pins.nSerieCliente -- SELECT * ")
        s.AppendLine("    FROM piezasNumerosSerie AS pins")
        s.AppendLine("    INNER JOIN piezas_Crystal_ruedas AS cry ON cry.idPiezasNumerosSerie = pins.id")
        s.AppendLine("    WHERE cry.NUMERO_CLIENTE <> pins.nSerieCliente")
        Return s.ToString()
    End Function

    Public Shared Function sqlValoresCrystal_pieza(ByVal idPieza As Integer) As String
        Dim s As New StringBuilder

        s.AppendLine("		/* VARIABLES INICIALES */")
        s.AppendLine("		DECLARE @idPieza INT = " & idPieza)
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
        s.AppendLine("			/* EL GRAN SELECT: en este select devolvemos TODAS las columnas de Crystal que tengan un valor en el historico y mas abajo estas se iran pasando 1 a 1 y se hara el update */")
        s.AppendLine("				SELECT prov.nColumna, hpv.valor ")
        s.AppendLine("				FROM historicoProduc AS hp")
        s.AppendLine("				INNER JOIN historicoProduc_valor AS hpv ON hpv.idHistoricoProduc = hp.id")
        ' La APK guarda historicoProduc_valor con idPlanosRutaOperacionesValor a NULL: se enlaza por orden, como en el resto de consultas
        s.AppendLine("				INNER JOIN planosRutaOperacionesValor AS prov ON prov.idRutaOperacion = hp.idOperacion AND ((hpv.orden = prov.orden AND hpv.idPlanosRutaOperacionesValor IS NULL) OR hpv.idPlanosRutaOperacionesValor = prov.id)")
        s.AppendLine("				WHERE prov.nColumna <> ''")
        s.AppendLine("				  AND hpv.valor <> ''")
        s.AppendLine("				  AND hp.cancelada = 0")
        s.AppendLine("				  AND hp.visibleEnHistorico = 1")
        s.AppendLine("				  AND hp.respuesta = 1")
        s.AppendLine("				  AND hp.idPieza = @idPieza")
        s.AppendLine("			UNION")
        s.AppendLine("				SELECT pro.nColumnaColadaOperacion, hp.colada")
        s.AppendLine("				FROM historicoProduc AS hp")
        s.AppendLine("				INNER JOIN planosRutasOperaciones AS pro ON pro.id = hp.idOperacion")
        s.AppendLine("				WHERE pro.nColumnaColadaOperacion <> ''")
        s.AppendLine("				  AND hp.colada <> ''")
        s.AppendLine("				  AND pro.solicitar_colada = 1")
        s.AppendLine("				  AND hp.cancelada = 0")
        s.AppendLine("				  AND hp.visibleEnHistorico = 1")
        s.AppendLine("				  AND hp.respuesta = 1")
        s.AppendLine("				  AND idPieza = @idPieza")
        s.AppendLine("			UNION ")
        s.AppendLine("				SELECT pro.nColumnaEdicionOperacion, hp.edicion")
        s.AppendLine("				FROM historicoProduc AS hp")
        s.AppendLine("				INNER JOIN planosRutasOperaciones AS pro ON pro.id = hp.idOperacion")
        s.AppendLine("				WHERE pro.nColumnaEdicionOperacion <> ''")
        s.AppendLine("				  AND hp.edicion <> ''")
        s.AppendLine("				  AND pro.solicitar_edicion = 1")
        s.AppendLine("				  AND hp.cancelada = 0")
        s.AppendLine("				  AND hp.visibleEnHistorico = 1")
        s.AppendLine("				  AND hp.respuesta = 1")
        s.AppendLine("				  AND idPieza = @idPieza")
        s.AppendLine("			UNION ")
        s.AppendLine("				SELECT pro.nColumnaFechaFabricacion, CONVERT(NVARCHAR, hp.fechaFab, 103) AS fechaFab")
        s.AppendLine("				FROM historicoProduc AS hp")
        s.AppendLine("				INNER JOIN planosRutasOperaciones AS pro ON pro.id = hp.idOperacion")
        s.AppendLine("				WHERE pro.nColumnaFechaFabricacion <> ''")
        s.AppendLine("				  AND hp.fechaFab <> ''")
        s.AppendLine("				  AND pro.solicitar_fechaFab = 1")
        s.AppendLine("				  AND hp.cancelada = 0")
        s.AppendLine("				  AND hp.cancelada = 0")
        s.AppendLine("				  AND hp.visibleEnHistorico = 1")
        s.AppendLine("				  AND hp.respuesta = 1")
        s.AppendLine("				  AND idPieza = @idPieza")
        s.AppendLine("			UNION ")
        s.AppendLine("				SELECT pro.nColumnaFechaOperario, CONVERT(NVARCHAR, hp.inicio , 103) AS fecha")
        s.AppendLine("				FROM historicoProduc AS hp")
        s.AppendLine("				INNER JOIN planosRutasOperaciones AS pro ON pro.id = hp.idOperacion")
        s.AppendLine("				WHERE pro.nColumnaFechaOperario <> ''")
        s.AppendLine("				  AND pro.solicitar_operario = 1")
        s.AppendLine("				  AND hp.inicio <> ''")
        s.AppendLine("				  AND hp.cancelada = 0")
        s.AppendLine("				  AND idPieza = @idPieza")
        s.AppendLine("			UNION ")
        s.AppendLine("				SELECT pro.nColumnaMarcaOperacion, hp.marca")
        s.AppendLine("				FROM historicoProduc AS hp")
        s.AppendLine("				INNER JOIN planosRutasOperaciones AS pro ON pro.id = hp.idOperacion")
        s.AppendLine("				WHERE pro.nColumnaMarcaOperacion <> ''")
        s.AppendLine("				  AND pro.solicitar_marca = 1")
        s.AppendLine("				  AND hp.marca <> ''")
        s.AppendLine("				  AND hp.cancelada = 0")
        s.AppendLine("				  AND hp.visibleEnHistorico = 1")
        s.AppendLine("				  AND hp.respuesta = 1")
        s.AppendLine("				  AND idPieza = @idPieza")
        s.AppendLine("			UNION ")
        s.AppendLine("				SELECT pro.nColumnaPlanoOperacion, hp.plano")
        s.AppendLine("				FROM historicoProduc AS hp")
        s.AppendLine("				INNER JOIN planosRutasOperaciones AS pro ON pro.id = hp.idOperacion")
        s.AppendLine("				WHERE pro.nColumnaPlanoOperacion <> ''")
        s.AppendLine("				  AND pro.solicitar_plano = 1")
        s.AppendLine("				  AND hp.plano <> ''")
        s.AppendLine("				  AND hp.cancelada = 0")
        s.AppendLine("				  AND hp.visibleEnHistorico = 1")
        s.AppendLine("				  AND hp.respuesta = 1")
        s.AppendLine("				  AND idPieza = @idPieza")
        s.AppendLine("			--UNION ")
        s.AppendLine("			--	SELECT pro.nColumnaNProveedorOperacion, hp.")
        s.AppendLine("			--	FROM historicoProduc AS hp")
        s.AppendLine("			--	INNER JOIN planosRutasOperaciones AS pro ON pro.id = hp.idOperacion")
        s.AppendLine("			--	WHERE pro.nColumnaNProveedorOperacion <> ''")
        s.AppendLine("			--	  AND hp. <> ''")
        s.AppendLine("			--	  AND hp.cancelada = 0")
        s.AppendLine("			--	  AND idPieza = @idPieza")
        s.AppendLine("			UNION ")
        s.AppendLine("				SELECT pro.nColumnaNserieOperacion, hp.Nserie")
        s.AppendLine("				FROM historicoProduc AS hp")
        s.AppendLine("				INNER JOIN planosRutasOperaciones AS pro ON pro.id = hp.idOperacion")
        s.AppendLine("				WHERE pro.nColumnaNserieOperacion <> ''")
        s.AppendLine("				  AND hp.Nserie <> ''")
        s.AppendLine("				  AND pro.solicitar_n_Serie = 1")
        s.AppendLine("				  AND hp.cancelada = 0")
        s.AppendLine("				  AND hp.visibleEnHistorico = 1")
        s.AppendLine("				  AND hp.respuesta = 1")
        s.AppendLine("				  AND idPieza = @idPieza")
        s.AppendLine("			UNION ")
        s.AppendLine("				SELECT pro.nColumnaOperario, CONVERT(NVARCHAR, CASE WHEN @tabla = 'piezas_Crystal_ejes' THEN o.nombre ELSE CAST(o.codigo AS VARCHAR(1000)) END) AS operario")
        s.AppendLine("				FROM historicoProduc AS hp")
        s.AppendLine("				INNER JOIN planosRutasOperaciones AS pro ON pro.id = hp.idOperacion")
        s.AppendLine("				INNER JOIN operarios AS o ON o.id = hp.idOperario")
        s.AppendLine("				WHERE pro.nColumnaOperario <> ''")
        s.AppendLine("				  AND hp.operario <> ''")
        s.AppendLine("				  AND pro.solicitar_operario = 1")
        s.AppendLine("				  AND hp.cancelada = 0")
        s.AppendLine("				  AND hp.visibleEnHistorico = 1")
        s.AppendLine("				  AND hp.respuesta = 1")
        s.AppendLine("				  AND idPieza = @idPieza")
        s.AppendLine("			UNION ")
        s.AppendLine("				SELECT pro.nColumnaOrdenFabOperacion, hp.ordenFab")
        s.AppendLine("				FROM historicoProduc AS hp")
        s.AppendLine("				INNER JOIN planosRutasOperaciones AS pro ON pro.id = hp.idOperacion")
        s.AppendLine("				WHERE pro.nColumnaOrdenFabOperacion <> ''")
        s.AppendLine("				  AND hp.ordenFab <> ''")
        s.AppendLine("				  AND pro.solicitar_OF = 1")
        s.AppendLine("				  AND hp.cancelada = 0")
        s.AppendLine("				  AND hp.visibleEnHistorico = 1")
        s.AppendLine("				  AND hp.respuesta = 1")
        s.AppendLine("				  AND idPieza = @idPieza")
        sqlValoresCrystal_operacionesPieza(s)
        s.AppendLine("		/* VARIABLES PARA EL LOOP */")
        s.AppendLine("        DECLARE @iIlara INT = 1")
        s.AppendLine("		DECLARE @Zenbat INT = (SELECT COUNT(id) FROM @dt)")
        s.AppendLine("		/* LOOP */")
        s.AppendLine("        WHILE (@iIlara < @Zenbat + 1)")
        s.AppendLine("        BEGIN")
        s.AppendLine("			/* VARIABLES POR CADA VALOR CON NCOLUMNA EN CRYSTAL */")
        s.AppendLine("        	DECLARE @Col VARCHAR(1000) = (SELECT nColumna                     FROM @dt WHERE id = @iIlara)")
        s.AppendLine("        	DECLARE @Val VARCHAR(1000) = (SELECT REPLACE(valor, ',', '.')	  FROM @dt WHERE id = @iIlara)")
        s.AppendLine("			/* CREAR EL UPDATE DE CRYSTAL */			")
        s.AppendLine("       		DECLARE @SQLLAG VARCHAR(3000)= 'UPDATE ' + @tabla + ' SET [' + @Col + '] = ''' + @Val + ''' ' + ' WHERE (idPiezasNumerosSerie = ' + CAST(@idPieza as nvarchar) + ')'")
        s.AppendLine("			/* EJECUTAR EL UPDATE QUE HEMOS GENERADO */")
        s.AppendLine("       		EXEC(@SQLLAG) ")
        s.AppendLine("			/* ESTA PARTE COMENTADA ESTA HECHA PARA PODER DEBUGUEAR LA CONSULTA DE UNA FORMA RAPIDA */")
        s.AppendLine("			/*")
        s.AppendLine("				PRINT('-------------------------------')")
        s.AppendLine("				PRINT(@iIlara)")
        s.AppendLine("				PRINT(@Col)")
        s.AppendLine("				PRINT(@Val)")
        s.AppendLine("				PRINT(@SQLLAG)")
        s.AppendLine("				DECLARE @SQLLAGtest VARCHAR(3000)= 'SELECT ''' + @Col + ''' AS columna,  ' + @Col + ' AS valor, ''' + @val + ''' AS valor2 FROM ' + @tabla + ' WHERE (idPiezasNumerosSerie = ' + CAST(@idPieza as nvarchar) + ')'")
        s.AppendLine("				PRINT(@SQLLAGtest)")
        s.AppendLine("				PRINT('-------------------------------')")
        s.AppendLine("			*/")
        s.AppendLine("			SET @iIlara += 1")
        s.AppendLine("        END")
        s.AppendLine("")
        Return s.ToString()
    End Function

    ''' <summary>
    ''' Ramas del "gran select" de sqlValoresCrystal_pieza para las operaciones DE PIEZA
    ''' (piezasNumerosSerieOperaciones / piezasNumerosSerieOperaciones_valor), que no estan en la
    ''' ruta del plano y por eso no salian por los JOIN a planosRutasOperaciones. Se enlazan por
    ''' hp.idPiezasNumerosSerieOperaciones. La copia por pieza no suele traer el nColumna de las
    ''' cotas: se recupera del catalogo (operaciones_valor) por nombre de operacion y orden, igual
    ''' que hace la APK al recibir el resultado (ID 349, WebServiceFunciones.SQL_nColumnaValorOperacionPieza).
    ''' Empiezan por UNION: se añaden detras de las ramas de la ruta.
    ''' </summary>
    Private Shared Sub sqlValoresCrystal_operacionesPieza(ByVal s As StringBuilder)
        Const filtro As String = "				  AND hp.cancelada = 0 AND hp.visibleEnHistorico = 1 AND hp.respuesta = 1 AND hp.idPieza = @idPieza"
        Const fromPnso As String = "				FROM historicoProduc AS hp INNER JOIN piezasNumerosSerieOperaciones AS pnso ON pnso.id = hp.idPiezasNumerosSerieOperaciones"

        s.AppendLine("			UNION ")
        s.AppendLine("				SELECT pv.nColumna, pv.valor")
        s.AppendLine("				FROM (")
        s.AppendLine("					SELECT hpv.valor,")
        s.AppendLine("						   ISNULL(NULLIF(pov.nColumna, ''), ISNULL((SELECT TOP 1 ovCat.nColumna")
        s.AppendLine("						                                            FROM operaciones AS opCat")
        s.AppendLine("						                                            INNER JOIN operaciones_valor AS ovCat ON ovCat.idOperacion = opCat.id AND ovCat.orden = pov.orden")
        s.AppendLine("						                                            WHERE opCat.nombre = pnso.nombre AND ISNULL(ovCat.nColumna, '') <> ''")
        s.AppendLine("						                                            ORDER BY opCat.id DESC), '')) AS nColumna")
        s.AppendLine("					FROM historicoProduc AS hp")
        s.AppendLine("					INNER JOIN piezasNumerosSerieOperaciones AS pnso ON pnso.id = hp.idPiezasNumerosSerieOperaciones")
        s.AppendLine("					INNER JOIN historicoProduc_valor AS hpv ON hpv.idHistoricoProduc = hp.id")
        s.AppendLine("					INNER JOIN piezasNumerosSerieOperaciones_valor AS pov ON pov.idpiezasNumerosSerieOperacion = pnso.id")
        s.AppendLine("					                                                   AND ((hpv.orden = pov.orden AND hpv.idPlanosRutaOperacionesValor IS NULL) OR hpv.idPlanosRutaOperacionesValor = pov.id)")
        s.AppendLine("					WHERE hpv.valor <> ''")
        s.AppendLine("	" & filtro)
        s.AppendLine("				) AS pv")
        s.AppendLine("				WHERE pv.nColumna <> ''")

        ' Datos de cabecera de la operacion: {columna nColumna de pnso, valor de hp, condicion de "solicitar"}
        Dim campos As String()() = {
            New String() {"nColumnaColadaOperacion", "hp.colada", "pnso.solicitar_Colada = 1"},
            New String() {"nColumnaEdicionOperacion", "hp.edicion", "pnso.solicitar_edicion = 1"},
            New String() {"nColumnaFechaFabricacion", "CONVERT(NVARCHAR, hp.fechaFab, 103)", "pnso.solicitar_FechaFab = 1"},
            New String() {"nColumnaFechaOperario", "CONVERT(NVARCHAR, hp.inicio, 103)", "hp.inicio <> ''"},
            New String() {"nColumnaMarcaOperacion", "hp.marca", "pnso.solicitar_Marca = 1"},
            New String() {"nColumnaNserieOperacion", "hp.Nserie", "pnso.solicitar_N_Serie = 1"},
            New String() {"nColumnaOrdenFabOperacion", "hp.ordenFab", "pnso.solicitar_OF = 1"}
        }
        For Each c In campos
            s.AppendLine("			UNION ")
            s.AppendLine("				SELECT pnso." & c(0) & ", " & c(1))
            s.AppendLine(fromPnso)
            s.AppendLine("				WHERE ISNULL(pnso." & c(0) & ", '') <> ''")
            s.AppendLine("				  AND ISNULL(" & c(1) & ", '') <> ''")
            s.AppendLine("				  AND " & c(2))
            s.AppendLine(filtro)
        Next

        s.AppendLine("			UNION ")
        s.AppendLine("				SELECT pnso.nColumnaOperario, CONVERT(NVARCHAR, CASE WHEN @tabla = 'piezas_Crystal_ejes' THEN o.nombre ELSE CAST(o.codigo AS VARCHAR(1000)) END)")
        s.AppendLine(fromPnso)
        s.AppendLine("				INNER JOIN operarios AS o ON o.id = hp.idOperario")
        s.AppendLine("				WHERE ISNULL(pnso.nColumnaOperario, '') <> ''")
        s.AppendLine("				  AND hp.operario <> ''")
        s.AppendLine(filtro)
    End Sub
#End Region

End Class
