<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Opciones
    Inherits System.Windows.Forms.Form

    'Form reemplaza a Dispose para limpiar la lista de componentes.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Requerido por el Diseñador de Windows Forms
    Private components As System.ComponentModel.IContainer

    'NOTA: el Diseñador de Windows Forms necesita el siguiente procedimiento
    'Se puede modificar usando el Diseñador de Windows Forms.  
    'No lo modifique con el editor de código.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Opciones))
        Me.Button2 = New System.Windows.Forms.Button()
        Me.carpetaOrigenPath = New System.Windows.Forms.TextBox()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.carpetaDestinoPath = New System.Windows.Forms.TextBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.minutosUpD = New System.Windows.Forms.NumericUpDown()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.Button3 = New System.Windows.Forms.Button()
        Me.minimizarCbx = New System.Windows.Forms.CheckBox()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.carpetaSAPPath = New System.Windows.Forms.TextBox()
        Me.Button4 = New System.Windows.Forms.Button()
        CType(Me.minutosUpD, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'Button2
        '
        Me.Button2.Location = New System.Drawing.Point(240, 71)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(132, 24)
        Me.Button2.TabIndex = 1
        Me.Button2.Text = "Seleccionar carpeta"
        Me.Button2.UseVisualStyleBackColor = True
        '
        'carpetaOrigenPath
        '
        Me.carpetaOrigenPath.Location = New System.Drawing.Point(12, 31)
        Me.carpetaOrigenPath.Name = "carpetaOrigenPath"
        Me.carpetaOrigenPath.Size = New System.Drawing.Size(222, 20)
        Me.carpetaOrigenPath.TabIndex = 2
        '
        'Button1
        '
        Me.Button1.Location = New System.Drawing.Point(240, 28)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(132, 24)
        Me.Button1.TabIndex = 3
        Me.Button1.Text = "Seleccionar carpeta"
        Me.Button1.UseVisualStyleBackColor = True
        '
        'carpetaDestinoPath
        '
        Me.carpetaDestinoPath.Location = New System.Drawing.Point(12, 74)
        Me.carpetaDestinoPath.Name = "carpetaDestinoPath"
        Me.carpetaDestinoPath.Size = New System.Drawing.Size(222, 20)
        Me.carpetaDestinoPath.TabIndex = 4
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(12, 15)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(197, 13)
        Me.Label1.TabIndex = 5
        Me.Label1.Text = "Carpeta de origen (para unir las planillas)"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(12, 58)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(197, 13)
        Me.Label2.TabIndex = 6
        Me.Label2.Text = "Carpeta de destino (con planillas unidas)"
        '
        'minutosUpD
        '
        Me.minutosUpD.Location = New System.Drawing.Point(12, 117)
        Me.minutosUpD.Maximum = New Decimal(New Integer() {120, 0, 0, 0})
        Me.minutosUpD.Name = "minutosUpD"
        Me.minutosUpD.Size = New System.Drawing.Size(121, 20)
        Me.minutosUpD.TabIndex = 7
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(12, 101)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(121, 13)
        Me.Label3.TabIndex = 8
        Me.Label3.Text = "Minutos hasta actualizar"
        '
        'Button3
        '
        Me.Button3.Location = New System.Drawing.Point(248, 205)
        Me.Button3.Name = "Button3"
        Me.Button3.Size = New System.Drawing.Size(131, 30)
        Me.Button3.TabIndex = 9
        Me.Button3.Text = "Guardar"
        Me.Button3.UseVisualStyleBackColor = True
        '
        'minimizarCbx
        '
        Me.minimizarCbx.AutoSize = True
        Me.minimizarCbx.CheckAlign = System.Drawing.ContentAlignment.MiddleRight
        Me.minimizarCbx.Checked = True
        Me.minimizarCbx.CheckState = System.Windows.Forms.CheckState.Checked
        Me.minimizarCbx.Location = New System.Drawing.Point(153, 118)
        Me.minimizarCbx.Name = "minimizarCbx"
        Me.minimizarCbx.Size = New System.Drawing.Size(197, 17)
        Me.minimizarCbx.TabIndex = 10
        Me.minimizarCbx.Text = "Minimización en bandeja del sistema"
        Me.minimizarCbx.UseVisualStyleBackColor = True
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(12, 148)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(220, 13)
        Me.Label4.TabIndex = 11
        Me.Label4.Text = "Carpeta de SAP (dejar el JSON de ruedas)"
        '
        'carpetaSAPPath
        '
        Me.carpetaSAPPath.Location = New System.Drawing.Point(12, 164)
        Me.carpetaSAPPath.Name = "carpetaSAPPath"
        Me.carpetaSAPPath.Size = New System.Drawing.Size(222, 20)
        Me.carpetaSAPPath.TabIndex = 12
        '
        'Button4
        '
        Me.Button4.Location = New System.Drawing.Point(240, 161)
        Me.Button4.Name = "Button4"
        Me.Button4.Size = New System.Drawing.Size(132, 24)
        Me.Button4.TabIndex = 13
        Me.Button4.Text = "Seleccionar carpeta"
        Me.Button4.UseVisualStyleBackColor = True
        '
        'Opciones
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(391, 250)
        Me.Controls.Add(Me.Button4)
        Me.Controls.Add(Me.carpetaSAPPath)
        Me.Controls.Add(Me.Label4)
        Me.Controls.Add(Me.minimizarCbx)
        Me.Controls.Add(Me.Button3)
        Me.Controls.Add(Me.Label3)
        Me.Controls.Add(Me.minutosUpD)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.carpetaDestinoPath)
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.carpetaOrigenPath)
        Me.Controls.Add(Me.Button2)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MaximizeBox = False
        Me.Name = "Opciones"
        Me.Text = "Opciones"
        CType(Me.minutosUpD, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents Button2 As Button
    Friend WithEvents carpetaOrigenPath As TextBox
    Friend WithEvents Button1 As Button
    Friend WithEvents carpetaDestinoPath As TextBox
    Friend WithEvents Label1 As Label
    Friend WithEvents Label2 As Label
    Friend WithEvents minutosUpD As NumericUpDown
    Friend WithEvents Label3 As Label
    Friend WithEvents Button3 As Button
    Friend WithEvents minimizarCbx As CheckBox
    Friend WithEvents Label4 As Label
    Friend WithEvents carpetaSAPPath As TextBox
    Friend WithEvents Button4 As Button
End Class
