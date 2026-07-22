Public Class Opciones
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim carpetaEntrada As FolderBrowserDialog = New FolderBrowserDialog

        If carpetaEntrada.ShowDialog = DialogResult.OK Then
            carpetaOrigenPath.Text = carpetaEntrada.SelectedPath
        End If
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Dim carpetaSalida As FolderBrowserDialog = New FolderBrowserDialog

        If carpetaSalida.ShowDialog = DialogResult.OK Then
            carpetaDestinoPath.Text = carpetaSalida.SelectedPath
        End If
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        Dim carpetaSap As FolderBrowserDialog = New FolderBrowserDialog

        If carpetaSap.ShowDialog = DialogResult.OK Then
            carpetaSAPPath.Text = carpetaSap.SelectedPath
        End If
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        ' El origen admite varias carpetas separadas por ";", por lo que hay que validar cada una por separado.
        Dim origenes = carpetaOrigenPath.Text.Split(";"c) _
                        .Select(Function(r) r.Trim()) _
                        .Where(Function(r) r <> "") _
                        .ToArray()

        Dim origenOk = origenes.Length > 0 AndAlso origenes.All(Function(r) IO.Directory.Exists(r))
        Dim destinoOk = IO.Directory.Exists(carpetaDestinoPath.Text.Trim())

        If origenOk And destinoOk Then
            My.Settings.origen = String.Join(";", origenes)
            My.Settings.destino = carpetaDestinoPath.Text.Trim()
            My.Settings.tiempoAct = minutosUpD.Value
            My.Settings.minimizar = minimizarCbx.Checked
            My.Settings.carpetaSAP = carpetaSAPPath.Text.Trim()
            My.Settings.Save()
            MessageBox.Show("Configuración guardada con éxito", "Configuración correcta", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Me.Close()
        Else
            MessageBox.Show("La ruta de la carpeta de origen o destino no existe o es inválida.", "Configuración errónea", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End If
    End Sub

    Private Sub Opciones_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        carpetaOrigenPath.Text = My.Settings.origen
        carpetaDestinoPath.Text = My.Settings.destino
        minutosUpD.Value = My.Settings.tiempoAct
        minimizarCbx.Checked = My.Settings.minimizar
        carpetaSAPPath.Text = My.Settings.carpetaSAP
    End Sub
End Class