using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DiscordInterface;
using Util;

namespace MusicBeePlugin
{
    public partial class Plugin
    {
        private MusicBeeApiInterface mbApiInterface;
        private readonly PluginInfo about = new PluginInfo();

        private SongInfo CurrentSongInfo { get; set; }

        public PluginInfo Initialise(IntPtr apiInterfacePtr)
        {
            this.mbApiInterface = new MusicBeeApiInterface();
            this.mbApiInterface.Initialise(apiInterfacePtr);
            this.about.PluginInfoVersion = PluginInfoVersion;
            this.about.Name = "Discord Rich Presence for MusicBee";
            this.about.Description = "Updates Discord Rich Presence with MusicBee metadata";
            this.about.Author = "Harmon758 / michaelgatesdev";
            this.about.TargetApplication = ""; // current only applies to artwork, lyrics or instant messenger name that appears in the provider drop down selector or target Instant Messenger
            this.about.Type = PluginType.General;
            this.about.VersionMajor = 1; // your plugin version
            this.about.VersionMinor = 0;
            this.about.Revision = 3;
            this.about.MinInterfaceVersion = MinInterfaceVersion;
            this.about.MinApiRevision = MinApiRevision;
            this.about.ReceiveNotifications = ReceiveNotificationFlags.PlayerEvents | ReceiveNotificationFlags.TagEvents;
            this.about.ConfigurationPanelHeight = 0; // height in pixels that musicbee should reserve in a panel for config settings. When set, a handle to an empty panel will be passed to the Configure function

            InitialiseDiscord();

            return this.about;
        }

        private static void InitialiseDiscord()
        {
            var handlers = new DiscordRPC.DiscordEventHandlers
            {
                readyCallback = HandleReadyCallback,
                errorCallback = HandleErrorCallback,
                disconnectedCallback = HandleDisconnectedCallback
            };
            DiscordRPC.Initialize("381981355539693579", ref handlers, true, null);
        }

        private static void HandleReadyCallback()
        {
        }

        private static void HandleErrorCallback(int errorCode, string message)
        {
        }

        private static void HandleDisconnectedCallback(int errorCode, string message)
        {
        }

        private static void UpdatePresence(SongInfo info)
        {
            var presence = new DiscordRPC.RichPresence
            {
                details = $"\"{info.TrackTitle}\"",
                state = info.Artist,
                //                startTimestamp = info.StartTimestamp,
                //                endTimestamp = info.EndTimestamp,
                largeImageKey = "musicbee",
                largeImageText = "Download MusicBee at https://getmusicbee.com/",
                smallImageKey = "musicbee",
                smallImageText = info.AlbumTitle,
            };

            DiscordRPC.UpdatePresence(ref presence);
        }

        public bool Configure(IntPtr panelHandle)
        {
            return false;
        }

        // called by MusicBee when the user clicks Apply or Save in the MusicBee Preferences screen.
        // its up to you to figure out whether anything has changed and needs updating
        public void SaveSettings()
        {
            // save any persistent settings in a sub-folder of this path
            //            var dataPath = this.mbApiInterface.Setting_GetPersistentStoragePath();
        }

        // MusicBee is closing the plugin (plugin is being disabled by user or MusicBee is shutting down)
        public void Close(PluginCloseReason reason)
        {
            DiscordRPC.Shutdown();
        }

        // uninstall this plugin - clean up any persisted files
        public void Uninstall()
        {
        }

        // receive event notifications from MusicBee
        // you need to set about.ReceiveNotificationFlags = PlayerEvents to receive all notifications, and not just the startup event
        public void ReceiveNotification(string sourceFileUrl, NotificationType type)
        {
            var artist = this.mbApiInterface.NowPlaying_GetFileTag(MetaDataType.Artist);
            artist = artist.Substring(0, artist.Length);
            var trackTitle = this.mbApiInterface.NowPlaying_GetFileTag(MetaDataType.TrackTitle);
            trackTitle = trackTitle.Substring(0, trackTitle.Length);
            var rawDuration = this.mbApiInterface.NowPlaying_GetFileProperty(FilePropertyType.Duration);
            //            var artwork = this.mbApiInterface.NowPlaying_GetArtwork();
            var position = this.mbApiInterface.Player_GetPosition();

            this.CurrentSongInfo = new SongInfo
            {
                Artist = StringUtils.Utf16ToUtf8(artist),
                TrackTitle = StringUtils.Utf16ToUtf8(trackTitle),
                Duration = ParseDuration(rawDuration),
                Position = position
                //                    AlbumArt = artwork
            };

            switch(type)
            {
                case NotificationType.PlayStateChanged:
                    switch(this.mbApiInterface.Player_GetPlayState())
                    {
                        case PlayState.Playing:
                            this.CurrentSongInfo.Position = position / 1000;
                            break;
                    }

                    break;
                case NotificationType.TrackChanged:
                    var now = (long) DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                    this.CurrentSongInfo.StartTimestamp = now;
                    var end = (long) DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds + this.CurrentSongInfo.Duration;
                    this.CurrentSongInfo.EndTimestamp = end;
                    break;
            }

            UpdatePresence(this.CurrentSongInfo);
        }

        private static int ParseDuration(string rawDuration)
        {
            var durations = rawDuration.Split(':');
            var mins = Convert.ToInt32(durations[0]);
            var secs = Convert.ToInt32(durations[1]);

            return mins * 60 + secs;
        }
    }
}