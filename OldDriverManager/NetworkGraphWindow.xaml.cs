using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
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

namespace OldDriverManager
{
    /// <summary>
    /// NetworkGraphWindow.xaml 的交互逻辑
    /// </summary>
    public partial class NetworkGraphWindow : MetroWindow
    {
        private List<Metadata> metadataList;
        public NetworkGraphWindow()
        {
            InitializeComponent();
        }

        public NetworkGraphWindow(List<Metadata> metadataList)
        {
            InitializeComponent();
            this.metadataList = metadataList;
            UpdateNetworkGraph();
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            UpdateNetworkGraph();
        }

        private async void UpdateNetworkGraph()
        {
            await Graph.EnsureCoreWebView2Async();

            List<string> pivot = new();

            string nodes = "new vis.DataSet([";
            string edges = "new vis.DataSet([";

            for (int i = 0; i < metadataList.Count; i++)
            {
                nodes += "{id:" + i + ",label:\"" + ((metadataList[i].title.Length > 20)? (metadataList[i].title.Substring(0, 20) + "...") : metadataList[i].title) + "\",title:\"" + metadataList[i].title + "\",shape:\"dot\",color:\"#5c5c5c\",size:10},";
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
                nodes += "{id:" + (i + metadataList.Count) + ",label:\"" + pivot[i] + "\",shape:\"dot\",color:\"#5c5c5c\",size:10},";
            }

            for (int i = 0; i < metadataList.Count; i++)
            {
                for (int j = 0; j < pivot.Count; j++)
                {
                    if (metadataList[i].actors.Contains(pivot[j]) || metadataList[i].tags.Contains(pivot[j]))
                    {
                        edges += "{from:" + i + ",to:" + (metadataList.Count + j) + ",color:\"#5c5c5c\"},";
                    }
                }
            }

            if (nodes[nodes.Length - 1] == ',') nodes = nodes.Substring(0, nodes.Length - 1);
            if (edges[edges.Length - 1] == ',') edges = edges.Substring(0, edges.Length - 1);

            nodes += "])";
            edges += "])";

            string html = "<html><head><link rel=\"stylesheet\" href=\"https://cdn.jsdelivr.net/npm/vis-network@latest/styles/vis-network.css\" type=\"text/css\"><script type=\"text/javascript\" src=\"https://cdn.jsdelivr.net/npm/vis-network@latest/dist/vis-network.min.js\"></script><center><h1></h1></center><style type=\"text/css\">#mynetwork{background-color:#fff;border:1px solid #d3d3d3;position:relative;float:left}</style></head><body><div id=\"mynetwork\"></div><script type=\"text/javascript\">function drawGraph(){var e=document.getElementById(\"mynetwork\");nodes=" + nodes + ",edges=" + edges + ",data={nodes:nodes,edges:edges};var o={configure:{enabled:!1},edges:{color:{inherit:!0},smooth:{enabled:!0,type:\"dynamic\"}},interaction:{dragNodes:!0,hideEdgesOnDrag:!1,hideNodesOnDrag:!1},physics:{enabled:!0,stabilization:{enabled:!0,fit:!0,iterations:1e3,onlyDynamicEdges:!1,updateInterval:50}}};return network=new vis.Network(e,data,o),network}var edges,nodes,network,container,options,data;drawGraph()</script></body></html>";
            Graph.NavigateToString(html);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
