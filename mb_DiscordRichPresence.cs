using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

using DiscordInterface;
using Util;

namespace MusicBeePlugin
{
    public partial class Plugin
    {
        private MusicBeeApiInterface mbApiInterface;
        private readonly PluginInfo about = new PluginInfo();

        public PluginInfo Initialise(IntPtr apiInterfacePtr)
        {
            this.mbApiInterface = new MusicBeeApiInterface();
            this.mbApiInterface.Initialise(apiInterfacePtr);
            this.about.PluginInfoVersion = PluginInfoVersion;
            this.about.Name = "Discord Rich Presence";
            this.about.Description = "Sets currently playing song as Discord Rich Presence";
            this.about.Author = "Harmon758";
            this.about.TargetApplication = "";   // current only applies to artwork, lyrics or instant messenger name that appears in the provider drop down selector or target Instant Messenger
            this.about.Type = PluginType.General;
            this.about.VersionMajor = 1;  // your plugin version
            this.about.VersionMinor = 0;
            this.about.Revision = 2;
            this.about.MinInterfaceVersion = MinInterfaceVersion;
            this.about.MinApiRevision = MinApiRevision;
            this.about.ReceiveNotifications = (ReceiveNotificationFlags.PlayerEvents | ReceiveNotificationFlags.TagEvents);
            this.about.ConfigurationPanelHeight = 0;   // height in pixels that musicbee should reserve in a panel for config settings. When set, a handle to an empty panel will be passed to the Configure function

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

        private static void HandleReadyCallback() { }
        private static void HandleErrorCallback(int errorCode, string message) { }
        private static void HandleDisconnectedCallback(int errorCode, string message) { }

        private static void UpdatePresence(string song, string duration, int position, string state = "Listening to music via MusicBee")
        {
            var presence = new DiscordRPC.RichPresence { state = state };
            song = Utility.Utf16ToUtf8(song);
            presence.details = song.Substring(0, song.Length - 1);
            presence.largeImageKey = "musicbee";
            var now = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            presence.startTimestamp = now - position;
            // string[] durations = duration.Split(':');
            // long end = now + System.Convert.ToInt64(durations[0]) * 60 + System.Convert.ToInt64(durations[1]);
            // presence.endTimestamp = end;
            DiscordRPC.UpdatePresence(ref presence);
        }

        public bool Configure(IntPtr panelHandle)
        {
            // save any persistent settings in a sub-folder of this path
            var dataPath = this.mbApiInterface.Setting_GetPersistentStoragePath();
            // panelHandle will only be set if you set about.ConfigurationPanelHeight to a non-zero value
            // keep in mind the panel width is scaled according to the font the user has selected
            // if about.ConfigurationPanelHeight is set to 0, you can display your own popup window
            if (panelHandle != IntPtr.Zero)
            {
                var configPanel = (Panel)Control.FromHandle(panelHandle);
                var prompt = new Label
                {
                    AutoSize = true,
                    Location = new Point(0, 0),
                    Text = "prompt:"
                };
                var textBox = new TextBox();
                textBox.Bounds = new Rectangle(60, 0, 100, textBox.Height);
                configPanel.Controls.AddRange(new Control[] { prompt, textBox });
            }
            return false;
        }
       
        // called by MusicBee when the user clicks Apply or Save in the MusicBee Preferences screen.
        // its up to you to figure out whether anything has changed and needs updating
        public void SaveSettings()
        {
            // save any persistent settings in a sub-folder of this path
            var dataPath = this.mbApiInterface.Setting_GetPersistentStoragePath();
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
            var trackTitle = this.mbApiInterface.NowPlaying_GetFileTag(MetaDataType.TrackTitle);
            var duration = this.mbApiInterface.NowPlaying_GetFileProperty(FilePropertyType.Duration);
            // mbApiInterface.NowPlaying_GetDuration();
            var position = this.mbApiInterface.Player_GetPosition();
            var song = artist + " - " + trackTitle;
            if (string.IsNullOrEmpty(artist)) { song = trackTitle; }
            // perform some action depending on the notification type
            switch (type)
            {
                case NotificationType.PluginStartup:
                    // perform startup initialisation
                case NotificationType.PlayStateChanged:
                    switch (this.mbApiInterface.Player_GetPlayState())
                    {
                        case PlayState.Playing:
                            UpdatePresence(song, duration, position / 1000);
                            break;
                        case PlayState.Paused:
                            UpdatePresence(song, duration, 0, "Paused");
                            break;
                    }
                    break;
                case NotificationType.TrackChanged:
                    UpdatePresence(song, duration, 0);
                    break;
            }
        }
   }
}