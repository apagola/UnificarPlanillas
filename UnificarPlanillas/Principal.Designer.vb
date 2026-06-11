<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Principal
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
        Me.components = New System.ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Principal))
        Me.actualizarBtn = New System.Windows.Forms.Button()
        Me.PictureBox1 = New System.Windows.Forms.PictureBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.cuentaAtrasLbl = New System.Windows.Forms.Label()
        Me.MenuStrip1 = New System.Windows.Forms.MenuStrip()
        Me.opcionesToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.Spr = New System.Windows.Forms.Label()
        Me.estadoLbl = New System.Windows.Forms.Label()
        Me.NotifyIcon1 = New System.Windows.Forms.NotifyIcon(Me.components)
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.MenuStrip1.SuspendLayout()
        Me.SuspendLayout()
        '
        'actualizarBtn
        '
        Me.actualizarBtn.Location = New System.Drawing.Point(74, 212)
        Me.actualizarBtn.Name = "actualizarBtn"
        Me.actualizarBtn.Size = New System.Drawing.Size(187, 53)
        Me.actualizarBtn.TabIndex = 0
        Me.actualizarBtn.Text = "Actualizar"
        Me.actualizarBtn.UseVisualStyleBackColor = True
        '
        'PictureBox1
        '
        Me.PictureBox1.BackgroundImage = CType(resources.GetObject("PictureBox1.BackgroundImage"), System.Drawing.Image)
        Me.PictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.PictureBox1.Location = New System.Drawing.Point(12, 61)
        Me.PictureBox1.Name = "PictureBox1"
        Me.PictureBox1.Size = New System.Drawing.Size(316, 78)
        Me.PictureBox1.TabIndex = 1
        Me.PictureBox1.TabStop = False
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(22, 180)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(146, 13)
        Me.Label1.TabIndex = 2
        Me.Label1.Text = "Actualización de planillas en: "
        '
        'cuentaAtrasLbl
        '
        Me.cuentaAtrasLbl.AutoSize = True
        Me.cuentaAtrasLbl.Location = New System.Drawing.Point(212, 180)
        Me.cuentaAtrasLbl.Name = "cuentaAtrasLbl"
        Me.cuentaAtrasLbl.Size = New System.Drawing.Size(49, 13)
        Me.cuentaAtrasLbl.TabIndex = 3
        Me.cuentaAtrasLbl.Text = "00:00:00"
        '
        'MenuStrip1
        '
        Me.MenuStrip1.BackColor = System.Drawing.SystemColors.MenuBar
        Me.MenuStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.opcionesToolStripMenuItem})
        Me.MenuStrip1.Location = New System.Drawing.Point(0, 0)
        Me.MenuStrip1.Name = "MenuStrip1"
        Me.MenuStrip1.Size = New System.Drawing.Size(342, 24)
        Me.MenuStrip1.TabIndex = 4
        Me.MenuStrip1.Text = "MenuStrip1"
        '
        'opcionesToolStripMenuItem
        '
        Me.opcionesToolStripMenuItem.Name = "opcionesToolStripMenuItem"
        Me.opcionesToolStripMenuItem.Size = New System.Drawing.Size(69, 20)
        Me.opcionesToolStripMenuItem.Text = "Opciones"
        '
        'Spr
        '
        Me.Spr.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.Spr.Location = New System.Drawing.Point(10, 282)
        Me.Spr.Name = "Spr"
        Me.Spr.Size = New System.Drawing.Size(318, 2)
        Me.Spr.TabIndex = 5
        '
        'estadoLbl
        '
        Me.estadoLbl.AutoSize = True
        Me.estadoLbl.Location = New System.Drawing.Point(7, 289)
        Me.estadoLbl.Name = "estadoLbl"
        Me.estadoLbl.Size = New System.Drawing.Size(67, 13)
        Me.estadoLbl.TabIndex = 6
        Me.estadoLbl.Text = "Esperando..."
        '
        'NotifyIcon1
        '
        Me.NotifyIcon1.Icon = CType(resources.GetObject("NotifyIcon1.Icon"), System.Drawing.Icon)
        Me.NotifyIcon1.Text = "Unificador planillas"
        Me.NotifyIcon1.Visible = True
        '
        'Principal
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(342, 311)
        Me.Controls.Add(Me.estadoLbl)
        Me.Controls.Add(Me.Spr)
        Me.Controls.Add(Me.cuentaAtrasLbl)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.PictureBox1)
        Me.Controls.Add(Me.actualizarBtn)
        Me.Controls.Add(Me.MenuStrip1)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MainMenuStrip = Me.MenuStrip1
        Me.MaximizeBox = False
        Me.Name = "Principal"
        Me.Text = "Unificador planillas"
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.MenuStrip1.ResumeLayout(False)
        Me.MenuStrip1.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents actualizarBtn As Button
    Friend WithEvents PictureBox1 As PictureBox
    Friend WithEvents Label1 As Label
    Friend WithEvents cuentaAtrasLbl As Label
    Friend WithEvents MenuStrip1 As MenuStrip
    Friend WithEvents opcionesToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents Spr As Label
    Friend WithEvents estadoLbl As Label
    Friend WithEvents NotifyIcon1 As NotifyIcon
End Class
