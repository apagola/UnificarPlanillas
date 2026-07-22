Imports System.Text
Imports System.Data
Imports Newtonsoft.Json

' =============================================================================
' Generacion del JSON SAP de ruedas de hierro DENTRO de la miniapp (self-contained).
'
' OPCION B (duplicar): se copia aqui la logica de la web para NO arrastrar las DLLs
' del web (Telerik/Crystal) ni depender de la connString compilada en AppCode.dll
' (modulo globales es Friend -> no overrideable, y apunta a la BD copia). Aqui se
' usa la capa de datos local 'funciones' (connString de PRODUCCION de la miniapp).
'
' >>> MANTENER SINCRONIZADO con el web <<<
'   - get_info_SAP_ruedas_hierro*  : CAF_MiiRA_A_R\AppCode\Maestras\historicoProduc.vb  (~L6146)
'   - crear_JSON_SAP + helpers      : CAF_MiiRA_A_R\Funciones\Produccion\crearSAP.vb    (~L102)
'   - clases Record_sap, etc.       : CAF_MiiRA_A_R\Funciones\Produccion\crearSAP.vb    (~L507)
' Unicos cambios respecto al web: MyFunctions.listToString -> listToString (local),
'   BD.GetDataSet -> funciones.cargar_dataset, y AppCode.MyFunctions.dateToYYYYMMDDHHmmss
'   -> ToString("yyyyMMddHHmmss") (rama LABOR_TIME, que va a False para ruedas hierro).
' El SQL es logicamente identico al del web => el JSON resultante es byte a byte igual.
' =============================================================================
Public Class RuedasHierroSAP

#Region "COLA generarJson (capa de datos local, PRODUCCION)"
    ' nSerie marcados desde la web con generarJson = 1 (la cola/disparador).
    Public Shared Function getNSeriesPendientesGenerarJson() As List(Of Integer)
        Dim ids As List(Of Integer) = New List(Of Integer)
        Dim dt As DataTable = funciones.cargar_todos("SELECT id FROM SAP_ruedas_hierro_nSeries WHERE generarJson = 1")
        If dt IsNot Nothing Then
            For Each r As DataRow In dt.Rows
                ids.Add(CInt(r("id")))
            Next
        End If
        Return ids
    End Function

    ' Pone generarJson = 1/0 en los nSerie indicados (la miniapp la usa para desencolar con valor=False).
    Public Shared Function Update_generarJson(ByVal ids As List(Of Integer), valor As Boolean) As Integer
        Dim s As StringBuilder = New StringBuilder()
        s.AppendLine("UPDATE SAP_ruedas_hierro_nSeries")
        s.AppendLine("SET generarJson = " & If(valor, "1", "0"))
        s.AppendLine("WHERE id IN (" & listToString(ids) & ")")
        Return funciones.Update(s.ToString())
    End Function
#End Region

#Region "JSON SAP (wrapper, mismo armado que la web)"
    Public Shared Function crear_JSON_SAP_idHPns(ByVal ids As List(Of Integer), todas As Boolean, Optional marcarExportado As Boolean = True) As String
        Dim dt_infoSAP = get_info_SAP_ruedas_hierro_nSeries(ids, todas, marcarExportado)
        Dim json_content = crear_JSON_SAP(dt_infoSAP.Tables(0), dt_infoSAP.Tables(1), dt_infoSAP.Tables(2), dt_infoSAP.Tables(3), dt_infoSAP.Tables(4),
                                                             True, True, True, True, False, True)
        Return json_content
    End Function
#End Region

