﻿//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Linq;
using System.Threading.Tasks;
using Mapster;
using Microsoft.Extensions.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Core.HttpClients.ChannelHttpClients;
using NosCore.Core.HttpClients.ConnectedAccountHttpClients;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject;
using NosCore.GameObject.Holders;
using NosCore.GameObject.HttpClients.FriendHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.FriendService;
using NosCore.PacketHandlers.Friend;
using NosCore.Packets.ClientPackets.Relations;
using NosCore.Packets.Enumerations;
using NosCore.Shared.Configuration;
using NosCore.Shared.I18N;
using NosCore.Tests.Shared;
using Serilog;
using Character = NosCore.Data.WebApi.Character;

namespace NosCore.PacketHandlers.Tests.Friend
{
    [TestClass]
    public class FinsPacketHandlerTests
    {
        private static readonly ILogger Logger = new Mock<ILogger>().Object;
        private readonly Mock<IChannelHttpClient> _channelHttpClient = TestHelpers.Instance.ChannelHttpClient;
        private IDao<CharacterRelationDto, Guid>? _characterRelationDao;
        private readonly Mock<IConnectedAccountHttpClient> _connectedAccountHttpClient = TestHelpers.Instance.ConnectedAccountHttpClient;
        private FinsPacketHandler? _finsPacketHandler;
        private readonly Mock<IFriendHttpClient> _friendHttpClient = TestHelpers.Instance.FriendHttpClient;
        private FriendRequestHolder? _friendRequestHolder;

        private ClientSession? _session;
        private ClientSession? _targetSession;

        [TestInitialize]
        public async Task SetupAsync()
        {
            TypeAdapterConfig<MapNpcDto, MapNpc>.NewConfig()
                .ConstructUsing(src => new MapNpc(null, Logger, TestHelpers.Instance.DistanceCalculator, TestHelpers.Instance.Clock));
            Broadcaster.Reset();
            await TestHelpers.ResetAsync().ConfigureAwait(false);
            _session = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            _targetSession = await TestHelpers.Instance.GenerateSessionAsync().ConfigureAwait(false);
            _characterRelationDao = TestHelpers.Instance.CharacterRelationDao;
            _friendRequestHolder = new FriendRequestHolder();
            _connectedAccountHttpClient.Setup(s => s.GetCharacterAsync(_targetSession.Character.CharacterId, null))
                .ReturnsAsync(new Tuple<ServerConfiguration?, ConnectedAccount?>(new ServerConfiguration(),
                    new ConnectedAccount
                    {
                        ChannelId = 1, ConnectedCharacter = new Character { Id = _targetSession.Character.CharacterId }
                    }));
            _connectedAccountHttpClient.Setup(s => s.GetCharacterAsync(_session.Character.CharacterId, null))
                .ReturnsAsync(new Tuple<ServerConfiguration?, ConnectedAccount?>(new ServerConfiguration(),
                    new ConnectedAccount
                    { ChannelId = 1, ConnectedCharacter = new Character { Id = _session.Character.CharacterId } }));
            _finsPacketHandler = new FinsPacketHandler(_friendHttpClient.Object, _channelHttpClient.Object,
                _connectedAccountHttpClient.Object);
        }

        [TestMethod]
        public async Task Test_Add_FriendAsync()
        {
            _friendRequestHolder!.FriendRequestCharacters.TryAdd(Guid.NewGuid(),
                new Tuple<long, long>(_targetSession!.Character.CharacterId, _session!.Character.CharacterId));
            var finsPacket = new FinsPacket
            {
                CharacterId = _targetSession.Character.CharacterId,
                Type = FinsPacketType.Accepted
            };

            var friend = new FriendService(Logger, _characterRelationDao!, TestHelpers.Instance.CharacterDao,
                _friendRequestHolder, _connectedAccountHttpClient.Object, TestHelpers.Instance.LogLanguageLocalizer);
            _friendHttpClient.Setup(s => s.AddFriendAsync(It.IsAny<FriendShipRequest>()))
                .Returns(friend.AddFriendAsync(_session.Character.CharacterId, finsPacket.CharacterId, finsPacket.Type));
            await _finsPacketHandler!.ExecuteAsync(finsPacket, _session).ConfigureAwait(false);
            Assert.IsTrue(_characterRelationDao!.LoadAll().Count() == 2);
        }

        [TestMethod]
        public async Task Test_Add_Friend_When_DisconnectedAsync()
        {
            var finsPacket = new FinsPacket
            {
                CharacterId = _targetSession!.Character.CharacterId,
                Type = FinsPacketType.Accepted
            };
            var friend = new FriendService(Logger, _characterRelationDao!, TestHelpers.Instance.CharacterDao,
                _friendRequestHolder!, _connectedAccountHttpClient.Object, TestHelpers.Instance.LogLanguageLocalizer);
            _friendHttpClient.Setup(s => s.AddFriendAsync(It.IsAny<FriendShipRequest>()))
                .Returns(friend.AddFriendAsync(_session!.Character.CharacterId, finsPacket.CharacterId, finsPacket.Type));
            await _finsPacketHandler!.ExecuteAsync(finsPacket, _session).ConfigureAwait(false);

            Assert.IsFalse(_characterRelationDao!.LoadAll().Any());
        }

        [TestMethod]
        public async Task Test_Add_Not_Requested_FriendAsync()
        {
            var finsPacket = new FinsPacket
            {
                CharacterId = _targetSession!.Character.CharacterId,
                Type = FinsPacketType.Accepted
            };
            var friend = new FriendService(Logger, _characterRelationDao!, TestHelpers.Instance.CharacterDao,
                _friendRequestHolder!, _connectedAccountHttpClient.Object, TestHelpers.Instance.LogLanguageLocalizer);
            _friendHttpClient.Setup(s => s.AddFriendAsync(It.IsAny<FriendShipRequest>()))
                .Returns(friend.AddFriendAsync(_session!.Character.CharacterId, finsPacket.CharacterId, finsPacket.Type));

            await _finsPacketHandler!.ExecuteAsync(finsPacket, _session).ConfigureAwait(false);
            Assert.IsFalse(_characterRelationDao!.LoadAll().Any());
        }
    }
}