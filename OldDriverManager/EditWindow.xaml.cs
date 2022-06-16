﻿using MahApps.Metro.Controls;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
using Path = System.IO.Path;

namespace OldDriverManager
{
    /// <summary>
    /// EditWindow.xaml 的交互逻辑
    /// </summary>
    public delegate void MetadataDelegate(Metadata? metadata);
    public partial class EditWindow : MetroWindow
    {
        public MetadataDelegate metadataDelegate;

        Metadata? returnMetadata = null;
        public EditWindow()
        {
            InitializeComponent();
            RefreshTagAndActor();
        }

        public EditWindow(Metadata metadata)
        {
            InitializeComponent();
            RefreshTagAndActor();
            Init(metadata);
        }

        void RefreshTagAndActor()
        {
            BindingList<string> actors = new();
            BindingList<string> tags = new();

            Task task = new Task(() =>
            {
                try
                {
                    StreamReader streamReader = new(Path.Combine(Properties.Settings.Default.RootPath, Properties.Settings.Default.ActorsFileName));
                    string json = streamReader.ReadToEnd();
                    actors = JsonConvert.DeserializeObject<BindingList<string>>(json);
                    streamReader.Close();

                    streamReader = new(Path.Combine(Properties.Settings.Default.RootPath, Properties.Settings.Default.TagsFileName));
                    json = streamReader.ReadToEnd();
                    tags = JsonConvert.DeserializeObject<BindingList<string>>(json);
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
                ActorList.ItemsSource = actors;
                TagList.ItemsSource = tags;
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        void Init(Metadata metadata)
        {
            returnMetadata = metadata;

            FilePath.Content = metadata.path;
            FileTitle.Text = metadata.title;
            Rank.Value = metadata.rank;
            PreviewTime.Text = metadata.previewMin.ToString();

            foreach (string actor in metadata.actors)
            {
                ActorList.SelectedItems.Add(actor);
            }

            foreach (string tag in metadata.tags)
            {
                TagList.SelectedItems.Add(tag);
            }
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "请选择文件";
            openFileDialog.Multiselect = false;
            if ((bool)openFileDialog.ShowDialog())
            {
                FilePath.Content = openFileDialog.FileName;
                string title = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                FileTitle.Text = title;
            }
            
        }

        private void Finish_Click(object sender, RoutedEventArgs e)
        {
            List<string> actors = new();
            List<string> tags = new();
            foreach (string actor in ActorList.SelectedItems)
            {
                actors.Add(actor);
            }
            foreach (string tag in TagList.SelectedItems)
            {
                tags.Add(tag);
            }
            string title = FileTitle.Text;
            string path = FilePath.Content.ToString();
            float previewTime;
            bool previewTimeCheck = float.TryParse(PreviewTime.Text, out previewTime);
            int rank = (int)Rank.Value;


            if (File.Exists(path) && (title != "") && previewTimeCheck)
            {
                Metadata newMetadata = new Metadata(title,
                    path,
                    rank,
                    previewTime,
                    actors,
                    tags);

                returnMetadata = newMetadata;
                this.Close();
            }
            else
            {
                MessageBox.Show("错误，请检查格式");
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            metadataDelegate(returnMetadata);
        }
    }
}