#Region "get_info_SAP_ruedas_hierro (DUPLICADO de historicoProduc.vb - MANTENER SINCRONIZADO)"
    Shared Function get_info_SAP_ruedas_hierro_nSeries(ByVal ids As List(Of Integer), todas As Boolean, Optional marcarExportado As Boolean = True) As DataSet
        Return get_info_SAP_ruedas_hierro(ids, "nSerie", todas, False, marcarExportado)
    End Function
    Shared Function get_info_SAP_ruedas_hierro_plano(ByVal ids As List(Of Integer), todas As Boolean, soloAcero As Boolean, Optional marcarExportado As Boolean = True) As DataSet
        Return get_info_SAP_ruedas_hierro(ids, "plano", todas, soloAcero, marcarExportado)
    End Function
    Private Shared Function get_info_SAP_ruedas_hierro(ByVal ids As List(Of Integer), tipo As String, todas As Boolean, soloAcero As Boolean, Optional marcarExportado As Boolean = True) As DataSet
        Dim s As StringBuilder = New StringBuilder()

        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ''''''''''                                                                          '''''''''''''''
        ''''''''''       ESTA FUNCION ESTA AQUI PARA MANTENER TODAS LAS DEMAS IGUALES       '''''''''''''''
        ''''''''''                                                                          '''''''''''''''
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        s.AppendLine("---------------------------------------------------------------------------------------")
        s.AppendLine("------- ESTE OUTPUT TIENE QUE SER IGUAL PARA: get_info_SAP_ruedas_hierro      ---------")
        s.AppendLine("------- ESTE OUTPUT TIENE QUE SER IGUAL PARA: get_info_SAP_unidas             ---------")
        s.AppendLine("------- ESTE OUTPUT TIENE QUE SER IGUAL PARA: get_info_SAP_reductoras         ---------")
        s.AppendLine("------- ESTE OUTPUT TIENE QUE SER IGUAL PARA: get_info_SAP_reductoras_planos  ---------")
        s.AppendLine("------- ESTE OUTPUT TIENE QUE SER IGUAL PARA: get_info_SAP_ruedasElasticas    ---------")
        s.AppendLine("------- ESTE OUTPUT TIENE QUE SER IGUAL PARA: get_info_SAP_ruedas_pintadas    ---------")
        s.AppendLine("---------------------------------------------------------------------------------------")
        s.AppendLine("")
        s.AppendLine("DECLARE @dt AS TABLE (ordenOp INT, retrabajo BIT, id INT, semanaLote VARCHAR(100),idPlanosRutasOperaciones INT,codigoSap VARCHAR(100),plano VARCHAR(100),colada VARCHAR(100),Nserie VARCHAR(100),terminada BIT, SERIALNO VARCHAR(100))")
        s.AppendLine("")
        s.AppendLine("INSERT INTO @dt ")
        s.AppendLine("SELECT ordenOp, retrabajo, id, semanaLote, idPlanosRutasOperaciones, codigoSap, plano, colada, Nserie, terminada, CONCAT(colada, '-', Nserie) AS SERIALNO")
        s.AppendLine("FROM (")
        s.AppendLine("	SELECT duplicante.ordenOp AS ordenOp")
        s.AppendLine("		 , CASE WHEN duplicante2.orden = 2 THEN 1 ELSE 0 END AS retrabajo")
        s.AppendLine("		 , CONCAT(rhns.id, '0', duplicante.ordenOp, duplicante2.orden) AS id")
        s.AppendLine("		 , rhp.nOrder AS semanaLote, rhp.id AS idPlanosRutasOperaciones")
        s.AppendLine("		 , CASE WHEN duplicante.ordenOp = 1 AND duplicante2.orden = 1 THEN  CONCAT(rhp.forja_Operation_SAP, '-', rhp.forja_OperationActivity) ")
        s.AppendLine("			   WHEN duplicante.ordenOp = 1 AND duplicante2.orden = 2 THEN  CONCAT(rhp.forja_Rework_Operation_SAP, '-', rhp.forja_Rework_OperationActivity) ")
        s.AppendLine("			   WHEN duplicante.ordenOp = 2 AND duplicante2.orden = 1 THEN  CONCAT(rhp.tt_Operation_SAP, '-', rhp.tt_OperationActivity) ")
        s.AppendLine("			   WHEN duplicante.ordenOp = 2 AND duplicante2.orden = 2 THEN  CONCAT(rhp.tt_Rework_Operation_SAP, '-', rhp.tt_Rework_OperationActivity) ")
        s.AppendLine("			   WHEN duplicante.ordenOp = 3 AND duplicante2.orden = 1 THEN  CONCAT(rhp.granallado_Operation_SAP, '-', rhp.granallado_OperationActivity) ")
        s.AppendLine("			   WHEN duplicante.ordenOp = 3 AND duplicante2.orden = 2 THEN  CONCAT(rhp.granallado_Rework_Operation_SAP, '-', rhp.granallado_Rework_OperationActivity) ")
        s.AppendLine("			   WHEN duplicante.ordenOp = 4 AND duplicante2.orden = 1 THEN  CONCAT(rhp.mecanizado_Operation_SAP, '-', rhp.mecanizado_OperationActivity) ")
        s.AppendLine("			   WHEN duplicante.ordenOp = 4 AND duplicante2.orden = 2 THEN  CONCAT(rhp.mecanizado_Rework_Operation_SAP, '-', rhp.mecanizado_Rework_OperationActivity) ")
        s.AppendLine("			   WHEN duplicante.ordenOp = 5 AND duplicante2.orden = 1 THEN  CONCAT(rhp.verificado_Operation_SAP, '-', rhp.verificado_OperationActivity) ")
        s.AppendLine("			   WHEN duplicante.ordenOp = 5 AND duplicante2.orden = 2 THEN  CONCAT(rhp.verificado_Rework_Operation_SAP, '-', rhp.verificado_Rework_OperationActivity) ")
        s.AppendLine("			   WHEN duplicante.ordenOp = 6 AND duplicante2.orden = 1 THEN  CONCAT(rhp.repaso_Operation_SAP, '-', rhp.repaso_OperationActivity) ")
        s.AppendLine("			   WHEN duplicante.ordenOp = 6 AND duplicante2.orden = 2 THEN  CONCAT(rhp.repaso_Rework_Operation_SAP, '-', rhp.repaso_Rework_OperationActivity) 	     ")
        s.AppendLine("		   END AS codigoSap")
        s.AppendLine("		 , rhp.plano AS plano, rhns.colada AS colada, rhns.nSerie AS Nserie")
        s.AppendLine("		 , CASE WHEN duplicante.ordenOp = 1 THEN rhns.forja")
        s.AppendLine("				WHEN duplicante.ordenOp = 2 THEN rhns.tt")
        s.AppendLine("				WHEN duplicante.ordenOp = 3 THEN rhns.granallado")
        s.AppendLine("				WHEN duplicante.ordenOp = 4 THEN rhns.mecanizado")
        s.AppendLine("				WHEN duplicante.ordenOp = 5 THEN rhns.verificado")
        s.AppendLine("				WHEN duplicante.ordenOp = 6 THEN rhns.repaso")
        s.AppendLine("		    END AS terminada")
        s.AppendLine("		 , CASE WHEN duplicante.ordenOp = 1 THEN rhns.forja_exportado")
        s.AppendLine("				WHEN duplicante.ordenOp = 2 THEN rhns.tt_exportado")
        s.AppendLine("				WHEN duplicante.ordenOp = 3 THEN rhns.granallado_exportado")
        s.AppendLine("				WHEN duplicante.ordenOp = 4 THEN rhns.mecanizado_exportado")
        s.AppendLine("				WHEN duplicante.ordenOp = 5 THEN rhns.verificado_exportado")
        s.AppendLine("				WHEN duplicante.ordenOp = 6 THEN rhns.repaso_exportado")
        s.AppendLine("		    END AS exportado")
        s.AppendLine("	FROM SAP_ruedas_hierro_planos as rhp")
        s.AppendLine("	INNER JOIN SAP_ruedas_hierro_nSeries AS rhns ON rhp.id = rhns.idSAP_ruedas_hierro_planos")
        s.AppendLine("	INNER JOIN (")
        s.AppendLine("				  SELECT 1 AS ordenOp")
        If Not soloAcero Then
            s.AppendLine("      UNION ALL SELECT 2 AS ordenOp")
            s.AppendLine("		UNION ALL SELECT 3 AS ordenOp UNION ALL SELECT 4 AS ordenOp")
            s.AppendLine("		UNION ALL SELECT 5 AS ordenOp") ' SELECT 6 (REPASO) eliminado: repaso se ignora, ya no se exporta a SAP
        End If
        s.AppendLine("	) AS duplicante ON 1 = 1")
        s.AppendLine("	INNER JOIN (SELECT 1 AS orden UNION SELECT 2) AS duplicante2 ON 1 = 1")
        s.AppendLine("	WHERE (")
        s.AppendLine("	       (rhns.forja       = 0")  'SI FORJA NOOK
        If soloAcero Then
            s.AppendLine("		OR rhns.forja          = 1)")  'SI FORJA OK
        Else
            s.AppendLine("		OR rhns.tt          = 0")  'SI TT NOOK
            s.AppendLine("		OR rhns.granallado  = 0")  'SI GRANALLADO NOOK
            s.AppendLine("		OR rhns.mecanizado  = 0")  'SI MECANIZADO NOOK
            s.AppendLine("		OR 1 = 0")  'REPASO IGNORADO: ya no decide el export (antes: OR rhns.repaso = 0)
            s.AppendLine("		OR rhns.verificado  = 0")  'SI VERIFICADO NOOK
            s.AppendLine("		OR rhns.verificado  = 1)") 'SI VERIFICADO OK
            s.AppendLine("		OR (ordenOp <= 2 AND rhns.tt = 1)")  'SI TT OK (SOLO TT)
            s.AppendLine("		OR (ordenOp <= 3 AND rhns.granallado = 1)")  'SI GRANALLADO OK (SOLO GRANALLADO)
        End If
        s.AppendLine("		)")
        If tipo = "nSerie" Then
            s.AppendLine("	  AND rhns.id IN (" & listToString(ids) & ") ")
        Else
            s.AppendLine("	  AND rhp.id IN (" & listToString(ids) & ") ")
        End If
        s.AppendLine(") AS a")
        s.AppendLine("WHERE a.codigoSap <> '-'")
        s.AppendLine("  AND a.terminada >= 0 /* SI ES -1 ES QUE AUN NO HA TENIDO RESPUESTA */")
        s.AppendLine("  AND a.terminada <> 2 /* RETRABAJO PENDIENTE: NO SE EXPORTA HASTA RESOLVERSE A OK/NOK (terminada es BIT y un 2 se convertiria en 1) */")
        s.AppendLine("  AND NOT (retrabajo = 1 AND terminada = 0) /* Si es NO OK , no tiene retrabajo */")
        If Not todas Then
            s.AppendLine("  AND a.exportado = 0")
        End If
        s.AppendLine("")
        s.AppendLine("/* Eliminar operaciones posteriores a un SCRAP (terminada=0 sin retrabajo OK): tras un SCRAP no se exportan más operaciones de la rueda */")
        s.AppendLine("DELETE d FROM @dt AS d")
        s.AppendLine("WHERE EXISTS (")
        s.AppendLine("    SELECT 1 FROM @dt AS scrap")
        s.AppendLine("    WHERE scrap.id / 1000 = d.id / 1000")
        s.AppendLine("      AND scrap.retrabajo = 0")
        s.AppendLine("      AND scrap.terminada = 0")
        s.AppendLine("      AND scrap.ordenOp < d.ordenOp")
        s.AppendLine("      AND NOT EXISTS (")
        s.AppendLine("          SELECT 1 FROM @dt AS rework")
        s.AppendLine("          WHERE rework.id / 1000 = scrap.id / 1000")
        s.AppendLine("            AND rework.ordenOp = scrap.ordenOp")
        s.AppendLine("            AND rework.retrabajo = 1")
        s.AppendLine("            AND rework.terminada = 1")
        s.AppendLine("      )")
        s.AppendLine(")")
        s.AppendLine("")
        s.AppendLine("/* START / END */")
        s.AppendLine("SELECT id, semanaLote,idPlanosRutasOperaciones,codigoSap,plano,colada,Nserie,terminada, SERIALNO")
        s.AppendLine("FROM @dt")
        s.AppendLine("")
        s.AppendLine("/* ASSEMBLY */")
        s.AppendLine("SELECT id")
        s.AppendLine("	 , ISNULL(REPLACE(CASE WHEN ultimoNserie = 1 AND suma_anterior - CAST(FLOOR(suma_anterior) AS DECIMAL(16,3)) > 0 THEN (1 - suma_anterior + CAST(FLOOR(suma_anterior) AS DECIMAL(16,3))) ELSE 0 END + quantity, '.', ','), '') AS quantity")
        s.AppendLine("	 , uom ")
        s.AppendLine("	 , material	")
        s.AppendLine("	 , plant ")
        s.AppendLine("	 , storage_location	")
        s.AppendLine("	 , reservation ")
        s.AppendLine("	 , reservation_item	")
        s.AppendLine("	 , assembly_type	")
        s.AppendLine("	 , assembly_type_description ")
        s.AppendLine("	 , SLMat ")
        s.AppendLine("	 , NserieMat ")
        s.AppendLine("	 , operacionAcero ")
        s.AppendLine("	 , CASE WHEN ultimoNserie = 1 AND aceroMenor = 1 THEN 1 ELSE 0 END AS imprimirRestoAceroMenor")
        s.AppendLine("FROM (")
        s.AppendLine("	SELECT dt.id")
        s.AppendLine("		 , CAST(FLOOR(CAST(REPLACE(CASE WHEN CAST(REPLACE(ca.quantity, ',', '.') AS DECIMAL(16,3)) < CAST(REPLACE(ca.quantityTeorica, ',', '.') AS DECIMAL(16,3)) THEN ca.quantityTeorica ELSE ca.quantity END, ',', '.') AS DECIMAL(16,10)) * 1e3) / 1e3 AS DECIMAL(16,3)) AS quantity")
        s.AppendLine("		 , ISNULL(ca.uom						, '') AS uom")
        s.AppendLine("		 , ISNULL(ca.material					, '') AS material")
        s.AppendLine("		 , ISNULL(ca.plant					, '') AS plant")
        s.AppendLine("		 , ISNULL(ca.storage_location			, '') AS storage_location")
        s.AppendLine("		 , ISNULL(ca.reservation				, '') AS reservation")
        s.AppendLine("		 , ISNULL(ca.reservation_item			, '') AS reservation_item")
        s.AppendLine("		 , ISNULL(ca.assembly_type			, '') AS assembly_type")
        s.AppendLine("		 , ISNULL(ca.assembly_type_description, '') AS assembly_type_description")
        s.AppendLine("		 , '' AS SLMat")
        s.AppendLine("		 , '' AS NserieMat")
        s.AppendLine("		 , CASE WHEN ISNULL(ca.uom						, '') = 'KG' THEN 1 ELSE 0 END AS operacionAcero")
        s.AppendLine("		 , ISNULL(SUM(CAST(FLOOR(CAST(REPLACE(CASE WHEN CAST(REPLACE(ca.quantity, ',', '.') AS DECIMAL(16,3)) < CAST(REPLACE(ca.quantityTeorica, ',', '.') AS DECIMAL(16,3)) THEN ca.quantityTeorica ELSE ca.quantity END, ',', '.') AS DECIMAL(16,3)) * 1e3) / 1e3 AS DECIMAL(16,3)) ) OVER (PARTITION BY dt.semanaLote ORDER BY dt.nSerie ASC ROWS BETWEEN UNBOUNDED PRECEDING AND 0 PRECEDING), 0) AS suma_anterior")
        s.AppendLine("		 , CASE WHEN ROW_NUMBER() OVER(PARTITION BY dt.semanaLote ORDER BY dt.nSerie DESC) = 1 THEN 1 ELSE 0 END AS ultimoNserie")
        s.AppendLine("		 , CASE WHEN CAST(REPLACE(ca.quantity, ',', '.') AS DECIMAL(16,3)) < CAST(REPLACE(ca.quantityTeorica, ',', '.') AS DECIMAL(16,3)) THEN 1 ELSE 0 END AS aceroMenor")
        s.AppendLine("	FROM @dt AS dt")
        s.AppendLine("	LEFT OUTER JOIN SAP_ruedas_hierro_component_assigment AS ca ON ca.idSAP_ruedas_hierro_planos = dt.idPlanosRutasOperaciones  AND ca.idOperacionSAP_DAT = dt.ordenOp")
        s.AppendLine("	WHERE ISNULL(CASE WHEN CAST(REPLACE(ca.quantity, ',', '.') AS DECIMAL(16,3)) < CAST(REPLACE(ca.quantityTeorica, ',', '.') AS DECIMAL(16,3)) THEN ca.quantityTeorica ELSE ca.quantity END , '') <> '' -- MINIMO TENER ALGO")
        s.AppendLine("	  AND retrabajo = 0")
        s.AppendLine(") AS a")
        s.AppendLine("")
        s.AppendLine("/* VALORES */")
        s.AppendLine("SELECT idHistoricoProduc, id, valor, nombreSap, ok")
        s.AppendLine("FROM (")
        s.AppendLine("  SELECT CONCAT(rhns.id, '0', CASE WHEN duplicante.ordenVar < 7 THEN 3 ELSE 5 END, '2'/* 1 para original 2 para retrabajo*/) AS idHistoricoProduc")
        s.AppendLine("       , -1 AS id")
        s.AppendLine("  	 , CASE WHEN duplicante.ordenVar = 12 THEN rhns.verificado_desequilibrio_gramos ELSE 1.00 END valor")
        s.AppendLine("  	 , CASE WHEN (duplicante.ordenVar = 1)	THEN CONCAT(rhp.granallado_ht_durezaIndividual_InspectionCharacteristic, '-', rhp.granallado_ht_durezaIndividual_VERWMERKM)")
        s.AppendLine("  			WHEN (duplicante.ordenVar = 2)	THEN CONCAT(rhp.granallado_ht_3_puntos_100_InspectionCharacteristic, '-', rhp.granallado_ht_3_puntos_100_VERWMERKM)")
        s.AppendLine("  			WHEN (duplicante.ordenVar = 3)	THEN CONCAT(rhp.granallado_ht_InspectionCharacteristic, '-', rhp.granallado_ht_VERWMERKM)")
        s.AppendLine("  			WHEN (duplicante.ordenVar = 5)	THEN CONCAT(rhp.granallado_diferenciaMaxMinW002_InspectionCharacteristic, '-', rhp.granallado_diferenciaMaxMinW002_VERWMERKM)")
        s.AppendLine("  			WHEN (duplicante.ordenVar = 6)	THEN CONCAT(rhp.granallado_ht_3_puntos_10_InspectionCharacteristic, '-', rhp.granallado_ht_3_puntos_10_VERWMERKM)")
        s.AppendLine("  			WHEN (duplicante.ordenVar = 7)	THEN CONCAT(rhp.verificado_particulasMagneticas_InspectionCharacteristic, '-', rhp.verificado_particulasMagneticas_VERWMERKM)")
        s.AppendLine("  			WHEN (duplicante.ordenVar = 19)	THEN CONCAT(rhp.verificado_ht_durezaIndividual_InspectionCharacteristic, '-', rhp.verificado_ht_durezaIndividual_VERWMERKM)")
        s.AppendLine("  			WHEN (duplicante.ordenVar = 8)	THEN CONCAT(rhp.verificado_utrasonidosLlanta_InspectionCharacteristic, '-', rhp.verificado_utrasonidosLlanta_VERWMERKM)")
        s.AppendLine("  			WHEN (duplicante.ordenVar = 9)	THEN CONCAT(rhp.verificado_utrasonidosVeloyCubo_InspectionCharacteristic, '-', rhp.verificado_utrasonidosVeloyCubo_VERWMERKM)")
        s.AppendLine("  			WHEN (duplicante.ordenVar = 10)	THEN CONCAT(rhp.verificado_utrasonidosSondaTandem_InspectionCharacteristic, '-', rhp.verificado_utrasonidosSondaTandem_VERWMERKM)")
        s.AppendLine("  			WHEN (duplicante.ordenVar = 11)	THEN CONCAT(rhp.verificado_tensionesResiduales_InspectionCharacteristic, '-', rhp.verificado_tensionesResiduales_VERWMERKM)")
        s.AppendLine("  			WHEN (duplicante.ordenVar = 12)	THEN CONCAT(rhp.verificado_desequilibrio_InspectionCharacteristic, '-', rhp.verificado_desequilibrio_VERWMERKM)")
        s.AppendLine("  			WHEN (duplicante.ordenVar = 13)	THEN CONCAT(rhp.verificado_resultadoDimensional_InspectionCharacteristic, '-', rhp.verificado_resultadoDimensional_VERWMERKM)")
        s.AppendLine("  			WHEN (duplicante.ordenVar = 14)	THEN CONCAT(rhp.verificado_inspeccionVisual_InspectionCharacteristic, '-', rhp.verificado_inspeccionVisual_VERWMERKM)")
        s.AppendLine("  			WHEN (duplicante.ordenVar = 15)	THEN CONCAT(rhp.verificado_perfil_InspectionCharacteristic, '-', rhp.verificado_perfil_VERWMERKM)")
        s.AppendLine("  			WHEN (duplicante.ordenVar = 16)	THEN CONCAT(rhp.verificado_rugosidadVelo_InspectionCharacteristic, '-', rhp.verificado_rugosidadVelo_VERWMERKM)")
        s.AppendLine("  			WHEN (duplicante.ordenVar = 17)	THEN CONCAT(rhp.verificado_rugosidadCalaje_InspectionCharacteristic, '-', rhp.verificado_rugosidadCalaje_VERWMERKM)")
        s.AppendLine("  			WHEN (duplicante.ordenVar = 18)	THEN CONCAT(rhp.verificado_marcaje_InspectionCharacteristic, '-', rhp.verificado_marcaje_VERWMERKM)")
        s.AppendLine("  		END AS nombreSap")
        s.AppendLine("  	 , CASE WHEN duplicante.ordenVar = 1  THEN CASE WHEN rhns.granallado_ht_durezaIndividual    >= 0 THEN rhns.granallado_ht_durezaIndividual    ELSE rhns.granallado END")
        s.AppendLine("			    WHEN duplicante.ordenVar = 2  THEN CASE WHEN rhns.granallado_ht_3_puntos_100        >= 0 THEN rhns.granallado_ht_3_puntos_100        ELSE rhns.granallado END")
        s.AppendLine("			    WHEN duplicante.ordenVar = 3  THEN CASE WHEN rhns.granallado_ht                     >= 0 THEN rhns.granallado_ht                     ELSE rhns.granallado END")
        s.AppendLine("			    WHEN duplicante.ordenVar = 5  THEN CASE WHEN rhns.granallado_diferenciaMaxMinW002   >= 0 THEN rhns.granallado_diferenciaMaxMinW002   ELSE rhns.granallado END")
        s.AppendLine("			    WHEN duplicante.ordenVar = 6  THEN CASE WHEN rhns.granallado_ht_3_puntos_10         >= 0 THEN rhns.granallado_ht_3_puntos_10         ELSE rhns.granallado END")
        s.AppendLine("			    WHEN duplicante.ordenVar = 7  THEN CASE WHEN rhns.verificado_particulasMagneticas   >= 0 THEN rhns.verificado_particulasMagneticas   ELSE rhns.verificado END")
        s.AppendLine("			    WHEN duplicante.ordenVar = 19 THEN CASE WHEN rhns.verificado_ht_durezaIndividual    >= 0 THEN rhns.verificado_ht_durezaIndividual    ELSE rhns.verificado END")
        s.AppendLine("			    WHEN duplicante.ordenVar = 8  THEN CASE WHEN rhns.verificado_utrasonidosLlanta      >= 0 THEN rhns.verificado_utrasonidosLlanta      ELSE rhns.verificado END")
        s.AppendLine("			    WHEN duplicante.ordenVar = 9  THEN CASE WHEN rhns.verificado_utrasonidosVeloyCubo   >= 0 THEN rhns.verificado_utrasonidosVeloyCubo   ELSE rhns.verificado END")
        s.AppendLine("			    WHEN duplicante.ordenVar = 10 THEN CASE WHEN rhns.verificado_utrasonidosSondaTandem >= 0 THEN rhns.verificado_utrasonidosSondaTandem ELSE rhns.verificado END")
        s.AppendLine("			    WHEN duplicante.ordenVar = 11 THEN CASE WHEN rhns.verificado_tensionesResiduales    >= 0 THEN rhns.verificado_tensionesResiduales    ELSE rhns.verificado END")
        s.AppendLine("			    WHEN duplicante.ordenVar = 12 THEN CASE WHEN rhns.verificado_desequilibrio          >= 0 THEN rhns.verificado_desequilibrio          ELSE rhns.verificado END")
        s.AppendLine("			    WHEN duplicante.ordenVar = 13 THEN CASE WHEN rhns.verificado_resultadoDimensional   >= 0 THEN rhns.verificado_resultadoDimensional   ELSE rhns.verificado END")
        s.AppendLine("			    WHEN duplicante.ordenVar = 14 THEN CASE WHEN rhns.verificado_inspeccionVisual       >= 0 THEN rhns.verificado_inspeccionVisual       ELSE rhns.verificado END")
        s.AppendLine("			    WHEN duplicante.ordenVar = 15 THEN CASE WHEN rhns.verificado_perfil                 >= 0 THEN rhns.verificado_perfil                 ELSE rhns.verificado END")
        s.AppendLine("			    WHEN duplicante.ordenVar = 16 THEN CASE WHEN rhns.verificado_rugosidadVelo          >= 0 THEN rhns.verificado_rugosidadVelo          ELSE rhns.verificado END")
        s.AppendLine("			    WHEN duplicante.ordenVar = 17 THEN CASE WHEN rhns.verificado_rugosidadCalaje        >= 0 THEN rhns.verificado_rugosidadCalaje        ELSE rhns.verificado END")
        s.AppendLine("			    WHEN duplicante.ordenVar = 18 THEN CASE WHEN rhns.verificado_marcaje                >= 0 THEN rhns.verificado_marcaje                ELSE rhns.verificado END")
        s.AppendLine("	            ELSE 1 ")
        s.AppendLine("	        END AS ok")
        s.AppendLine("  FROM SAP_ruedas_hierro_planos as rhp")
        s.AppendLine("  INNER JOIN SAP_ruedas_hierro_nSeries AS rhns ON rhp.id = rhns.idSAP_ruedas_hierro_planos")
        s.AppendLine("  INNER JOIN (")
        s.AppendLine("  			  SELECT 1  AS ordenVar UNION ALL SELECT 2  AS ordenVar --GRANALLADO")
        s.AppendLine("  	UNION ALL SELECT 3  AS ordenVar UNION ALL SELECT 4  AS ordenVar --GRANALLADO")
        s.AppendLine("  	UNION ALL SELECT 5  AS ordenVar UNION ALL SELECT 6  AS ordenVar --GRANALLADO")
        s.AppendLine("  	UNION ALL SELECT 7  AS ordenVar UNION ALL SELECT 8  AS ordenVar --VERIFICADO")
        s.AppendLine("  	UNION ALL SELECT 9  AS ordenVar UNION ALL SELECT 10 AS ordenVar --VERIFICADO")
        s.AppendLine("  	UNION ALL SELECT 11 AS ordenVar UNION ALL SELECT 12 AS ordenVar --VERIFICADO")
        s.AppendLine("  	UNION ALL SELECT 13 AS ordenVar UNION ALL SELECT 14 AS ordenVar --VERIFICADO")
        s.AppendLine("  	UNION ALL SELECT 15 AS ordenVar UNION ALL SELECT 16 AS ordenVar --VERIFICADO")
        s.AppendLine("  	UNION ALL SELECT 17 AS ordenVar UNION ALL SELECT 18 AS ordenVar --VERIFICADO")
        s.AppendLine("  	UNION ALL SELECT 19 AS ordenVar ")
        s.AppendLine("  ) AS duplicante ON 1 = 1")
        s.AppendLine("  WHERE (((rhns.granallado_ht_durezaIndividual   >= 0 OR rhns.granallado = 1) AND duplicante.ordenVar = 1)  ---GRANALLADO")
        s.AppendLine("      OR ((rhns.granallado_ht_3_puntos_100       >= 0 OR rhns.granallado = 1) AND duplicante.ordenVar = 2)  ---GRANALLADO")
        s.AppendLine("      OR ((rhns.granallado_ht                    >= 0 OR rhns.granallado = 1) AND duplicante.ordenVar = 3)  ---GRANALLADO")
        s.AppendLine("      OR ((rhns.granallado_diferenciaMaxMinW002  >= 0 OR rhns.granallado = 1) AND duplicante.ordenVar = 5)  ---GRANALLADO")
        s.AppendLine("      OR ((rhns.granallado_ht_3_puntos_10        >= 0 OR rhns.granallado = 1) AND duplicante.ordenVar = 6)  ---GRANALLADO")
        s.AppendLine("  	OR ((rhns.verificado_particulasMagneticas  >= 0 OR rhns.verificado = 1) AND duplicante.ordenVar = 7)  --VERIFICADO")
        s.AppendLine("  	OR ((rhns.verificado_ht_durezaIndividual   >= 0 OR rhns.verificado = 1) AND duplicante.ordenVar = 19) --VERIFICADO")
        s.AppendLine("      OR ((rhns.verificado_utrasonidosLlanta     >= 0 OR rhns.verificado = 1) AND duplicante.ordenVar = 8)  --VERIFICADO")
        s.AppendLine("      OR ((rhns.verificado_utrasonidosVeloyCubo  >= 0 OR rhns.verificado = 1) AND duplicante.ordenVar = 9)  --VERIFICADO")
        s.AppendLine("      OR ((rhns.verificado_utrasonidosSondaTandem>= 0 OR rhns.verificado = 1) AND duplicante.ordenVar = 10) --VERIFICADO")
        s.AppendLine("      OR ((rhns.verificado_tensionesResiduales   >= 0 OR rhns.verificado = 1) AND duplicante.ordenVar = 11) --VERIFICADO")
        s.AppendLine("      OR ((rhns.verificado_desequilibrio         >= 0 OR rhns.verificado = 1) AND duplicante.ordenVar = 12) --VERIFICADO")
        s.AppendLine("      OR ((rhns.verificado_resultadoDimensional  >= 0 OR rhns.verificado = 1) AND duplicante.ordenVar = 13) --VERIFICADO")
        s.AppendLine("  	OR ((rhns.verificado_inspeccionVisual      >= 0 OR rhns.verificado = 1) AND duplicante.ordenVar = 14) --VERIFICADO")
        s.AppendLine("      OR ((rhns.verificado_perfil                >= 0 OR rhns.verificado = 1) AND duplicante.ordenVar = 15) --VERIFICADO")
        s.AppendLine("      OR ((rhns.verificado_rugosidadVelo         >= 0 OR rhns.verificado = 1) AND duplicante.ordenVar = 16) --VERIFICADO")
        s.AppendLine("      OR ((rhns.verificado_rugosidadCalaje       >= 0 OR rhns.verificado = 1) AND duplicante.ordenVar = 17) --VERIFICADO")
        s.AppendLine("      OR ((rhns.verificado_marcaje               >= 0 OR rhns.verificado = 1) AND duplicante.ordenVar = 18))--VERIFICADO")
        s.AppendLine("    AND (duplicante.ordenVar <= 6 OR rhns.verificado <> 2) /* RETRABAJO PENDIENTE: NO SE EXPORTAN LAS VARIABLES DE VERIFICADO */")
        s.AppendLine("    AND rhp.id IN (SELECT idPlanosRutasOperaciones FROM @dt) ")
        If tipo = "nSerie" Then
            s.AppendLine("    AND rhns.id IN (" & listToString(ids) & ") ")
        Else
            s.AppendLine("    AND rhp.id IN (" & listToString(ids) & ") ")
        End If
        s.AppendLine(") AS a")
        s.AppendLine("WHERE nombreSap <> '-'")
        s.AppendLine("  AND ok <> 2 /* RETRABAJO PENDIENTE: NO SE EXPORTA NINGUN VALOR EN RETRABAJO */")
        s.AppendLine("")
        s.AppendLine("/* TIEMPOS OPERARIOS */")
        s.AppendLine("DECLARE @dtTiempos AS TABLE (id INT, codigoOperario VARCHAR(100),fechaIni DATETIME,duracion INT)")
        s.AppendLine("SELECT * FROM @dtTiempos")
        s.AppendLine("")
        s.AppendLine("/* SERIALNO */")
        s.AppendLine("DECLARE @dtSERIALNO AS TABLE (idSemanaLotes INT, sl_order VARCHAR(100), material VARCHAR(100), idPins INT, nSerie VARCHAR(100), SERIALNO VARCHAR(100), idNuevoSERIALNO INT)")
        s.AppendLine("SELECT sl_order ,material, idPins, nSerie, SERIALNO FROM @dtSERIALNO AS sno")
        s.AppendLine("")
        s.AppendLine("/* NO EXPORTABLES */")
        s.AppendLine("DECLARE @dtSinExportar AS TABLE (idPins INT, coladaPieza VARCHAR(100), nSeriePieza VARCHAR(100), exportado_HP BIT, exportado_Ruedas BIT, colada VARCHAR(100), Nserie VARCHAR(100))")
        s.AppendLine("SELECT idPins, coladaPieza, nSeriePieza, exportado_HP, exportado_Ruedas, colada, Nserie FROM @dtSinExportar")
        s.AppendLine("")
        ' EXPORT-FLAG DECOUPLE: la web descarga sin marcar (marcarExportado=False); la miniapp de planillas marca *_exportado tras escribir el JSON en la carpeta de SAP
        If marcarExportado Then
        s.AppendLine("UPDATE rhns")
        s.AppendLine("SET rhns.forja_exportado      = up.forja_exportado")
        s.AppendLine("  , rhns.forja_exportado_fecha = CASE WHEN rhns.forja_exportado_fecha IS NOT NULL THEN rhns.forja_exportado_fecha")
        s.AppendLine("                                           WHEN up.forja_exportado = 1 THEN CAST(GETDATE() AS DATE)")
        s.AppendLine("                                           ELSE NULL")
        s.AppendLine("                                      END")
        s.AppendLine("  , rhns.tt_exportado		    = up.tt_exportado")
        s.AppendLine("  , rhns.granallado_exportado = up.granallado_exportado")
        s.AppendLine("  , rhns.mecanizado_exportado = up.mecanizado_exportado")
        s.AppendLine("  , rhns.verificado_exportado = up.verificado_exportado")
        s.AppendLine("  , rhns.verificado_exportado_fecha = CASE WHEN rhns.verificado_exportado_fecha IS NOT NULL THEN rhns.verificado_exportado_fecha")
        s.AppendLine("                                           WHEN up.verificado_exportado = 1 THEN CAST(GETDATE() AS DATE)")
        s.AppendLine("                                           ELSE NULL")
        s.AppendLine("                                      END")
        s.AppendLine("  , rhns.repaso_exportado	    = up.repaso_exportado")
        If soloAcero Then
            s.AppendLine("  , rhns.forja = CASE WHEN up.forja_exportado = 1 AND rhns.forja <> 0 THEN 1 ELSE rhns.forja END")
        End If
        s.AppendLine("FROM SAP_ruedas_hierro_nSeries AS rhns")
        s.AppendLine("INNER JOIN (")
        s.AppendLine("	SELECT rhns.id")
        s.AppendLine("		 , MAX(CASE WHEN dt.ordenOp = 1 THEN 1 ELSE rhns.forja_exportado      END) AS forja_exportado")
        s.AppendLine("		 , MAX(CASE WHEN dt.ordenOp = 2 THEN 1 ELSE rhns.tt_exportado		    END) AS tt_exportado")
        s.AppendLine("		 , MAX(CASE WHEN dt.ordenOp = 3 THEN 1 ELSE rhns.granallado_exportado END) AS granallado_exportado")
        s.AppendLine("		 , MAX(CASE WHEN dt.ordenOp = 4 THEN 1 ELSE rhns.mecanizado_exportado END) AS mecanizado_exportado")
        s.AppendLine("		 , MAX(CASE WHEN dt.ordenOp = 5 THEN 1 ELSE rhns.verificado_exportado END) AS verificado_exportado")
        s.AppendLine("		 , MAX(CASE WHEN dt.ordenOp = 6 THEN 1 ELSE rhns.repaso_exportado	    END) AS repaso_exportado")
        s.AppendLine("	FROM SAP_ruedas_hierro_nSeries AS rhns")
        s.AppendLine("	INNER JOIN @dt AS dt ON (dt.id / 1000) = rhns.id")
        s.AppendLine("	GROUP BY rhns.id")
        s.AppendLine(") AS up ON up.id = rhns.id")
        End If

        Return funciones.cargar_dataset(s.ToString())
    End Function
