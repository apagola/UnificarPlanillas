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

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        If IO.Directory.Exists(carpetaOrigenPath.Text) And IO.Directory.Exists(carpetaDestinoPath.Text) Then
            My.Settings.origen = carpetaOrigenPath.Text
            My.Settings.destino = carpetaDestinoPath.Text
            My.Settings.tiempoAct = minutosUpD.Value
            My.Settings.minimizar = minimizarCbx.Checked
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
    End Sub
End Class