using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;

namespace IngameScript
{
    partial class Program
    {
        public List<IMyTerminalBlock> getGridBlocks()
        {
            List<IMyTerminalBlock> _blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(_blocks, block => Me.IsSameConstructAs(block));
            return _blocks;
        }
        public List<IMyTerminalBlock> getOffGridBlocks()
        {
            List<IMyTerminalBlock> _blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(_blocks, block => !Me.IsSameConstructAs(block));
            return _blocks;
        }

        
        public bool isContainer(IMyTerminalBlock block)
        {
            MyIni _ini = new MyIni();
            if (block.InventoryCount == 0) return false;
            if (block is IMyCargoContainer) return true;
            //if (_useAllCargoContainers) return true;
            // Check if explicitly enabled.
            MyIniParseResult result;
            if (!_ini.TryParse(block.CustomData, out result)) return false;
            return _ini.ContainsSection(INI_SECTION);
        }

        static Dictionary<TKey, TValue> ConvertToDictionary<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> source)
        {
            return source.ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        static string[] Split(string input)
        {
            List<string> result = new List<string>();
            int squareBracketCount = 0, curlyBracketCount = 0, parenthesesCount = 0, startIndex = 0;

            for (int i = 0; i < input.Length; i++)
            {
                UpdateBracketCounts(input[i], ref squareBracketCount, ref curlyBracketCount, ref parenthesesCount);

                if (input[i] == ',' && squareBracketCount == 0 && curlyBracketCount == 0 && parenthesesCount == 0)
                {
                    result.Add(input.Substring(startIndex, i - startIndex));
                    startIndex = i + 1;
                }
            }

            result.Add(input.Substring(startIndex)); // Add the remaining part of the string
            return result.ToArray();
        }

        private static void UpdateBracketCounts(char currentChar, ref int squareCount, ref int curlyCount, ref int parenthesesCount)
        {
            switch (currentChar)
            {
                case '[': squareCount++; break;
                case ']': squareCount--; break;
                case '{': curlyCount++; break;
                case '}': curlyCount--; break;
                case '(': parenthesesCount++; break;
                case ')': parenthesesCount--; break;
            }
        }

        #region IGC
        public void transmitToAllConnectedGrid(string channel, string header, string messageBody)
        {
            // Transmit the message to all connected grids
            IGC.SendBroadcastMessage(channel, $"{header}\n{messageBody}", TransmissionDistance.ConnectedConstructs);
        }
        public void transmitToAll(string channel, string header, string messageBody)
        {
            // Transmit the message to all connected grids
            IGC.SendBroadcastMessage(channel, $"{header}\n{messageBody}");
        }
        public void transmitToAllMaxAntennaRange(string channel, string header, string messageBody)
        {
            // Transmit the message to all connected grids
            IGC.SendBroadcastMessage(channel, $"{header}\n{messageBody}", TransmissionDistance.AntennaRelay);
        }

#endregion        
    }
}
