'Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System.Collections.ObjectModel
Imports System.Windows
Imports System.Windows.Controls
Imports Microsoft.WindowsAPICodePack.Shell

Namespace Microsoft.WindowsAPICodePack.Samples.KnownFoldersBrowser
	''' <summary>
	''' Interaction logic for Window1.xaml
	''' </summary>
	Partial Public Class Window1
		Inherits Window
		Public Sub New()
			InitializeComponent()
		End Sub

		Private Sub NavigateExplorerBrowser(ByVal sender As Object, ByVal args As SelectionChangedEventArgs)
			' TODO - Make XAML only
			' There is currently no way to get all the KnownFolder properties in a collection
			' that can be use for binding to a listbox. Create our own properties collection with name/value pairs
			Dim kf As IKnownFolder = TryCast((TryCast(sender, ListBox)).SelectedItem, IKnownFolder)

            Dim properties As New Collection(Of KnownFolderProperty)()
            properties.Add(New KnownFolderProperty("Canonical Name", kf.CanonicalName))
            properties.Add(New KnownFolderProperty("Category", kf.Category.ToString()))
            properties.Add(New KnownFolderProperty("Definition Options", kf.DefinitionOptions.ToString()))
            properties.Add(New KnownFolderProperty("Description", kf.Description))
            properties.Add(New KnownFolderProperty("File Attributes", kf.FileAttributes.ToString()))
            properties.Add(New KnownFolderProperty("Folder Id", kf.FolderId.ToString()))
            properties.Add(New KnownFolderProperty("Folder Type", kf.FolderType))
            properties.Add(New KnownFolderProperty("Folder Type Id", kf.FolderTypeId.ToString()))
            properties.Add(New KnownFolderProperty("Localized Name", kf.LocalizedName))
            properties.Add(New KnownFolderProperty("Localized Name Resource Id", kf.LocalizedNameResourceId))
            properties.Add(New KnownFolderProperty("Parent Id", kf.ParentId.ToString()))
            properties.Add(New KnownFolderProperty("ParsingName", kf.ParsingName))
            properties.Add(New KnownFolderProperty("Path", kf.Path))
            properties.Add(New KnownFolderProperty("Relative Path", kf.RelativePath))
            properties.Add(New KnownFolderProperty("Redirection", kf.Redirection.ToString()))
            properties.Add(New KnownFolderProperty("Security", kf.Security))
            properties.Add(New KnownFolderProperty("Tooltip", kf.Tooltip))
            properties.Add(New KnownFolderProperty("Tooltip Resource Id", kf.TooltipResourceId))

			' Bind the collection to the properties listbox.
			PropertiesListBox.ItemsSource = properties

		End Sub
	End Class

    Friend Structure KnownFolderProperty

        Public Sub New(ByVal prop As String, ByVal val As String)
            PropertyName = prop
            Value = val
        End Sub

        Private privateProperty As String
        Public Property PropertyName() As String
            Get
                Return privateProperty
            End Get
            Set(ByVal value As String)
                privateProperty = value
            End Set
        End Property
        Private privateValue As Object
        Public Property Value() As Object
            Get
                Return privateValue
            End Get
            Set(ByVal value As Object)
                privateValue = value
            End Set
        End Property
    End Structure
End Namespace