#End Region

#Region "crear_JSON_SAP + helpers (DUPLICADO de crearSAP.vb - MANTENER SINCRONIZADO)"
    Public Shared Function crear_JSON_SAP(ByVal dtHP_infoSAP As DataTable, ByVal dtHPv_infoSAP As DataTable, ByVal dtHPv_infoSAP_valores As DataTable,
                                          ByVal dtHPv_infoSAP_tiempos_operarios As DataTable, ByVal dtSERIALNO As DataTable,
                                          ByVal OP_START As Boolean, ByVal ASSEMBLY As Boolean, ByVal VALORES As Boolean, ByVal OP_END As Boolean, ByVal LABOR_TIME As Boolean, SERIALNO As Boolean) As String

        Dim result As List(Of Record_sap) = New List(Of Record_sap)

        'Dim sumaQuantity = 0.0
        'Dim agrupadoSemanaLote = ""
        'Dim rowAnterior

        'For Each row In dtHP_infoSAP.Rows
        '    Try
        '        Dim dt_assigment As DataTable = dtHPv_infoSAP.Select("id = " & row("id")).CopyToDataTable
        '        For Each assigment In dt_assigment.Rows
        '            assigment("quantity") = 0
        '        Next
        '        'If agrupadoSemanaLote <> row("semanaLote") Then
        '        '    Dim dt_assigment_anterior As DataTable = dtHPv_infoSAP.Select("id = " & rowAnterior("id")).CopyToDataTable



        '        '    For Each assigment In dt_assigment.Rows
        '        '        Dim quantity As Decimal = assigment("quantity")
        '        '        sumaQuantity = sumaQuantity + quantity
        '        '    Next

        '        '    agrupadoSemanaLote = row("semanaLote")
        '        '    sumaQuantity = 0
        '        'End If

        '        'rowAnterior = row
        '    Catch ex As Exception

        '    End Try
        'Next

        For Each row In dtHP_infoSAP.Rows
            ' las operaciones vienen una vez esten empezadas y acabadas, se tendran que generar los 2 record a la vez.
            ' DATOS GENERALES DE LA OPERACION
            Try
                For Each codigoSAP In row("codigoSap").ToString.Split(",")
                    Try
                        Dim is_execution_oa As Is_execution_oa = crear_is_execution_oa(row, codigoSAP) ' si la operacion no tiene definido todo lo necesario cascara y ni se generara su correspondiente JSON (se ha pensado asi) 
                        Dim is_serialized_product As Is_serialized_product = crear_is_serialized_product(row)

                        Dim operacionAcero = False ' Como el es acero se trae segun los Component assigmment, se va actualizando hasta que sea true por cada vuelta para usar al hacer el zConfirm

                        ' OP START
                        If OP_START Then
                            result.Add(New Record_sap With {
                                .RECORD_ID = result.Count + 1,
                                .ACTION = "SAP_START",
                                .IS_EXECUTION_OA = is_execution_oa,
                                .IS_SERIALIZED_PRODUCT = is_serialized_product
                            })
                        End If

                        ' ASSEMBLY
                        If ASSEMBLY Then
                            Try
                                Dim dt_assigment As DataTable = dtHPv_infoSAP.Select("id = '" & row("id") & "'").CopyToDataTable
                                For Each assigment In dt_assigment.Rows

                                    ' Check operacionAcero
                                    If assigment("operacionAcero") = 1 Then
                                        operacionAcero = True
                                    End If
                                    Dim is_assembly_data As Is_assembly_data = crear_is_assembly_data(assigment, row("SERIALNO"), row("semanaLote").ToString, codigoSAP = "1251-1251-1")
                                    Dim is_related_component As Is_related_component = crear_is_related_component(assigment)
                                    If is_assembly_data.QUANTITY > 0 Then ' Si la cantidad es 0 no se exporta
                                        result.Add(New Record_sap With {
                                            .RECORD_ID = result.Count + 1,
                                            .ACTION = "COMPONENT_ASSEMBLY",
                                            .IS_ASSEMBLY_DATA = is_assembly_data,
                                            .IS_EXECUTION_OA = is_execution_oa,
                                            .IS_SERIALIZED_PRODUCT = is_serialized_product,
                                            .IS_RELATED_COMPONENT = is_related_component
                                        })
                                    End If

                                    ' Sultimo nserie consumo de acero menor
                                    If assigment("imprimirRestoAceroMenor") = 1 Then
                                        ' Comentado hasta que lo necesiten imprimir (ultimas cosas de Julen T_T)
                                        'result.Add(New Record_sap With {
                                        '    .RECORD_ID = result.Count + 1,
                                        '    .ACTION = "FINAL_ISSUE",
                                        '    .IS_EXECUTION_OA = is_execution_oa,
                                        '    .IS_RELATED_COMPONENT = Is_related_component
                                        '})
                                    End If
                                Next
                            Catch ex As Exception

                            End Try
                        End If
                        ' VALORES
                        If VALORES Then
                            Try
                                Dim id = row("id")
                                Dim dt_valores As DataTable = dtHPv_infoSAP_valores.Select("idHistoricoProduc = '" & row("id") & "'").CopyToDataTable
                                If dt_valores.Rows.Count > 0 Then
                                    Dim l_ct_summarized_data As List(Of Ct_summarized_data) = crear_l_ct_summarized_data(dt_valores)
                                    result.Add(New Record_sap With {
                                        .RECORD_ID = result.Count + 1,
                                        .ACTION = "EXEC_RECORDRESULTS",
                                        .IS_EXECUTION_OA = is_execution_oa,
                                        .IS_SERIALIZED_PRODUCT = is_serialized_product,
                                        .CT_SUMMARIZED_DATA = l_ct_summarized_data
                                    })
                                End If
                            Catch ex As Exception

                            End Try
                        End If
                        ' OP END
                        If OP_END Then
                            'If operacionAcero Then
                            '    If row("terminada") = 1 Or row("terminada") Then
                            '        result.Add(New Record_sap With {
                            '            .RECORD_ID = result.Count + 1,
                            '            .ACTION = "Z_CONFIRM1",
                            '            .IS_EXECUTION_OA = is_execution_oa,
                            '            .IS_SERIALIZED_PRODUCT = is_serialized_product
                            '        })
                            '    ElseIf row("terminada") = 0 Or Not row("terminada") Then
                            '        result.Add(New Record_sap With {
                            '            .RECORD_ID = result.Count + 1,
                            '            .ACTION = "SAP_SCRAP_IP",
                            '            .IS_EXECUTION_OA = is_execution_oa,
                            '            .IS_SERIALIZED_PRODUCT = is_serialized_product
                            '        })
                            '    End If
                            'Else
                            If row("terminada") = 1 Or row("terminada") Then
                                result.Add(New Record_sap With {
                                    .RECORD_ID = result.Count + 1,
                                    .ACTION = "Z_CONFIRM1_RC",
                                    .IS_EXECUTION_OA = is_execution_oa,
                                    .IS_SERIALIZED_PRODUCT = is_serialized_product,
                                    .IS_REASON_CODE = New Is_reason_code
                                })
                            ElseIf row("terminada") = 0 Or Not row("terminada") Then
                                result.Add(New Record_sap With {
                                    .RECORD_ID = result.Count + 1,
                                    .ACTION = "SAP_SCRAP_IP",
                                    .IS_EXECUTION_OA = is_execution_oa,
                                    .IS_SERIALIZED_PRODUCT = is_serialized_product,
                                    .IS_REASON_CODE = New Is_reason_code
                                })
                            End If
                        End If

                        ' TIEMPOS OPERARIOS
                        If LABOR_TIME Then
                            Try
                                Dim dt_tiempos_operarios As DataTable = dtHPv_infoSAP_tiempos_operarios.Select("id = '" & row("id") & "'").CopyToDataTable
                                For Each tiempoOperario In dt_tiempos_operarios.Rows
                                    Dim labor As Labor = crear_labor(tiempoOperario)
                                    result.Add(New Record_sap With {
                                        .RECORD_ID = result.Count + 1,
                                        .ACTION = "LABOUR_TIME",
                                        .IS_EXECUTION_OA = is_execution_oa,
                                        .IS_SERIALIZED_PRODUCT = is_serialized_product,
                                        .LABOR = labor
                                    })
                                Next
                            Catch ex As Exception

                            End Try
                        End If
                    Catch ex As Exception

                    End Try
                Next
            Catch ex As Exception

            End Try
        Next

        For Each row In dtSERIALNO.Rows

            Dim SERIALNO_list As SERIALNO_list = New SERIALNO_list With {
                .SERIALNO = row("SERIALNO"),
                .SERGE = row("nSerie")
            }
            result.Add(New Record_sap With {
                .RECORD_ID = result.Count + 1,
                .ACTION = "SERIAL_MAPPING",
                .ORDER = row("sl_order"),
                .MATERIAL = row("material"),
                .SERIAL_NUMBER_LIST = SERIALNO_list
            })

        Next


        Dim jsonResult As String = JsonConvert.SerializeObject(result, Formatting.Indented)
        jsonResult = jsonResult.Replace("," & System.Environment.NewLine & "    ""IS_EXECUTION_OA"": null", "")
        jsonResult = jsonResult.Replace("," & System.Environment.NewLine & "    ""IS_SERIALIZED_PRODUCT"": null", "")
        jsonResult = jsonResult.Replace("," & System.Environment.NewLine & "    ""CT_SUMMARIZED_DATA"": null", "")
        jsonResult = jsonResult.Replace("," & System.Environment.NewLine & "    ""IS_ASSEMBLY_DATA"": null", "")
        jsonResult = jsonResult.Replace("," & System.Environment.NewLine & "    ""LABOR"": null", "")
        jsonResult = jsonResult.Replace("," & System.Environment.NewLine & "    ""IS_RELATED_COMPONENT"": null", "")
        jsonResult = jsonResult.Replace("," & System.Environment.NewLine & "    ""IS_RELATED_COMPONENT"": null", "")
        jsonResult = jsonResult.Replace("," & System.Environment.NewLine & "    ""IS_REASON_CODE"": null", "")
        jsonResult = jsonResult.Replace("," & System.Environment.NewLine & "    ""ORDER"": null", "")
        jsonResult = jsonResult.Replace("," & System.Environment.NewLine & "    ""MATERIAL"": null", "")
        jsonResult = jsonResult.Replace("," & System.Environment.NewLine & "    ""SERIAL_NUMBER_LIST"": null", "")
        jsonResult = jsonResult.Replace("," & System.Environment.NewLine & "      ""SERIAL_NUMBER"": null", "")
        jsonResult = jsonResult.Replace("," & System.Environment.NewLine & "      ""BATCH"": null", "")
        jsonResult = jsonResult.Replace("," & System.Environment.NewLine & "        ""INSPECTIONRESULTMEANVALUE"": null", "")
        jsonResult = jsonResult.Replace("," & System.Environment.NewLine & "        ""CHARACTERISTICATTRIBUTECODE"": null", "")
        jsonResult = jsonResult.Replace("," & System.Environment.NewLine & "        ""CHARACTERISTICATTRIBUTECODEGRP"": null", "")

        If jsonResult = "[]" Then
            jsonResult = ""
        End If
        Return jsonResult
    End Function
    Private Shared Function crear_is_assembly_data(ByVal row As DataRow, serialNo As String, order As String, ruedasHierro As Boolean) As Is_assembly_data

        Dim quantity As String = row("quantity").ToString
        Dim uom As String = row("uom").ToString
        Dim material As String = row("material").ToString
        Dim plant As String = row("plant").ToString
        Dim storage_location As String = row("storage_location").ToString
        Dim stock_type As String = "U"
        Dim SLMat As String = row("SLMat").ToString
        Dim NserieMat As String = row("NserieMat").ToString

        Dim batch As String = Nothing
        Dim serial_number As String = Nothing

        Dim imprimir = True

        Select Case row("assembly_type_description").ToString.ToUpper
            Case "BATCH"
                batch = SLMat
                'If ruedasHierro Then
                '    batch = order
                'End If
                If batch = "" Then ' Si no tiene OF de material, se pone la colada que se asignaba antes de este cambio
                    Dim colada = serialNo
                    If serialNo.Contains("-") Then
                        Try
                            Dim coladaInt As Integer = serialNo.Split("-")(0)
                            colada = coladaInt
                        Catch ex As Exception
                            colada = serialNo.Split("-")(0)
                        End Try
                    End If
                    batch = colada
                End If
                If batch = "" Then ' REDUCTORAS
                    batch = NserieMat
                End If
                If ruedasHierro Then
                    batch = batch.Substring(batch.Length - 4)
                ElseIf batch <> "" And Not batch.StartsWith("8") Then
                    batch = "0000000000" & batch
                    batch = batch.Substring(batch.Length - 10)
                End If
                imprimir = (batch <> "")
            Case "SERIALIZED"
                serial_number = NserieMat
                imprimir = (serial_number <> "")
            Case "SERIALIZED WITH BATCH"
                batch = SLMat
                serial_number = NserieMat
                imprimir = (batch <> "" Or serial_number <> "")
            Case "MANUAL"
                'NADA pero si que se manda el bloque!
            Case Else
                'No eran solo 4 valores posibles? 
        End Select

        If imprimir Then
            Return New Is_assembly_data With {
                .QUANTITY = quantity,
                .UOM = uom,
                .MATERIAL = material,
                .PLANT = plant,
                .STORAGE_LOCATION = storage_location,
                .STOCK_TYPE = stock_type,
                .BATCH = batch,
                .SERIAL_NUMBER = serial_number
            }
        Else
            Return New Is_assembly_data With {.QUANTITY = -1}
        End If
    End Function
    Private Shared Function crear_is_related_component(ByVal row As DataRow) As Is_related_component

        Dim reservation As String = row("reservation").ToString
        Dim reservation_item As String = row("reservation_item").ToString

        Return New Is_related_component With {
            .RESERVATION = reservation,
            .RESERVATION_ITEM = reservation_item
        }

    End Function
    Private Shared Function crear_is_execution_oa(ByVal row As DataRow, codigoSap As String) As Is_execution_oa

        Dim primerGuion = codigoSap.IndexOf("-")

        Dim order As String = row("semanaLote").ToString
        Dim operation As String = codigoSap.Substring(0, primerGuion)
        Dim segment_type As String = 2
        Dim operacion_activity As String = codigoSap.Substring(primerGuion + 1)

        Return New Is_execution_oa With {
            .ORDER = order,
            .OPERATION = operation,
            .SEGMENT_TYPE = segment_type,
            .OPERATION_ACTIVITY = operacion_activity
        }

    End Function
    Private Shared Function crear_is_serialized_product(ByVal row As DataRow) As Is_serialized_product

        Dim material As String = row("plano").ToString
        Dim serial_number As String = ""
        If row("SERIALNO").ToString <> "" Then
            serial_number = row("SERIALNO").ToString
        ElseIf row("colada").ToString = "" Then
            serial_number = row("Nserie").ToString
        ElseIf row("colada").ToString = "" Then
            serial_number = row("colada").ToString
        Else
            serial_number = row("colada").ToString & "-" & row("Nserie").ToString
        End If

        Return New Is_serialized_product With {
            .MATERIAL = material,
            .SERIAL_NUMBER = serial_number
        }

    End Function
    Private Shared Function crear_l_ct_summarized_data(ByVal dt As DataTable) As List(Of Ct_summarized_data)

        Dim result = New List(Of Ct_summarized_data)

        For Each row In dt.Rows
            Dim primerGuion = row("nombreSap").ToString.IndexOf("-")
            If primerGuion > 0 Then
                Dim inspectioncharacteristic As String = row("nombreSap").ToString.Substring(0, primerGuion)
                Dim inpectionerultmeanvalue As String = Nothing
                Dim characteristicattributecode As String = Nothing
                Dim characteristicattributecodegrp As String = Nothing
                Select Case row("nombreSap").ToString.Replace(inspectioncharacteristic & "-", "").ToUpper
                    Case "CMGW001", "CMGA005", "CMGA006", "CMGA057", "CMGA063", "CMGA064", "CMGA065",
                         "CMGA109", "CMGW038", "CMGW039", "CMGW040", "CMGW042", "CMGW043",
                         "CMGW087", "CMGW006", "CMGW005", "CMGW041", "CMGA005", "CMGA006",
                         "CMGA007", "CMGA054", "CMGA057", "CMGA063", "CMGA065", "CMGA108",
                         "CMGA109", "CMGW007", "CMG0001" 'CONFIRMACION
                        If row("ok") = 1 Then
                            characteristicattributecode = "OK"
                        Else
                            characteristicattributecode = "NOK"
                        End If
                        characteristicattributecodegrp = "QUAL01"
                    Case "CMGW002", "CMGW003", "CMGW007", "CMGW010", "CMGW038",
                         "CMGW039", "CMGW040", "CMGW042" 'VALOR
                        inpectionerultmeanvalue = row("valor")
                End Select
                Dim inpectionvaluationresult As String = ""
                If row("ok") = 1 Then
                    inpectionvaluationresult = "A"
                Else
                    inpectionvaluationresult = "R"
                End If

                result.Add(New Ct_summarized_data With {
                    .INSPECTIONCHARACTERISTIC = inspectioncharacteristic,
                    .INSPECTIONRESULTMEANVALUE = inpectionerultmeanvalue,
                    .CHARACTERISTICATTRIBUTECODE = characteristicattributecode,
                    .CHARACTERISTICATTRIBUTECODEGRP = characteristicattributecodegrp,
                    .INSPECTIONVALUATIONRESULT = inpectionvaluationresult
                })
            End If
        Next

        Return result
    End Function
    Private Shared Function crear_labor(ByVal row As DataRow) As Labor

        Dim fechaInicio As DateTime = row("fechaIni")

        Dim exec_user As String = row("codigoOperario").ToString
        Dim action_time As String = fechaInicio.ToString("yyyyMMddHHmmss")
        Dim actl_exec_duration As String = row("duracion").ToString

        Return New Labor With {
            .EXEC_USER = exec_user,
            .ACTION_TIME = action_time,
            .ACTL_EXEC_DURATION = actl_exec_duration
        }

    End Function
