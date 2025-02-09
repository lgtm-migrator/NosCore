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

using Json.Patch;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NosCore.Core.I18N;
using NosCore.Data.Enumerations.I18N;
using NosCore.Shared.Authentication;
using NosCore.Shared.Configuration;
using NosCore.Shared.Enumerations;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using NodaTime;
using NosCore.Core.Services.IdService;
using NosCore.Shared.I18N;

namespace NosCore.Core.Controllers
{
    [Route("api/[controller]")]
    [AuthorizeRole(AuthorityType.Root)]
    public class ChannelController : Controller
    {
        private readonly IOptions<WebApiConfiguration> _apiConfiguration;
        private readonly ILogger _logger;
        private long _id;
        private readonly IHasher _hasher;
        private readonly IClock _clock;
        private readonly IIdService<ChannelInfo> _channelInfoIdService;
        private readonly ILogLanguageLocalizer<LogLanguageKey> _logLanguage;

        public ChannelController(IOptions<WebApiConfiguration> apiConfiguration, ILogger logger, IHasher hasher, IClock clock, IIdService<ChannelInfo> channelInfoIdService, ILogLanguageLocalizer<LogLanguageKey> logLanguage)
        {
            _logger = logger;
            _apiConfiguration = apiConfiguration;
            _hasher = hasher;
            _clock = clock;
            _channelInfoIdService = channelInfoIdService;
            _logLanguage = logLanguage;
        }

        private string GenerateToken()
        {
            var claims = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "Server"),
                new Claim(ClaimTypes.Role, nameof(AuthorityType.Root))
            });
            var password = _hasher.Hash(_apiConfiguration.Value.Password!, _apiConfiguration.Value.Salt);

            var keyByteArray = Encoding.Default.GetBytes(password);
            var signinKey = new SymmetricSecurityKey(keyByteArray);
            var handler = new JwtSecurityTokenHandler();
            var securityToken = handler.CreateToken(new SecurityTokenDescriptor
            {
                Subject = claims,
                Issuer = "Issuer",
                Audience = "Audience",
                SigningCredentials = new SigningCredentials(signinKey, SecurityAlgorithms.HmacSha256Signature)
            });
            return handler.WriteToken(securityToken);
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult Connect([FromBody] Channel data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (!ModelState.IsValid)
            {
                _logger.Error(_logLanguage[LogLanguageKey.AUTHENTICATED_ERROR]);
                return BadRequest(BadRequest(_logLanguage[LogLanguageKey.AUTH_ERROR]));
            }

            if (data.MasterCommunication!.Password != _apiConfiguration.Value.Password)
            {
                _logger.Error(_logLanguage[LogLanguageKey.AUTHENTICATED_ERROR]);
                return BadRequest(_logLanguage[LogLanguageKey.AUTH_INCORRECT]);
            }

            _id = _channelInfoIdService.GetNextId();
            _logger.Debug(_logLanguage[LogLanguageKey.AUTHENTICATED_SUCCESS], _id.ToString(CultureInfo.CurrentCulture),
                data.ClientName);

            var serv = new ChannelInfo
            {
                Name = data.ClientName,
                Host = data.Host,
                Port = data.Port,
                DisplayPort = (ushort?)data.DisplayPort,
                DisplayHost = data.DisplayHost,
                IsMaintenance = data.StartInMaintenance,
                ServerId = data.ServerId,
                Id = _id,
                ConnectedAccountLimit = data.ConnectedAccountLimit,
                WebApi = data.WebApi,
                LastPing = _clock.GetCurrentInstant(),
                Type = data.ClientType,
            };

            _channelInfoIdService.Items[_id] = serv;
            data.ChannelId = _id;


            return Ok(new ConnectionInfo { Token = GenerateToken(), ChannelInfo = data });
        }

        [HttpPut]
        public IActionResult UpdateToken([FromBody] Channel data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var channel = _channelInfoIdService.Items.Values.First(s =>
                (s.Name == data.ClientName) && (s.Host == data.Host) && (s.Port == data.Port));
            _logger.Debug(_logLanguage[LogLanguageKey.TOKEN_UPDATED], channel.Id.ToString(CultureInfo.CurrentCulture),
                data.ClientName);
            return Ok(new ConnectionInfo { Token = GenerateToken(), ChannelInfo = data });
        }

        // GET api/channel
        [HttpGet]
#pragma warning disable CA1822 // Mark members as static
        public List<ChannelInfo> GetChannels(long? id)
#pragma warning restore CA1822 // Mark members as static
        {
            return id != null ? _channelInfoIdService.Items.Values.Where(s => s.Id == id).ToList() : _channelInfoIdService.Items.Values.ToList();
        }

        [HttpPatch]
        public HttpStatusCode PingUpdate(int id, [FromBody] JsonPatch data)
        {
            var chann = _channelInfoIdService.Items.Values.FirstOrDefault(s => s.Id == id);
            if (chann == null)
            {
                return HttpStatusCode.NotFound;
            }

            if ((chann.LastPing.Plus(Duration.FromSeconds(10)) < _clock.GetCurrentInstant()) && !Debugger.IsAttached)
            {
                _channelInfoIdService.Items.TryRemove(_id, out _);
                _logger.Warning(_logLanguage[LogLanguageKey.CONNECTION_LOST],
                    _id.ToString(CultureInfo.CurrentCulture));
                return HttpStatusCode.RequestTimeout;
            }

            var result = data?.Apply(JsonDocument.Parse(JsonSerializer.SerializeToUtf8Bytes(chann)).RootElement);
            _channelInfoIdService.Items[_channelInfoIdService.Items.First(s => s.Value.Id == id).Key] = JsonSerializer.Deserialize<ChannelInfo>(result!.Value.GetRawText())!;
            return HttpStatusCode.OK;
        }
    }
}