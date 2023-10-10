using GW2IO.Maps.Static;
using GW2IO.Maps.Structures;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Blish_HUD.Settings.UI.Views;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using GW2IO.Maps.Services;
using static Blish_HUD.ArcDps.ArcDpsEnums;
using GW2IO.Maps.Extensions;

namespace GW2IO.Maps.Views
{
    internal class SettingsView : View, IDisposable
    {
        private Dictionary<string, string> _userGuilds = new Dictionary<string, string>();
        private Settings _settings;
        private Gw2ApiManager _apiManager;
        private Logger _logger;
        private PlayerService _playerService;

        private FlowPanel root;
        private ViewContainer customChannel;

        private FlowPanel statusInfo;
        private Label statusLabel;
        private LoadingSpinner statusSpinner;

        private FlowPanel statusButtons;
        private StandardButton connectButton;
        private StandardButton disconnectButton;

        private FlowPanel guildChannel;
        private Dropdown guildChannelDropdown;
        private LoadingSpinner guildChannelLoadingSpinner;

        public SettingsView(Logger logger, Settings settings, Gw2ApiManager apiManager, PlayerService playerService)
        {
            _logger = logger;
            _settings = settings;
            _apiManager = apiManager;
            _playerService = playerService;

            _ = GetUserGuilds();
            apiManager.SubtokenUpdated += ApiManager_SubtokenUpdated;
        }

        private void ApiManager_SubtokenUpdated(object sender, ValueEventArgs<IEnumerable<TokenPermission>> e) =>
            _ = GetUserGuilds();

        protected override void Build(Container buildPanel)
        {
            base.Build(buildPanel);

            root = new FlowPanel
            {
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                WidthSizingMode = SizingMode.Fill,
                HeightSizingMode = SizingMode.AutoSize,
                OuterControlPadding = new Vector2(4, 0),
                Parent = buildPanel
            };


            statusInfo = new FlowPanel
            {
                FlowDirection = ControlFlowDirection.SingleLeftToRight,
                WidthSizingMode = SizingMode.Fill,
                HeightSizingMode = SizingMode.AutoSize,
                OuterControlPadding = new Vector2(4, 0),
                Parent = root
            };

            var currentState = _playerService.GetPlayerState();
            statusLabel = new Label()
            {
                Text = currentState.ToString(),
                AutoSizeWidth = true,
                TextColor = currentState.GetStatusColor(),
                Parent = statusInfo
            };

            statusSpinner = new LoadingSpinner
            {
                BasicTooltipText = "Connecting...",
                Size = new Point(20, 20),
                Parent = statusInfo,
                Visible = false,
            };


            statusButtons = new FlowPanel
            {
                FlowDirection = ControlFlowDirection.SingleLeftToRight,
                WidthSizingMode = SizingMode.Fill,
                HeightSizingMode = SizingMode.AutoSize,
                OuterControlPadding = new Vector2(4, 0),
                Parent = root
            };

            disconnectButton = new StandardButton()
            {
                Text = "Disconnect",
                Size = new Point(110, 30),
                Parent = statusButtons,
            };
            disconnectButton.Click += Disconnect_Click;

            connectButton = new StandardButton()
            {
                Text = "Connect",
                Size = new Point(110, 30),
                Parent = statusButtons,
            };
            connectButton.Click += Connect_Click;

            _playerService.StateChanged += PlayerStateChanged;

            CreateDefaultSettingsView(_settings.SelectedChannelType, root);

            customChannel = CreateDefaultSettingsView(_settings.SelectedCustomChannel, root);
            guildChannel = CreateGuildDropdown(root);

            _settings.SelectedChannelType.SettingChanged += SelectedChannelSettingChanged;
            SelectedChannelSettingChanged(this, new ValueChangedEventArgs<ChannelType>(_settings.SelectedChannelType.Value, _settings.SelectedChannelType.Value));
        }


        public void Dispose()
        {
            _apiManager.SubtokenUpdated -= ApiManager_SubtokenUpdated;
            _settings.SelectedChannelType.SettingChanged -= SelectedChannelSettingChanged;
            _playerService.StateChanged -= PlayerStateChanged;

            guildChannelDropdown.ValueChanged -= SelectedGuildSettingChanged;
            disconnectButton.Click -= Disconnect_Click;
            connectButton.Click -= Connect_Click;
        }

