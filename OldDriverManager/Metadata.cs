using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OldDriverManager
{
    public class Metadata
    {
        public string title;
        public string path;
        public int rank;
        public float previewMin;
        public List<string> actors;
        public List<string> tags;
        public Metadata(string title, string path, int rank, float previewMin, List<string> actors, List<string> tags)
        {
            this.title = title;
            this.path = path;
            this.rank = rank;
            this.previewMin = previewMin;
            this.actors = actors;
            this.tags = tags;
        }
    }
}
