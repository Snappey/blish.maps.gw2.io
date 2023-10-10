using GW2IO.Maps.Structures;
using Blish_HUD;
using Blish_HUD.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime;

namespace GW2IO.Maps.Static
{
    internal class Settings : IDisposable
    {
        private Logger _logger;

        public SettingEntry<ChannelType> SelectedChannelType;
        public SettingEntry<string> SelectedGuildChannel;
        public SettingEntry<string> SelectedCustomChannel;

        public SettingEntry<string> TopicPrefix;
        public SettingEntry<string> MqttServer;
        public SettingEntry<int> MqttServerPort;
        public SettingEntry<string> MqttServerAuth;

        public Settings(Logger Logger, SettingCollection settings)
        {
            _logger = Logger;

            SelectedChannelType = settings.DefineSetting("channel", ChannelType.Global, () => "Channel");
            SelectedGuildChannel = settings.DefineSetting("selectedGuild", "", () => "Selected Guild", () => "Guild that should be used if your channel is set to Guild");
            SelectedCustomChannel = settings.DefineSetting("customChannel", "", () => "Custom Channel", () => "Name of a custom channel to use if your channel is set to Custom");
            SelectedCustomChannel.SetValidation((val) => val.Length > 2 && val.Length < 32 ? 
                new SettingValidationResult(true) :
                new SettingValidationResult(false, "Channel must be more than 2 characters and less than 32"));

            TopicPrefix = settings.DefineSetting("topicPrefix", "maps.gw2.io/global");
            MqttServer = settings.DefineSetting("mqttServer", "leyline.gw2.io:443/mqtt");
            MqttServerAuth = settings.DefineSetting("mqttServerAuth", "https://auth-leyline.gw2.io/auth");

            SelectedChannelType.SettingChanged += SelectedChannelType_SettingChanged;
            SelectedGuildChannel.SettingChanged += SelectedGuildChannel_SettingChanged;
            SelectedCustomChannel.SettingChanged += SelectedCustomChannel_SettingChanged;
        }

        private void SelectedCustomChannel_SettingChanged(object sender, ValueChangedEventArgs<string> e) => UpdateTopicPrefix(SelectedChannelType.Value);
        private void SelectedGuildChannel_SettingChanged(object sender, ValueChangedEventArgs<string> e) => UpdateTopicPrefix(SelectedChannelType.Value);
        private void SelectedChannelType_SettingChanged(object sender, ValueChangedEventArgs<ChannelType> e) => UpdateTopicPrefix(e.NewValue);

        private void UpdateTopicPrefix(ChannelType type)
        {
            switch (type)
            {
                case ChannelType.Global:
                    TopicPrefix.Value = "maps.gw2.io/global";
                    break;
                case ChannelType.Guild:
                    TopicPrefix.Value = $"maps.gw2.io/guild/{SelectedGuildChannel.Value.ToUpper()}";
                    break;
                case ChannelType.Custom:
                    TopicPrefix.Value = $"maps.gw2.io/custom/{SelectedCustomChannel.Value}";
                    break;
                case ChannelType.Solo:
                    TopicPrefix.Value = $"maps.gw2.io/solo";
                    break;
            }
        }

        public void Dispose()
        {
            SelectedChannelType.SettingChanged -= SelectedChannelType_SettingChanged;
            SelectedGuildChannel.SettingChanged -= SelectedGuildChannel_SettingChanged;
            SelectedCustomChannel.SettingChanged -= SelectedCustomChannel_SettingChanged;
        }
    }
}
