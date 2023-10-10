using Blish_HUD;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Gw2Sharp.WebApi.V2.Models;
using System.Collections.Generic;
using GW2IO.Maps.Extensions;
using GW2IO.Maps.Static;
using Blish_HUD.Graphics.UI;
using GW2IO.Maps.Views;
using GW2IO.Maps.Services;
using Blish_HUD.Controls;

namespace GW2IO.Maps
{
    [Export(typeof(Blish_HUD.Modules.Module))]
    public class Module : Blish_HUD.Modules.Module
    {

        private static readonly Logger Logger = Logger.GetLogger<Module>();

        #region Service Managers
        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;
        #endregion

        private MqttService _mqttService;
        private PlayerService _playerService;
        private Settings _settings;

        private CornerIconMenu _cornerIcon;

        [ImportingConstructor]
        public Module([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { }

        protected override void DefineSettings(SettingCollection settings)
        {
            _settings = new Settings(Logger, settings);

            base.DefineSettings(settings);
        }

        public override IView GetSettingsView()
        {
            return new SettingsView(Logger, _settings, Gw2ApiManager, _playerService);
        }

        protected override Task LoadAsync()
        {
            _ = cacheMapData();
            return base.LoadAsync();
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            _mqttService = new MqttService(Logger, _settings);
            _playerService = new PlayerService(Logger, _settings, Gw2ApiManager, _mqttService);
            _cornerIcon = new CornerIconMenu(_playerService, _settings);

            Gw2ApiManager.SubtokenUpdated += UpdatePlayerAccountInfo;
            GameService.Gw2Mumble.PlayerCharacter.NameChanged += StateUpdateHandler;
            GameService.Gw2Mumble.FinishedLoading += StateUpdateHandler;
            GameService.Gw2Mumble.PlayerCharacter.CurrentMountChanged += StateUpdateHandler;
            GameService.Gw2Mumble.PlayerCharacter.IsCommanderChanged += StateUpdateHandler;
            GameService.Gw2Mumble.PlayerCharacter.SpecializationChanged += StateUpdateHandler;
            GameService.Gw2Mumble.CurrentMap.MapChanged += StateUpdateHandler;

            _ = _playerService.Connect();

            base.OnModuleLoaded(e);
        }

        private void StateUpdateHandler(object sender, EventArgs _) =>
            SendPlayerState();

        private async void UpdatePlayerAccountInfo(object sender, ValueEventArgs<IEnumerable<TokenPermission>> e)
        {
            var updated = await _playerService.Connect();
            if (updated)
            {
                ScreenNotification.ShowNotification("connected to maps.gw2.io");
            }
            else
            {
                ScreenNotification.ShowNotification("failed to connect to maps.gw2.io");
            }
        }

        private bool isAvailable() => GameService.Gw2Mumble.IsAvailable;    

        private double _sendLocationMilliseconds = 0;
        private double _updateWillSeconds = 0;
        private double _sendKeepAliveSeconds = 0;
        private Vector2 _lastSendCoords = new Vector2(0, 0);

        private const double LOCATION_UPDATE_MS = 500;
        private const double WILL_UPDATE_SECS = 60;
        private const double KEEP_ALIVE_UPDATE_SECS = 30;

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            _sendLocationMilliseconds += gameTime.ElapsedGameTime.TotalMilliseconds;
            _updateWillSeconds += gameTime.ElapsedGameTime.TotalSeconds;
            _sendKeepAliveSeconds += gameTime.ElapsedGameTime.TotalSeconds;

            if (isAvailable())
            {
                UpdateHook(ref _sendLocationMilliseconds, LOCATION_UPDATE_MS, () => {
                    _lastSendCoords = SendPlayerData(_lastSendCoords);
                });
                UpdateHook(ref _updateWillSeconds, WILL_UPDATE_SECS, SendPlayerWillUpdate);
                UpdateHook(ref _sendKeepAliveSeconds, KEEP_ALIVE_UPDATE_SECS, SendPlayerKeepAlive);
            }
        }

        protected void UpdateHook(ref double timer, double updateTime, Action callback)
        {
            if (timer >= updateTime)
            {
                callback.Invoke();
                timer = 0;
            }
        }

        protected override void Unload()
        {
            if (_playerService.IsAvailable())
                _playerService.Disconnect().Wait();

            _settings?.Dispose();
            _mqttService?.Dispose();

            _cornerIcon?.Dispose();

            Gw2ApiManager.SubtokenUpdated -= UpdatePlayerAccountInfo;
            Gw2ApiManager.SubtokenUpdated -= UpdatePlayerAccountInfo;
            GameService.Gw2Mumble.PlayerCharacter.NameChanged -= StateUpdateHandler;
            GameService.Gw2Mumble.FinishedLoading -= StateUpdateHandler;
            GameService.Gw2Mumble.PlayerCharacter.CurrentMountChanged -= StateUpdateHandler;
            GameService.Gw2Mumble.PlayerCharacter.IsCommanderChanged -= StateUpdateHandler;
            GameService.Gw2Mumble.PlayerCharacter.SpecializationChanged -= StateUpdateHandler;
            GameService.Gw2Mumble.CurrentMap.MapChanged -= StateUpdateHandler;

            base.Unload();
        }

        private Vector2 SendPlayerData(Vector2 lastCoords)
        {
            var coords = getCoordinates();
            if (lastCoords == coords || coords == Vector2.Zero)
            {
                return lastCoords;
            }

            return _playerService.SendLocationData(coords) ?
                coords :
                Vector2.Zero;
        }

        private void SendPlayerState() => _playerService.SendStateUpdate();
        private void SendPlayerDelete() => _playerService.SendDelete();
        private void SendPlayerWillUpdate() => _playerService.UpdateWill();
        private void SendPlayerKeepAlive() => _playerService.SendKeepAlive();


        internal Dictionary<int, Map> _gw2Maps = new Dictionary<int, Map>();

        private async Task cacheMapData()
        {
            // Cache MapData by id for coordinates
            var maps = await GameService.Gw2WebApi.AnonymousConnection.Client.V2.Maps.AllAsync();
            foreach (var map in maps)
            {
                _gw2Maps.Add(map.Id, map);
            }
        }

        private Vector2 getCoordinates()
        {
            if (!GameService.Gw2Mumble.CurrentMap.IsCompetitiveMode)
            { // Doesn't work in WvW
                return new Vector2( 
                   (int)GameService.Gw2Mumble.RawClient.PlayerLocationMap.X,
                   (int)GameService.Gw2Mumble.RawClient.PlayerLocationMap.Y
                );
            }

            if (_gw2Maps.TryGetValue(GameService.Gw2Mumble.CurrentMap.Id, out var gw2Map) == false)
            {
                return Vector2.Zero;
            }

            return gw2Map.WorldMeterCoordsToMapCoords(GameService.Gw2Mumble.PlayerCharacter.Position);
        }
    }

}
