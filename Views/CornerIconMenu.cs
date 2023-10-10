using Blish_HUD.Content;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GW2IO.Maps.Services;
using GW2IO.Maps.Static;
using GW2IO.Maps.Structures;

namespace GW2IO.Maps.Views
{
    internal class CornerIconMenu : IDisposable
    {
        private CornerIcon _cornerIcon;
        private PlayerService _playerService;
        private Settings _settings;

        public CornerIconMenu(PlayerService playerService, Settings settings)
        {
            _playerService = playerService;
            _settings = settings;

            _cornerIcon = new CornerIcon()
            {
                Icon = AsyncTexture2D.FromAssetId(157124),
                HoverIcon = AsyncTexture2D.FromAssetId(157123),
                Parent = GameService.Graphics.SpriteScreen,
                BasicTooltipText = GetTooltipText(_playerService.GetPlayerState()),
            };

            _cornerIcon.Click += CornerIconClicked;
            _playerService.StateChanged += UpdateTooltip;
            _settings.SelectedChannelType.SettingChanged += ChannelTypeChanged;
            _settings.SelectedGuildChannel.SettingChanged += GuildChannelTypeChanged;
            _settings.SelectedCustomChannel.SettingChanged += CustomChannelTypeChanged;
            _playerService.StateChanged += UpdateLoading;
        }

        private void UpdateLoading(PlayerState state, string message)
        {
            switch (state)
            {
                case PlayerState.Disconnected:
                case PlayerState.Connected:
                    _cornerIcon.LoadingMessage = null;
                    return;
                case PlayerState.Connecting:
                    _cornerIcon.LoadingMessage = $"Connecting...";
                    return;
            }
        }

        private void ChannelTypeChanged(object sender, ValueChangedEventArgs<Structures.ChannelType> e) =>
            _cornerIcon.BasicTooltipText = GetTooltipText(_playerService.GetPlayerState());

        private void CustomChannelTypeChanged(object sender, ValueChangedEventArgs<string> e) =>
            _cornerIcon.BasicTooltipText = GetTooltipText(_playerService.GetPlayerState());

        private void GuildChannelTypeChanged(object sender, ValueChangedEventArgs<string> e) =>
            _cornerIcon.BasicTooltipText = GetTooltipText(_playerService.GetPlayerState());

        private void UpdateTooltip(PlayerState state, string message) =>
            _cornerIcon.BasicTooltipText = GetTooltipText(state);

        private void CornerIconClicked(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            if (!_playerService.IsAvailable())
                _ = _playerService.Connect();
        }

        public string GetTooltipText(PlayerState state)
        {
            var selectedChannel = _settings.SelectedChannelType.Value;

            switch (state)
            {
                case PlayerState.Connected:
                    switch (selectedChannel)
                    {
                        case ChannelType.Guild:
                            return $"Connected to {selectedChannel}, {_settings.SelectedGuildChannel.Value}";
                        case ChannelType.Custom:
                            return $"Connected to {selectedChannel}, {_settings.SelectedCustomChannel.Value}";
                        default:
                            return $"Connected to {selectedChannel}";
                    }
                case PlayerState.Disconnected:
                    return $"Disconnected, click to reconnect";
                case PlayerState.Connecting:
                    return $"Connecting to ${selectedChannel}...";
                default:
                    return $"Something has gone very wrong..";
            }
        }

        public void Dispose()
        {
            _cornerIcon.Click -= CornerIconClicked;
            _playerService.StateChanged -= UpdateTooltip;
            _playerService.StateChanged -= UpdateLoading;
            _settings.SelectedChannelType.SettingChanged -= ChannelTypeChanged;
            _settings.SelectedGuildChannel.SettingChanged -= GuildChannelTypeChanged;
            _settings.SelectedCustomChannel.SettingChanged -= CustomChannelTypeChanged;
            _cornerIcon.Dispose();
        }
    }
}
