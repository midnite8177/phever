' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.IO
Imports System.Windows.Forms
Imports Microsoft.WindowsAPICodePack.Shell
Imports Microsoft.WindowsAPICodePack.Taskbar
Imports System.Reflection

Namespace TaskbarDemo
	Partial Public Class ChildDocument
		Inherits Form
		' Keep a reference to the Taskbar instance
		Private windowsTaskbar As TaskbarManager = TaskbarManager.Instance

		Private childWindowJumpList As JumpList
		Private childWindowAppId As String

		Public Sub New(ByVal count As Integer)
			childWindowAppId = "TaskbarDemo.ChildWindow" & count

			InitializeComponent()

			' Progress Bar
			For Each state As String In System.Enum.GetNames(GetType(TaskbarProgressBarState))
				comboBoxProgressBarStates.Items.Add(state)
			Next state

			'
			comboBoxProgressBarStates.SelectedItem = "NoProgress"

			AddHandler Shown, AddressOf ChildDocument_Shown
		End Sub

		Private Sub ChildDocument_Shown(ByVal sender As Object, ByVal e As EventArgs)
			' Set our default
			windowsTaskbar.SetProgressState(TaskbarProgressBarState.NoProgress, Me.Handle)
		End Sub

		#Region "Progress Bar"

		Private Sub trackBar1_Scroll(ByVal sender As Object, ByVal e As EventArgs) Handles trackBar1.Scroll
			' When the user changes the trackBar value,
			' update the progress bar in our UI as well as Taskbar
			progressBar1.Value = trackBar1.Value

			windowsTaskbar.SetProgressValue(trackBar1.Value, 100, Me.Handle)
		End Sub


		Private Sub comboBoxProgressBarStates_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles comboBoxProgressBarStates.SelectedIndexChanged
			' Update the status of the taskbar progress bar

			Dim state As TaskbarProgressBarState = CType(System.Enum.Parse(GetType(TaskbarProgressBarState), CStr(comboBoxProgressBarStates.SelectedItem)), TaskbarProgressBarState)

			windowsTaskbar.SetProgressState(state, Me.Handle)

			' Update the application progress bar,
			' as well disable the trackbar in some cases
			Select Case state
				Case TaskbarProgressBarState.Normal
					If trackBar1.Value = 0 Then
						trackBar1.Value = 20
						progressBar1.Value = trackBar1.Value
					End If

					progressBar1.Style = ProgressBarStyle.Continuous
					windowsTaskbar.SetProgressValue(trackBar1.Value, 100, Me.Handle)
					trackBar1.Enabled = True
				Case TaskbarProgressBarState.Paused
					If trackBar1.Value = 0 Then
						trackBar1.Value = 20
						progressBar1.Value = trackBar1.Value
					End If

					progressBar1.Style = ProgressBarStyle.Continuous
					windowsTaskbar.SetProgressValue(trackBar1.Value, 100, Me.Handle)
					trackBar1.Enabled = True
				Case TaskbarProgressBarState.Error
					If trackBar1.Value = 0 Then
						trackBar1.Value = 20
						progressBar1.Value = trackBar1.Value
					End If

					progressBar1.Style = ProgressBarStyle.Continuous
					windowsTaskbar.SetProgressValue(trackBar1.Value, 100, Me.Handle)
					trackBar1.Enabled = True
				Case TaskbarProgressBarState.Indeterminate
					progressBar1.Style = ProgressBarStyle.Marquee
					progressBar1.MarqueeAnimationSpeed = 30
					trackBar1.Enabled = False
				Case TaskbarProgressBarState.NoProgress
					progressBar1.Value = 0
					trackBar1.Value = 0
					progressBar1.Style = ProgressBarStyle.Continuous
					trackBar1.Enabled = False
			End Select
		End Sub

		#End Region

		#Region "Icon Overlay"

		Private Sub labelNoIconOverlay_Click(ByVal sender As Object, ByVal e As EventArgs) Handles labelNoIconOverlay.Click
			windowsTaskbar.SetOverlayIcon(Me.Handle, Nothing, Nothing)

			' 
			labelNoIconOverlay.BorderStyle = BorderStyle.Fixed3D
			pictureIconOverlay1.BorderStyle = BorderStyle.None
			pictureIconOverlay2.BorderStyle = BorderStyle.None
			pictureIconOverlay3.BorderStyle = BorderStyle.None
		End Sub

		Private Sub pictureIconOverlay1_Click(ByVal sender As Object, ByVal e As EventArgs) Handles pictureIconOverlay1.Click
			windowsTaskbar.SetOverlayIcon(Me.Handle, My.Resources.Green, "Green")

			'
			pictureIconOverlay1.BorderStyle = BorderStyle.Fixed3D
			labelNoIconOverlay.BorderStyle = BorderStyle.None
			pictureIconOverlay2.BorderStyle = BorderStyle.None
			pictureIconOverlay3.BorderStyle = BorderStyle.None
		End Sub

		Private Sub pictureIconOverlay2_Click(ByVal sender As Object, ByVal e As EventArgs) Handles pictureIconOverlay2.Click
			windowsTaskbar.SetOverlayIcon(Me.Handle, My.Resources.Yellow, "Yellow")

			'
			pictureIconOverlay2.BorderStyle = BorderStyle.Fixed3D
			labelNoIconOverlay.BorderStyle = BorderStyle.None
			pictureIconOverlay1.BorderStyle = BorderStyle.None
			pictureIconOverlay3.BorderStyle = BorderStyle.None
		End Sub

		Private Sub pictureIconOverlay3_Click(ByVal sender As Object, ByVal e As EventArgs) Handles pictureIconOverlay3.Click
			windowsTaskbar.SetOverlayIcon(Me.Handle, My.Resources.Red, "Red")

			'
			pictureIconOverlay3.BorderStyle = BorderStyle.Fixed3D
			labelNoIconOverlay.BorderStyle = BorderStyle.None
			pictureIconOverlay1.BorderStyle = BorderStyle.None
			pictureIconOverlay2.BorderStyle = BorderStyle.None
		End Sub

		#End Region

		Private Sub buttonRefreshTaskbarList_Click(ByVal sender As Object, ByVal e As EventArgs) Handles buttonRefreshTaskbarList.Click
			childWindowJumpList.Refresh()
		End Sub

		Private Sub button1_Click(ByVal sender As Object, ByVal e As EventArgs) Handles button1.Click
			childWindowJumpList = JumpList.CreateJumpListForIndividualWindow(childWindowAppId, Me.Handle)

			CType(sender, Button).Enabled = False
			groupBoxCustomCategories.Enabled = True
			buttonRefreshTaskbarList.Enabled = True
		End Sub

		Private Sub buttonUserTasksAddTasks_Click_1(ByVal sender As Object, ByVal e As EventArgs) Handles buttonUserTasksAddTasks.Click
			' Start from an empty list for user tasks
			childWindowJumpList.ClearAllUserTasks()

			' Path to Windows system folder
			Dim systemFolder As String = Environment.GetFolderPath(Environment.SpecialFolder.System)

			' Path to the Program Files folder
			Dim programFilesFolder As String = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)

			' Path to Windows folder
			Dim windowsFolder As String = Environment.GetEnvironmentVariable("windir")

			For Each item As Object In listBox1.SelectedItems
				Select Case item.ToString()
					Case "Notepad"
						childWindowJumpList.AddUserTasks(New JumpListLink(Path.Combine(systemFolder, "notepad.exe"), "Open Notepad") With {.IconReference = New IconReference(Path.Combine(systemFolder, "notepad.exe"), 0)})
					Case "Calculator"
						childWindowJumpList.AddUserTasks(New JumpListLink(Path.Combine(systemFolder, "calc.exe"), "Open Calculator") With {.IconReference = New IconReference(Path.Combine(systemFolder, "calc.exe"), 0)})
					Case "Paint"
						childWindowJumpList.AddUserTasks(New JumpListLink(Path.Combine(systemFolder, "mspaint.exe"), "Open Paint") With {.IconReference = New IconReference(Path.Combine(systemFolder, "mspaint.exe"), 0)})
					Case "WordPad"
						childWindowJumpList.AddUserTasks(New JumpListLink(Path.Combine(programFilesFolder, "Windows NT\Accessories\wordpad.exe"), "Open WordPad") With {.IconReference = New IconReference(Path.Combine(programFilesFolder, "Windows NT\Accessories\wordpad.exe"), 0)})
					Case "Windows Explorer"
						childWindowJumpList.AddUserTasks(New JumpListLink(Path.Combine(windowsFolder, "explorer.exe"), "Open Windows Explorer") With {.IconReference = New IconReference(Path.Combine(windowsFolder, "explorer.exe"), 0)})
					Case "Internet Explorer"
						childWindowJumpList.AddUserTasks(New JumpListLink(Path.Combine(programFilesFolder, "Internet Explorer\iexplore.exe"), "Open Internet Explorer") With {.IconReference = New IconReference(Path.Combine(programFilesFolder, "Internet Explorer\iexplore.exe"), 0)})
					Case "Control Panel"
                        childWindowJumpList.AddUserTasks(New JumpListLink((CType(KnownFolders.ControlPanel, ShellObject)).ParsingName, "Open Control Panel") With {.IconReference = New IconReference(Path.Combine(windowsFolder, "explorer.exe"), 0)})
                    Case "Documents Library"
                        If ShellLibrary.IsPlatformSupported Then
                            childWindowJumpList.AddUserTasks(New JumpListLink(KnownFolders.DocumentsLibrary.Path, "Open Documents Library") With {.IconReference = New IconReference(Path.Combine(windowsFolder, "explorer.exe"), 0)})
                        End If
                End Select
			Next item
		End Sub
	End Class
End Namespace
