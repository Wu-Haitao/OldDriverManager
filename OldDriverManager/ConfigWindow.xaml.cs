using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using MahApps.Metro.Controls;
using System.IO;
using Newtonsoft.Json;
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

        private void ActorList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ActorList.SelectedIndex == -1) return;
            Input.Text = actors[ActorList.SelectedIndex];
        }

        private void TagList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TagList.SelectedIndex == -1) return;
            Input.Text = tags[TagList.SelectedIndex];
        }
    }
}
