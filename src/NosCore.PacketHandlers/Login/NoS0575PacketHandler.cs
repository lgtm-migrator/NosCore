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

using Microsoft.Extensions.Options;
using NosCore.Core.Configuration;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.LoginService;
using NosCore.Packets.ClientPackets.Login;
using NosCore.Shared.I18N;
using Serilog;
using System.Threading.Tasks;
using NosCore.GameObject.Services.LoginService;

namespace NosCore.PacketHandlers.Login
{
    public class NoS0575PacketHandler : PacketHandler<NoS0575Packet>, ILoginPacketHandler
    {
        private readonly ILogger _logger;
        private readonly IOptions<LoginConfiguration> _loginConfiguration;
        private readonly ILoginService _loginService;
        private readonly ILogLanguageLocalizer<LogLanguageKey> _logLanguage;

        public NoS0575PacketHandler(ILoginService loginService, IOptions<LoginConfiguration> loginConfiguration, ILogger logger, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        {
            _loginService = loginService;
            _loginConfiguration = loginConfiguration;
            _logger = logger;
            _logLanguage = logLanguage;
        }

        public override Task ExecuteAsync(NoS0575Packet packet, ClientSession clientSession)
        {
            if (!_loginConfiguration.Value.EnforceNewAuth)
            {
                return _loginService.LoginAsync(packet.Username, packet.Md5String!, packet.ClientVersion!, clientSession,
                    packet.Password!,
                    false, packet.RegionType);
            }

            _logger.Warning(_logLanguage[LogLanguageKey.TRY_OLD_AUTH], packet.Username);
            return Task.CompletedTask;

        }
    }
}