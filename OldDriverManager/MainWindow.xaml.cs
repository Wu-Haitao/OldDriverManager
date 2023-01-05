using FFMpegCore;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MediaInfo;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Path = System.IO.Path;
using Size = System.Drawing.Size;

namespace OldDriverManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        List<Metadata> metadataList = new();

        BindingList<string> titles = new();

        int filterMarkNumber = 0;


        public MainWindow()
        {
            InitializeComponent();
            Init();
            UpdateLanguage();
            RefreshDataFromFile();
            ResetInfo();
        }

        void Init()
        {
            TitleList.ItemContainerGenerator.StatusChanged += ItemContainerGenerator_StatusChanged;
        }

        private void UpdateLanguage()
        {
            string[] supportedLang = new string[2] { "zh", "en" };
            string lang = File.ReadAllText(Properties.Settings.Default.LangFileName);
            if (!supportedLang.Contains(lang))
            {
                lang = supportedLang[0]; //Default
            }

            ResourceDictionary resDict = new ResourceDictionary();
            resDict.Source = new Uri(String.Format(@"..\Resource\StringResources.{0}.xaml", lang), UriKind.Relative);
            Application.Current.Resources.MergedDictionaries.Add(resDict);
        }

        private void ItemContainerGenerator_StatusChanged(object sender, EventArgs e)
        {
            if (TitleList.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
            {
                MarkFilteredResult();
            }
        }

        /// <summary>
        /// Refreshes the listbox based on the list of metadata
        /// </summary>
        void RefreshData()
        {
            titles.Clear();
            metadataList.ForEach(metadata => titles.Add(metadata.title));
        }

        /// <summary>
        /// Translates the JSON file, creates the list of metadata, and refresh the listbox
        /// </summary>
        void RefreshDataFromFile()
        {
            TitleList.ItemsSource = titles;

            Task task = new Task(() =>
            {
                try
                {
                    StreamReader streamReader = new(Path.Combine(Properties.Settings.Default.RootPath, Properties.Settings.Default.DataFileName));
                    string json = streamReader.ReadToEnd();
                    metadataList = JsonConvert.DeserializeObject<List<Metadata>>(json);
                    streamReader.Close();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            });
            task.Start();
            task.ContinueWith(t =>
            {
                titles.Clear();
                SortByName();
                metadataList.ForEach(metadata => titles.Add(metadata.title));
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        /// <summary>
        /// Writes the list of metadata stored in memory into the JSON file
        /// </summary>
        void WriteDataToFile()
        {
            Task task = new Task(() =>
            {
                try
                {
                    if (!Directory.Exists(Properties.Settings.Default.RootPath))
                    {
                        Directory.CreateDirectory(Properties.Settings.Default.RootPath);
                    }

                    StreamWriter streamWriter = new(Path.Combine(Properties.Settings.Default.RootPath, Properties.Settings.Default.DataFileName));
                    string json = JsonConvert.SerializeObject(metadataList);
                    streamWriter.Write(json);
                    streamWriter.Close();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            });
            task.Start();
            task.ContinueWith(t =>
            {
                RefreshDataFromFile();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void Data_Click(object sender, RoutedEventArgs e)
        {
            ConfigWindow configWindow = new ConfigWindow();
            configWindow.ShowDialog();
        }

        private void Batch_Click(object sender, RoutedEventArgs e)
        {
            BatchWindow batchWindow = new BatchWindow(metadataList);
            batchWindow.metadataListlDelegate = new MetadataListDelegate(BatchAdd);
            batchWindow.ShowDialog();
        }

        void BatchAdd(List<Metadata> newMetadataList)
        {
            metadataList = newMetadataList;
            WriteDataToFile();
            ResetInfo();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            EditWindow editWindow = new EditWindow();
            editWindow.Title = Application.Current.Resources["AddWindow"].ToString();
            editWindow.metadataDelegate += new MetadataDelegate(EditMetadata);
            editWindow.ShowDialog();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (TitleList.SelectedIndex == -1) return;
            Metadata selectedMetadata = metadataList[TitleList.SelectedIndex];
            metadataList.RemoveAt(TitleList.SelectedIndex);
            EditWindow editWindow = new EditWindow(selectedMetadata, TitleList.SelectedIndex);
            editWindow.Title = Application.Current.Resources["EditWindow"].ToString();
            editWindow.metadataDelegate += new MetadataDelegate(EditMetadata);
            editWindow.ShowDialog();
        }

        void EditMetadata(Metadata? metadata, int index)
        {
            if (metadata == null)
            {
                Debug.WriteLine("Something went wrong");
            }
            else
            {
                //Check if the file already exists
                if (metadataList.Any(item => item.path == metadata.path))
                {
                    this.ShowMessageAsync(Application.Current.Resources["SameFileExist"].ToString(), "");
                }
                else
                {
                    if (index == -1)
                    {
                        //Add
                        metadataList.Add(metadata);
                    }
                    else
                    {
                        metadataList.Insert(index, metadata);
                    }
                    WriteDataToFile();
                }
            }

            ResetInfo();
        }

        private void OpenPath_Click(object sender, RoutedEventArgs e)
        {
            if (TitleList.SelectedIndex == -1) return;
            Metadata selectedMetadata = metadataList[TitleList.SelectedIndex];
            if (File.Exists(selectedMetadata.path))
            {
                string argument = "/select, \"" + selectedMetadata.path + "\"";
                Process.Start("explorer.exe", argument);
            }
            else
            {
                this.ShowMessageAsync(Application.Current.Resources["ErrorInPath"].ToString(), "");
            }
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (TitleList.SelectedIndex == -1) return;
            Metadata selectedMetadata = metadataList[TitleList.SelectedIndex];
            if (File.Exists(selectedMetadata.path))
            {
                Process.Start("explorer.exe", selectedMetadata.path);
            }
            else
            {
                this.ShowMessageAsync(Application.Current.Resources["ErrorInPath"].ToString(), "");
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (TitleList.SelectedIndex == -1) return;

            MessageDialogResult result = await this.ShowMessageAsync(Application.Current.Resources["DeletionConfirm"].ToString(), "", MessageDialogStyle.AffirmativeAndNegative);
            if (result == MessageDialogResult.Negative) return;

            //if (MessageBox.Show("确定要删除吗？", "确认", MessageBoxButton.YesNo) == MessageBoxResult.No) return;

            metadataList.RemoveAt(TitleList.SelectedIndex);
            WriteDataToFile();

            ResetInfo();
        }

        private async void TitleList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TitleList.SelectedIndex == -1)
            {
                ResetInfo();
                return;
            }

            this.Cursor = Cursors.Wait;

            int currentIndex = TitleList.SelectedIndex;

            Metadata selectedMetadata = metadataList[TitleList.SelectedIndex];

            FileTitle.Text = selectedMetadata.title;

            string fileDetail = "";
            foreach (string actor in selectedMetadata.actors)
            {
                fileDetail += actor + System.Environment.NewLine;
            }
            foreach (string tag in selectedMetadata.tags)
            {
                fileDetail += tag + System.Environment.NewLine;
            }
            FileDetails.Text = fileDetail;

            Rank.Value = selectedMetadata.rank;

            FileProperty.Text = Application.Current.Resources["Loading"] + "......";

            Poster.Source = null;
            Loading.Visibility = Visibility.Visible;


            if (File.Exists(selectedMetadata.path))
            {
                try
                {
                    MediaInfo.MediaInfo info = new MediaInfo.MediaInfo();
                    info.Open(selectedMetadata.path);
                    double videoLength = int.Parse(info.Get(StreamKind.Video, 0, "Duration")) / 60000d;
                    int videoWidth = int.Parse(info.Get(StreamKind.Video, 0, "Width"));
                    int videoHeight = int.Parse(info.Get(StreamKind.Video, 0, "Height"));
                    info.Close();

                    TimeSpan videoDuration = TimeSpan.FromMinutes(videoLength);

                    string fileProperty = String.Format(
                        Application.Current.Resources["Length"] + 
                        ": {0:hh\\:mm\\:ss}\n" +
                        Application.Current.Resources["Resolution"] + 
                        ": {1} * {2}", videoDuration, videoWidth, videoHeight);
                    if (currentIndex != TitleList.SelectedIndex) return;
                    FileProperty.Text = fileProperty;

                    this.Cursor = Cursors.Arrow;

                    if ((selectedMetadata.previewMin >= 0) && (selectedMetadata.previewMin <= videoLength))
                    {
                        Bitmap bitmap = await FFMpeg.SnapshotAsync(selectedMetadata.path, new Size(videoWidth / 10, videoHeight / 10), TimeSpan.FromMinutes(selectedMetadata.previewMin));
                        BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        if (currentIndex != TitleList.SelectedIndex) return;
                        Poster.Source = bitmapSource;
                        Loading.Visibility = Visibility.Hidden;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            }
            else
            {
                this.Cursor = Cursors.Arrow;
                await this.ShowMessageAsync(Application.Current.Resources["ErrorInPath"].ToString(), "");
            }
        }


        /// <summary>
        /// Clears all the info including title, file properties, and preview image
        /// </summary>
        void ResetInfo()
        {
            TitleList.UnselectAll();
            FileTitle.Text = "";
            FileDetails.Text = "";
            FileProperty.Text = "";
            Rank.Value = 0;
            Poster.Source = null;
            Loading.Visibility = Visibility.Hidden;
        }

        void SortByName()
        {
            metadataList.Sort((a, b) => a.title.CompareTo(b.title));
        }

        void SortByRank()
        {
            metadataList.Sort((b, a) =>
            {
                if (a.rank.CompareTo(b.rank) != 0)
                {
                    return a.rank.CompareTo(b.rank);
                }
                else
                {
                    return b.title.CompareTo(a.title);
                }
            });
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            HandleSort();
            HandleFilter();
            RefreshData();
            MarkFilteredResult();
            ResetInfo();
        }

        void HandleSort()
        {
            if (Sort.SelectedIndex == 0)
            {
                SortByName();
            }
            else
            {
                SortByRank();
            }
        }

        void HandleFilter()
        {
            string key = KeyFilter.Text.Trim();
            if (key == "")
            {
                filterMarkNumber = 0;
                return;
            }

            List<Metadata> filteredList = new();

            foreach (Metadata metadata in metadataList)
            {
                bool check = false;
                if (metadata.title.Contains(key)) check = true;
                foreach (string actor in metadata.actors)
                {
                    if (actor.Contains(key))
                    {
                        check = true;
                        break;
                    }
                }
                foreach (string tag in metadata.tags)
                {
                    if (tag.Contains(key))
                    {
                        check = true;
                        break;
                    }
                }
                if (check)
                {
                    filteredList.Add(metadata);
                }
            }
            foreach (Metadata metadata in filteredList)
            {
                metadataList.Remove(metadata);
            }

            metadataList = filteredList.Concat(metadataList).ToList();

            filterMarkNumber = filteredList.Count;
        }

        private void MarkFilteredResult()
        {
            for (int index = 0; index < filterMarkNumber; index++)
            {
                ListBoxItem item = (ListBoxItem)TitleList.ItemContainerGenerator.ContainerFromIndex(index);
                if (item != null) item.Background = new SolidColorBrush(Colors.DarkGray);
            }
        }

        private void Graph_Click(object sender, RoutedEventArgs e)
        {
            CreateGraph();
            GC.Collect(); //To avoid high memory usage
            OpenGraph();
        }

        private void CreateGraph()
        {
            List<string> pivot = new();

            string nodes = "new vis.DataSet([";
            string edges = "new vis.DataSet([";

            for (int i = 0; i < metadataList.Count; i++)
            {
                nodes += "{id:" + i + ",label:\"" + ((metadataList[i].title.Length > 20) ? (metadataList[i].title.Substring(0, 20) + "...") : metadataList[i].title) + "\",title:\"" + metadataList[i].title + "\",shape:\"dot\",color:\"#5c5c5c\",size:10},";
                foreach (string actor in metadataList[i].actors)
                {
                    if (!pivot.Contains(actor)) pivot.Add(actor);
                }
                foreach (string tag in metadataList[i].tags)
                {
                    if (!pivot.Contains(tag)) pivot.Add(tag);
                }
            }
            for (int i = 0; i < pivot.Count; i++)
            {
                nodes += "{id:" + (i + metadataList.Count) + ",label:\"" + pivot[i] + "\",shape:\"dot\",color:\"#c9c9c9\",size:5},";
            }
            for (int i = 0; i < metadataList.Count; i++)
            {
                for (int j = 0; j < pivot.Count; j++)
                {
                    if (metadataList[i].actors.Contains(pivot[j]) || metadataList[i].tags.Contains(pivot[j]))
                    {
                        edges += "{from:" + i + ",to:" + (metadataList.Count + j) + ",color:\"#b6b6b6\"},";
                    }
                }
            }

            if (nodes[nodes.Length - 1] == ',') nodes = nodes.Substring(0, nodes.Length - 1);
            if (edges[edges.Length - 1] == ',') edges = edges.Substring(0, edges.Length - 1);

            nodes += "])";
            edges += "])";

            string css = ".\\Resource\\vis-network.css";
            string js = ".\\Resource\\vis-network.min.js";

            string html = "<html><head><link rel=\"stylesheet\" href=\"" + css + "\" type=\"text/css\"><script type=\"text/javascript\" src=\"" + js + "\"></script><center><h1></h1></center><style type=\"text/css\">#mynetwork{background-color:#fff;border:1px solid #d3d3d3;position:relative;float:left}</style></head><body><div id=\"mynetwork\"></div><script type=\"text/javascript\">function drawGraph(){var e=document.getElementById(\"mynetwork\");nodes=" + nodes + ",edges=" + edges + ",data={nodes:nodes,edges:edges};var o={configure:{enabled:!1},edges:{color:{inherit:!0},smooth:{enabled:!0,type:\"dynamic\"}},layout:{improvedLayout:!1},interaction:{dragNodes:!0,hideEdgesOnDrag:!1,hideNodesOnDrag:!1},physics:{enabled:!0,stabilization:{enabled:!0,fit:!0,iterations:1e3,onlyDynamicEdges:!1,updateInterval:50}}};return network=new vis.Network(e,data,o),network}var edges,nodes,network,container,options,data;drawGraph()</script></body></html>";

            try
            {
                File.WriteAllText(Properties.Settings.Default.GraphFileName, html);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void OpenGraph()
        {
            ProcessStartInfo ps = new ProcessStartInfo(Properties.Settings.Default.GraphFileName)
            {
                UseShellExecute = true
            };
            Process.Start(ps);
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            if (FileTitle.Text != "")
            {
                Clipboard.SetText(FileTitle.Text);
            }
        }
    }
}
