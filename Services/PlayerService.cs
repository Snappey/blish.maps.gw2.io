using Blish_HUD.Modules.Managers;
using Blish_HUD;
using GW2IO.Maps.Static;
using MQTTnet.Client;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gw2Sharp.WebApi.V2.Models;
using GW2IO.Maps.Structures;
using Newtonsoft.Json;
using GW2IO.Maps.Extensions;
using System.Net.Http;
using GW2IO.Maps.Converters;
using Microsoft.Xna.Framework;

namespace GW2IO.Maps.Services
{
    internal enum PlayerState
    {
        Disconnected,
        Connecting,
        Connected
    }

    internal class PlayerService
    {
        private Logger _logger;
        private Settings _settings;
        private Gw2ApiManager _apiManager;
        private MqttService _mqtt;

        private Account _accountInfo;
        private WvwMatch _matchInfo;

        private PlayerState _playerState;

        public delegate void PlayerStateChanged(PlayerState state, string message);
        public event PlayerStateChanged StateChanged;

        public PlayerService(Logger logger, Settings settings, Gw2ApiManager gw2ApiManager, MqttService mqttService)
        {
            _logger = logger;
            _settings = settings;
            _apiManager = gw2ApiManager;
            _mqtt = mqttService;
        }

        PlayerRegion playerRegion(int worldId) => worldId >= 2000 ? PlayerRegion.EU :
            worldId >= 1000 ? PlayerRegion.US : PlayerRegion.Unknown;

        WvwTeam teamName(WvwMatchTeamList teams, int worldId) => 
            teams.Red.Contains(worldId) ? WvwTeam.Red :
            teams.Green.Contains(worldId) ? WvwTeam.Green : 
            teams.Blue.Contains(worldId) ? WvwTeam.Blue : WvwTeam.Unknown;

        int continentId() => GameService.Gw2Mumble.CurrentMap.IsCompetitiveMode ? 2 : 1;

        public async Task<bool> Connect() {
            UpdateState(PlayerState.Connecting, string.Empty);
            _logger.Info("Updating account information");

            var accountInfo = await refreshAccountInfo();
            if (accountInfo == null)
            {
                UpdateState(PlayerState.Disconnected, "failed to fetch account information from GW2 Api");
                return false;
            }

            var subToken = await fetchSubToken();
            if (string.IsNullOrWhiteSpace(subToken))
            {
                UpdateState(PlayerState.Disconnected, "failed to create subtoken from GW2 Api");
                return false;
            }

            var connectedToBroker = await connectToBroker(_accountInfo.Name, subToken);
            if (connectedToBroker)
                UpdateState(PlayerState.Connected, string.Empty);
            else
                UpdateState(PlayerState.Disconnected, "failed to connect maps.gw2.io");


            _logger.Info($"Connected to broker: {connectedToBroker}");
            return connectedToBroker;
        }

        public Task Disconnect() => _mqtt.Disconnect().ContinueWith((_) => UpdateState(PlayerState.Disconnected, "manually disconnected.."));

        private async Task<Account> refreshAccountInfo()
        {
            try
            {
                if (_apiManager.HasPermissions(new[] { TokenPermission.Account }))
                {
                    _accountInfo = await _apiManager.Gw2ApiClient.V2.Account.GetAsync();
                    _matchInfo = await _apiManager.Gw2ApiClient.V2.Wvw.Matches.World(_accountInfo.World).GetAsync();

                    _logger.Debug("refreshed account/wvw match info");
                    return _accountInfo;
                }
                else
                {
                    _logger.Error("missing permissions to acccess account api");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "failed to refresh users account info, aborting load.");
            }

            return null;
        }

        private async Task<string> fetchSubToken()
        {
            try
            {
                if (_apiManager.HasPermissions(new[] { TokenPermission.Account }))
                {
                    var createSubtoken = await _apiManager.Gw2ApiClient.V2.CreateSubtoken
                        .Expires(DateTimeOffset.Now.AddMinutes(5))
                        .WithPermissions(new[] { TokenPermission.Account })
                        .GetAsync();

                    _logger.Debug("created subtoken for JWT auth: " + createSubtoken.Subtoken.Substring(0, 24) + "...");

                    return createSubtoken.Subtoken;
                }
                else
                {
                    _logger.Error("missing permissions to acccess account api");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "failed to create subtoken, aborting load.");
            }

            return null;
        }

