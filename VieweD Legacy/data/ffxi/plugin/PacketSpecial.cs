﻿using System;
using VieweD.Engine.Common;

namespace VieweD.Engine.FFXI
{
    public static class CompileSpecialized
    {
        /// <summary>
        /// Client Connect
        /// </summary>
        /// <param name="pd"></param>
        /// <param name="pl"></param>
        public static void In0x00a(PacketData pd, PacketList pl)
        {
            pl.CurrentParsePlayerId = pd.GetUInt32AtPos(0x04);
            pl.CurrentParseZone = pd.GetUInt16AtPos(0x30);
            pl.CurrentParsePlayerName = pd.GetStringAtPos(0x84,16);

            if (GameViewForm.GV != null)
            {
                var GV = GameViewForm.GV;
                GV.gbPlayer.Text = "Player 0x" + pl.CurrentParsePlayerId.ToString("X8");
                GV.lPlayerName.Text = pl.CurrentParsePlayerName;
            }
        }

        /// <summary>
        /// NPC Interaction 1
        /// </summary>
        /// <param name="pd"></param>
        /// <param name="pl"></param>
        public static void In0x032(PacketData pd, PacketList pl)
        {
            pl.CurrentParseZone = pd.GetUInt16AtPos(0x0A);
        }

        /// <summary>
        /// NPC Interaction 2
        /// </summary>
        /// <param name="pd"></param>
        /// <param name="pl"></param>
        public static void In0x034(PacketData pd, PacketList pl)
        {
            pl.CurrentParseZone = pd.GetUInt16AtPos(0x2A);
        }

    }
}
