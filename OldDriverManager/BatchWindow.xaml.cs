using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using System.IO;
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
            openFileDialog.Title = "请选择文件";
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
    }
}
