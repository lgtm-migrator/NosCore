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

using NosCore.Data.WebApi;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosCore.GameObject.HttpClients.PacketHttpClient
{
    public interface IPacketHttpClient
    {
        Task BroadcastPacketAsync(PostedPacket packet, long channelId);
        Task BroadcastPacketAsync(PostedPacket packet);
        Task BroadcastPacketsAsync(List<PostedPacket> packets);
        Task BroadcastPacketsAsync(List<PostedPacket> packets, long channelId);
    }
}