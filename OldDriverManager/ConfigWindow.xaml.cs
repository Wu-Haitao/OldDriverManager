using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Diagnostics;
using MahApps.Metro.Controls;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Path = System.IO.Path;

namespace OldDriverManager
{
    /// <summary>
    /// ConfigWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ConfigWindow : MetroWindow
    {
        BindingList<string> actors = new();
        BindingList<string> tags = new();


        public ConfigWindow()
        {
            InitializeComponent();
            Init();
        }

        void Init()
        {

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


        private void Save_Click(object sender, RoutedEventArgs e)
        {

            Task task = new Task(() =>
            {
                try
                {
                    if (!Directory.Exists(Properties.Settings.Default.RootPath))
                    {
                        Directory.CreateDirectory(Properties.Settings.Default.RootPath);
                    }
                    
                    StreamWriter streamWriter = new(Path.Combine(Properties.Settings.Default.RootPath, Properties.Settings.Default.ActorsFileName));
                    string json = JsonConvert.SerializeObject(actors);
                    streamWriter.Write(json);
                    streamWriter.Close();

                    streamWriter = new(Path.Combine(Properties.Settings.Default.RootPath, Properties.Settings.Default.TagsFileName));
                    json = JsonConvert.SerializeObject(tags);
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
                this.Close();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void AddToTag_Click(object sender, RoutedEventArgs e)
        {
            if (Input.Text == "") return;

            if (tags.IndexOf(Input.Text) == -1) tags.Add(Input.Text);
        }

        private void AddToActor_Click(object sender, RoutedEventArgs e)
        {
            if (Input.Text == "") return;

            if (actors.IndexOf(Input.Text) == -1) actors.Add(Input.Text);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (ActorList.SelectedIndex != -1)
            {
                actors.RemoveAt(ActorList.SelectedIndex);
            }

            if (TagList.SelectedIndex != -1)
            {
                tags.RemoveAt(TagList.SelectedIndex);
            }
        }

        private void TagList_GotFocus(object sender, RoutedEventArgs e)
        {
            ActorList.UnselectAll();
        }

        private void ActorList_GotFocus(object sender, RoutedEventArgs e)
        {
            TagList.UnselectAll();
        }
    }
}