#End Region

#Region "Helpers locales"
    ' Replica de AppCode.MyFunctions.listToString(List(Of Integer)): "1, 2, 3" y "-1" si vacia.
    Private Shared Function listToString(ByVal lista As List(Of Integer)) As String
        Dim result = ""

        For Each n In lista
            If result = "" Then
                result = n
            Else
                result &= ", " & n
            End If
        Next

        If result = "" Then
            result = "-1"
        End If
        Return result
    End Function
#End Region

End Class

#Region "ESTRUCTURA JSON DE SAP (DUPLICADO de crearSAP.vb - MANTENER SINCRONIZADO)"
Public Class Record_sap

    Public Property RECORD_ID As String
    Public Property ACTION As String
    Public Property IS_ASSEMBLY_DATA As Is_assembly_data
    Public Property IS_EXECUTION_OA As Is_execution_oa
    Public Property IS_SERIALIZED_PRODUCT As Is_serialized_product
    Public Property IS_REASON_CODE As Is_reason_code
    Public Property IS_RELATED_COMPONENT As Is_related_component
    Public Property CT_SUMMARIZED_DATA As List(Of Ct_summarized_data)
    Public Property LABOR As Labor
    Public Property ORDER As String
    Public Property MATERIAL As String
    Public Property SERIAL_NUMBER_LIST As SERIALNO_list