        private void PlayerStateChanged(PlayerState state, string message)
        {
            statusLabel.Text = state.ToString() + ", " + message;
            statusLabel.TextColor = state.GetStatusColor();

            switch (state)
            {
                case PlayerState.Connected:
                    connectButton.Enabled = false;
                    disconnectButton.Enabled = true;

                    statusSpinner.Visible = false;
                    break;
                case PlayerState.Connecting:
                    connectButton.Enabled = false;
                    disconnectButton.Enabled = false;

                    statusSpinner.Visible = true;
                    break;
                case PlayerState.Disconnected:
                    connectButton.Enabled = true;
                    disconnectButton.Enabled = false;

                    statusSpinner.Visible = false;
                    break;
            }

            statusInfo.RecalculateLayout();
        }

        private void Connect_Click(object sender, Blish_HUD.Input.MouseEventArgs e) =>
            _ = _playerService.Connect();

        private void Disconnect_Click(object sender, Blish_HUD.Input.MouseEventArgs e) =>
            _ = _playerService.Disconnect();


        private void SelectedChannelSettingChanged(object sender, ValueChangedEventArgs<ChannelType> e)
        {
            switch (e.NewValue)
            {
                case ChannelType.Solo:
                case ChannelType.Global:
                    customChannel.Hide();
                    guildChannel.Hide();
                    break;
                case ChannelType.Custom:
                    customChannel.Show();
                    guildChannel.Hide();
                    break;
                case ChannelType.Guild:
                    customChannel.Hide();
                    guildChannel.Show();
                    break;
            }

            root.RecalculateLayout();
        }

        private async Task GetUserGuilds()
        {
            _logger.Debug("Fetching user guilds");

            var accountInfo = await _apiManager.Gw2ApiClient.V2.Account.GetAsync();
            var guildTasks = new List<Task>();
            var previouslySelectedGuildId = _settings.SelectedGuildChannel.Value;
            foreach (var guildId in accountInfo.Guilds)
            {
                guildChannelDropdown.Items.Add(guildId.ToString());
                guildTasks.Add(GetGuildName(guildId.ToString()));
            }

            await Task.WhenAll(guildTasks);
            _logger.Debug("Finished fetching all guild names, attempting to restore previously selected guild {GuildId}", previouslySelectedGuildId);
            if (_userGuilds.TryGetValue(previouslySelectedGuildId, out var friendlyName))
            {
                _logger.Debug("Found previously selected guild, restoring as friendly name {GuildName}", friendlyName);
                guildChannelDropdown.SelectedItem = friendlyName;
            }

            guildChannelLoadingSpinner.Hide();
        }

        private async Task GetGuildName(string guildId)
        {
            var guildInfo = await _apiManager.Gw2ApiClient.V2.Guild[guildId].GetAsync();
            var friendlyName = $"[{guildInfo.Tag}] {guildInfo.Name}";

            _userGuilds.Add(friendlyName, guildId);
            _userGuilds.Add(guildId, friendlyName);

            guildChannelDropdown.Items.Remove(guildId);
            guildChannelDropdown.Items.Add($"[{guildInfo.Tag}] {guildInfo.Name}");
        }

        private FlowPanel CreateGuildDropdown(Container parent)
        {
            var root = new FlowPanel
            {
                FlowDirection = ControlFlowDirection.LeftToRight,
                WidthSizingMode = SizingMode.Fill,
                HeightSizingMode = SizingMode.AutoSize,
                OuterControlPadding = new Vector2(4, 0),
                Parent = parent
            };

            new Label
            {
                Parent = root,
                Text = "Guild ",
                AutoSizeWidth = true,
            };

            guildChannelDropdown = new Dropdown
            {
                Parent = root,
            };

            guildChannelLoadingSpinner = new LoadingSpinner
            {
                Parent = root,
                BasicTooltipText = "Loading guilds from API..."
            };

            guildChannelDropdown.ValueChanged += SelectedGuildSettingChanged;

            return root;
        }

        private void SelectedGuildSettingChanged(object sender, ValueChangedEventArgs e)
        {
            _logger.Debug("Changed selected guild to {GuildId}", e.CurrentValue);
            if (_userGuilds.TryGetValue(e.CurrentValue, out var guildId))
            {
                _logger.Debug("Found friendly value {GuildName}", guildId);
                _settings.SelectedGuildChannel.Value = guildId;
            }
        }

        private ViewContainer CreateDefaultSettingsView(SettingEntry setting, Container parent)
        {
            var view = new ViewContainer { Parent = parent };
            view.Show(SettingView.FromType(setting, parent.Width));
            return view;
        }
    }
}
