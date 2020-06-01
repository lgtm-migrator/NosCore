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

using System.Linq;
using System.Threading.Tasks;
using NosCore.Packets.ServerPackets.Player;
using NosCore.Packets.ServerPackets.UI;
using NosCore.Data.Enumerations.I18N;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Extensions;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.Group;
using NosCore.Packets.Enumerations;

namespace NosCore.PacketHandlers.Game
{
    public class TitEqPacketHandler : PacketHandler<TitEqPacket>, IWorldPacketHandler
    {
        public override async Task ExecuteAsync(TitEqPacket titEqPacket, ClientSession session)
        {
            var tit = session.Character.Titles.FirstOrDefault(s => s.TitleType == titEqPacket.TitleId);
            if (tit == null)
            {
                return;
            }


            switch (titEqPacket.Mode)
            {
                case 1:
                    foreach (var title in session.Character.Titles.Where(s => s.TitleType != titEqPacket.TitleId))
                    {
                        title.Visible = false;
                    }
                    tit.Visible = !tit.Visible;
                    await session.SendPacketAsync(new InfoiPacket
                    {
                        Message = Game18NConstString.TitleChangedOrHidden
                    }).ConfigureAwait(false);
                    break;
                default:
                    foreach (var title in session.Character.Titles.Where(s => s.TitleType != titEqPacket.TitleId))
                    {
                        title.Active = false;
                    }
                    tit.Active = !tit.Active;
                    await session.SendPacketAsync(new InfoiPacket
                    {
                        Message = Game18NConstString.TitleEffectChangedOrDeactivated
                    }).ConfigureAwait(false);
                    break;
            }
            await session.Character.MapInstance.SendPacketAsync(session.Character.GenerateTitInfo()).ConfigureAwait(false);
            await session.Character.SendPacketAsync(session.Character.GenerateTitle()).ConfigureAwait(false);
        }
    }
}