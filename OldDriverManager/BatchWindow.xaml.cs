using MahApps.Metro.Controls;
using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Path = System.IO.Path;

namespace OldDriverManager
{
    /// <summary>
    /// BatchWindow.xaml 的交互逻辑
    /// </summary>
    public delegate void MetadataListDelegate(List<Metadata> metadataList);
    public partial class BatchWindow : MetroWindow
    {
        public MetadataListDelegate metadataListlDelegate;

        List<Metadata> returnMetadataList = new();
        public BatchWindow(List<Metadata> metadataList)
        {
            InitializeComponent();
            returnMetadataList = metadataList;
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = Application.Current.Resources["SelectFile"].ToString();
            openFileDialog.Multiselect = true;
            if ((bool)openFileDialog.ShowDialog())
            {
                foreach (string filePath in openFileDialog.FileNames)
                {
                    if (!returnMetadataList.Any(item => item.path == filePath))
                    {
                        string title = Path.GetFileNameWithoutExtension(filePath);
                        Metadata newMetadata = new Metadata(title, filePath, 0, 0, new List<string>(), new List<string>());
                        returnMetadataList.Add(newMetadata);
                    }
                }
            }
            metadataListlDelegate(returnMetadataList);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            List<Metadata> tempMetadataList = new();
            foreach (Metadata metadata in returnMetadataList)
            {
                if (!File.Exists(metadata.path))
                {
                    tempMetadataList.Add(metadata);
                }
            }
            tempMetadataList.ForEach(metadata => returnMetadataList.Remove(metadata));

            metadataListlDelegate(returnMetadataList);
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = Application.Current.Resources["SaveAs"].ToString();
            saveFileDialog.Filter = "Text file (*.txt)|*.txt";
            if ((bool)saveFileDialog.ShowDialog())
            {
                string listStr = "";
                foreach (Metadata item in returnMetadataList)
                {
                    listStr += item.title + "\n";
                }
                File.WriteAllText(saveFileDialog.FileName, listStr);
            }
        }
    }
}