End Class
Public Class SERIALNO_list

    Public Property SERIALNO As String
    Public Property SERGE As String

End Class
Public Class Is_assembly_data

    Public Property QUANTITY As String
    Public Property UOM As String
    Public Property MATERIAL As String
    Public Property PLANT As String
    Public Property STORAGE_LOCATION As String
    Public Property STOCK_TYPE As String
    Public Property BATCH As String
    Public Property SERIAL_NUMBER As String


End Class
Public Class Is_related_component

    Public Property RESERVATION As String
    Public Property RESERVATION_ITEM As String


End Class
Public Class Is_execution_oa

    Public Property ORDER As String
    Public Property OPERATION As String
    Public Property SEGMENT_TYPE As String
    Public Property OPERATION_ACTIVITY As String

End Class
Public Class Is_serialized_product

    Public Property MATERIAL As String
    Public Property SERIAL_NUMBER As String

End Class
Public Class Is_reason_code

    Public Property REASON_CODE_CATALOG As String = "0"
    Public Property REASON_CODE_GROUP As String = "YP-ALL01"
    Public Property REASON_CODE As String = "M002"

End Class

Public Class Ct_summarized_data

    Public Property INSPECTIONCHARACTERISTIC As String
    Public Property INSPECTIONRESULTMEANVALUE As String
    Public Property CHARACTERISTICATTRIBUTECODE As String
    Public Property CHARACTERISTICATTRIBUTECODEGRP As String
    Public Property INSPECTIONVALUATIONRESULT As String

End Class
Public Class Labor

    Public Property EXEC_USER As String
    Public Property ACTION_TIME As String
    Public Property ACTL_EXEC_DURATION As String

End Class

#End Region
