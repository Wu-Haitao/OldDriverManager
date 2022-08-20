using MahApps.Metro.Controls;
using FFMpegCore;
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
using MahApps.Metro.Controls.Dialogs;
using MediaInfo;

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
            RefreshDataFromFile();
            ResetInfo();
        }

        void Init()
        {
            TitleList.ItemContainerGenerator.StatusChanged += ItemContainerGenerator_StatusChanged;
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

        private void Config_Click(object sender, RoutedEventArgs e)
        {
            ConfigWindow configWindow = new ConfigWindow();
            configWindow.Title = "属性";
            configWindow.ShowDialog();
        }

        private void Batch_Click(object sender, RoutedEventArgs e)
        {
            BatchWindow batchWindow = new BatchWindow(metadataList);
            batchWindow.Title = "批量";
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
            editWindow.Title = "添加";
            editWindow.metadataDelegate += new MetadataDelegate(EditMetadata);
            editWindow.ShowDialog();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (TitleList.SelectedIndex == -1) return;
            Metadata selectedMetadata = metadataList[TitleList.SelectedIndex];
            metadataList.RemoveAt(TitleList.SelectedIndex);
            EditWindow editWindow = new EditWindow(selectedMetadata, TitleList.SelectedIndex);
            editWindow.Title = "编辑";
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
                    MessageBox.Show("无法添加：相同文件已存在！");
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
                Process.Start("explorer.exe", Path.GetDirectoryName(selectedMetadata.path));
            }
            else
            {
                this.ShowMessageAsync("错误，请检查路径", "");
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
                this.ShowMessageAsync("错误，请检查路径", "");
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (TitleList.SelectedIndex == -1) return;

            MessageDialogResult result = await this.ShowMessageAsync("确定要删除吗？", "", MessageDialogStyle.AffirmativeAndNegative);
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

            FileProperty.Text = "加载中......";

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

                    string fileProperty = String.Format("长度：{0:hh\\:mm\\:ss}\n分辨率：{1} * {2}", videoDuration, videoWidth, videoHeight);
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
            else {
                this.Cursor = Cursors.Arrow;
                await this.ShowMessageAsync("错误，请检查路径", "");
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

            NetworkGraphWindow networkGraphWindow = new NetworkGraphWindow(metadataList);
            networkGraphWindow.Show();
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            if (FileTitle.Text != "") {
                Clipboard.SetText(FileTitle.Text);
            }
        }
    }
}
