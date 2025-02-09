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

using NosCore.Data.Enumerations.Miniland;
using NosCore.GameObject;
using NosCore.GameObject.HttpClients.WarehouseHttpClient;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Services.ItemGenerationService.Item;
using NosCore.Packets.ClientPackets.Warehouse;
using System.Threading.Tasks;

namespace NosCore.PacketHandlers.Warehouse
{
    public class DepositPacketHandler : PacketHandler<DepositPacket>, IWorldPacketHandler
    {
        private readonly IWarehouseHttpClient _warehouseHttpClient;

        public DepositPacketHandler(IWarehouseHttpClient warehouseHttpClient)
        {
            _warehouseHttpClient = warehouseHttpClient;
        }

        public override Task ExecuteAsync(DepositPacket depositPacket, ClientSession clientSession)
        {
#pragma warning disable CS0612 //remove the pragma when the actual itemInstance is fetched
            IItemInstance itemInstance = new ItemInstance();
#pragma warning restore CS0612
            short slot = 0;
            return _warehouseHttpClient.DepositItemAsync(clientSession.Character.CharacterId,
                WarehouseType.Warehouse, itemInstance, slot);
        }
    }
}