        private async Task<bool> connectToBroker(string username, string subToken)
        {
            string authToken = string.Empty;
            try
            {
                using (var client = new HttpClient())
                {
                    var resp = await client.PostAsync(_settings.MqttServerAuth.Value,
                        new StringContent($"{{ \"api_token\": \"{subToken}\" }}", Encoding.UTF8, "application/json"));
                    if (!resp.IsSuccessStatusCode)
                    {
                        _logger.Error("failed to get JWT for maps.gw2.io access.. {StatusCode}", resp.StatusCode);
                        return false;
                    }

                    authToken = await resp.Content.ReadAsStringAsync();
                }

                if (string.IsNullOrWhiteSpace(authToken))
                {
                    _logger.Error("fetched blank JWT, skipping broker connection..");
                    return false;
                }

                var connected = await _mqtt.Connect(username, authToken);

                return connected.ResultCode == MqttClientConnectResultCode.Success;
            }
            catch(Exception ex)
            {
                _logger.Error(ex, "failed to connect to broker");
                return false;
            }
        }

        public bool SendLocationData(Vector2 coords) => sendUpdate(new CharacterLocationUpdate
        {
            CharacterName = GameService.Gw2Mumble.PlayerCharacter.Name,
            ContinentId = continentId(),
            MapId = GameService.Gw2Mumble.CurrentMap.Id,

            MapPosition = coords,
            CharacterForward = GameService.Gw2Mumble.PlayerCharacter.Forward,
        });
        public bool SendStateUpdate() => sendUpdate(new CharacterStateUpdate
        {
            CharacterName = GameService.Gw2Mumble.PlayerCharacter.Name,
            ContinentId = continentId(),
            MapId = GameService.Gw2Mumble.CurrentMap.Id,

            ShardId = GameService.Gw2Mumble.RawClient.ShardId,
            ServerConnectionInfo = GameService.Gw2Mumble.Info.ServerAddress + ":" + GameService.Gw2Mumble.Info.ServerPort,
            BuildId = GameService.Gw2Mumble.Info.BuildId,

            IsCommander = GameService.Gw2Mumble.PlayerCharacter.IsCommander,
            Mount = GameService.Gw2Mumble.PlayerCharacter.CurrentMount,
            Profession = GameService.Gw2Mumble.PlayerCharacter.Profession,
            Specialisation = GameService.Gw2Mumble.PlayerCharacter.Specialization,
        });
        public bool SendDelete() => sendUpdate(new CharacterDeleteUpdate { CharacterName = GameService.Gw2Mumble.PlayerCharacter.Name });
        public void SendKeepAlive() => sendUpdate(new CharacterKeepAlive());
        public void UpdateWill() => updateWill();
        public PlayerState GetPlayerState() => _playerState;

        public bool IsAvailable() => canSendData();
        private bool canSendData()
        {
            switch(_playerState)
            {
                case PlayerState.Disconnected: return false;
                case PlayerState.Connecting: return false;
                case PlayerState.Connected: return true;
                default:
                    _logger.Warn("ignoring send data request, unimplemented player state: {State}", _playerState);
                    return false;
            }
        }

        private void updateWill()
        {
            if (!canSendData())
            {
                return;
            }

            _mqtt.UpdateWill(getTopic(), JsonConvert.SerializeObject(new CharacterDeleteUpdate { CharacterName = GameService.Gw2Mumble.PlayerCharacter.Name }));
        }

        private bool sendUpdate<T>(T payload)
        {
            if (!canSendData())
            {
                return false;
            }

            try
            {
                _ = _mqtt.PublishString(getTopic(), JsonConvert.SerializeObject(payload, new JsonConverter[] { new Vector2Converter(), new Vector3Converter() }));
                return true;
            } 
            catch (Exception ex)
            {
                _logger.Error(ex, "failed to publish data to broker");
                if (!_mqtt.IsConnected())
                    UpdateState(PlayerState.Disconnected, "Unexpected disconnect");

                return false;
            }
        }

        private string getTopic()
        {
            string mapTopic = !GameService.Gw2Mumble.CurrentMap.IsCompetitiveMode ?
            $"{playerRegion(_accountInfo.World).GetTopicString()}/{GameService.Gw2Mumble.CurrentMap.Id}" : // Tyria
            $"{_matchInfo.Id}/{teamName(_matchInfo.AllWorlds, _accountInfo.World).GetTopicString()}/{GameService.Gw2Mumble.CurrentMap.Id}"; // Mists


            return _settings.SelectedChannelType.Value != ChannelType.Solo ?
                $"{_settings.TopicPrefix.Value}/{continentId()}/{mapTopic}/{_accountInfo.Name}" :
                $"{_settings.TopicPrefix.Value}/{_accountInfo.Name}/{continentId()}/{mapTopic}/{_accountInfo.Name}";
        }

        private void UpdateState(PlayerState state, string message)
        {
            _playerState = state;
            StateChanged?.Invoke(state, message);

            if (state == PlayerState.Connected)
                SendStateUpdate();
        }

    }
}
