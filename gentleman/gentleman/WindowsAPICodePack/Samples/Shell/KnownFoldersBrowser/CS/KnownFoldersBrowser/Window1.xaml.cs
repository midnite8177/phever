//Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Shell;

namespace Microsoft.WindowsAPICodePack.Samples.KnownFoldersBrowser
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
        }

        void NavigateExplorerBrowser(object sender, SelectionChangedEventArgs args)
        {
            // TODO - Make XAML only
            // There is currently no way to get all the KnownFolder properties in a collection
            // that can be use for binding to a listbox. Create our own properties collection with name/value pairs
            IKnownFolder kf = (sender as ListBox).SelectedItem as IKnownFolder;

            Collection<KnownFolderProperty> properties = new Collection<KnownFolderProperty>();
            properties.Add(new KnownFolderProperty { Property = "Canonical Name", Value = kf.CanonicalName });
            properties.Add(new KnownFolderProperty { Property = "Category", Value = kf.Category });
            properties.Add(new KnownFolderProperty { Property = "Definition Options", Value = kf.DefinitionOptions });
            properties.Add(new KnownFolderProperty { Property = "Description", Value = kf.Description });
            properties.Add(new KnownFolderProperty { Property = "File Attributes", Value = kf.FileAttributes });
            properties.Add(new KnownFolderProperty { Property = "Folder Id", Value = kf.FolderId });
            properties.Add(new KnownFolderProperty { Property = "Folder Type", Value = kf.FolderType });
            properties.Add(new KnownFolderProperty { Property = "Folder Type Id", Value = kf.FolderTypeId });
            properties.Add(new KnownFolderProperty { Property = "Localized Name", Value = kf.LocalizedName });
            properties.Add(new KnownFolderProperty { Property = "Localized Name Resource Id", Value = kf.LocalizedNameResourceId });
            properties.Add(new KnownFolderProperty { Property = "Parent Id", Value = kf.ParentId });
            properties.Add(new KnownFolderProperty { Property = "ParsingName", Value = kf.ParsingName });
            properties.Add(new KnownFolderProperty { Property = "Path", Value = kf.Path });
            properties.Add(new KnownFolderProperty { Property = "Relative Path", Value = kf.RelativePath });
            properties.Add(new KnownFolderProperty { Property = "Redirection", Value = kf.Redirection });
            properties.Add(new KnownFolderProperty { Property = "Security", Value = kf.Security });
            properties.Add(new KnownFolderProperty { Property = "Tooltip", Value = kf.Tooltip });
            properties.Add(new KnownFolderProperty { Property = "Tooltip Resource Id", Value = kf.TooltipResourceId });

            // Bind the collection to the properties listbox.
            PropertiesListBox.ItemsSource = properties;

        }
    }

    struct KnownFolderProperty
    {
        public string Property { set; get; }
        public object Value { set; get; }
    }

}
