using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MusicBeePlugin
{
    public class SongInfo
    {
        public string TrackTitle { get; set; }
        public string AlbumTitle { get; set; }
        public string Artist { get; set; }
        public int Duration {get;set; }
        public string AlbumArt {get;set; }
        public int Position { get; set; }
        public long StartTimestamp { get; set; }
        public long EndTimestamp { get; set; }
    }
}
