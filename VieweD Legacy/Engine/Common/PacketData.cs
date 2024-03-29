﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using PostSharp.Extensibility;

namespace VieweD.Engine.Common
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class PacketData
    {
        /// <summary>
        /// Owning PacketList of this PacketData
        /// </summary>
        public PacketList Parent { get; set; }
        
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        /// <summary>
        /// Reference tp the original PacketData in case this is a generated one
        /// </summary>
        public PacketData Creator { get; set; }

        // ReSharper disable once CollectionNeverUpdated.Global
        /// <summary>
        /// Text as represented by it's source (has no fixed format)
        /// </summary>
        public List<string> RawText { get; set; }
        
        /// <summary>
        /// HeaderText as shown in the Packet List
        /// </summary>
        public string HeaderText { get; set; }
        
        /// <summary>
        /// Original Header Text before it was processed, used for some dialogs
        /// </summary>
        public string OriginalHeaderText { get; set; }

        /// <summary>
        /// Actual data to parse
        /// </summary>
        public List<byte> RawBytes { get; set; }

        /// <summary>
        /// Direction of the data flow
        /// </summary>
        public PacketLogTypes PacketLogType { get; set; }

        /// <summary>
        /// Internal Packet Level (used by some engines)
        /// </summary>
        public byte PacketLevel { get; set; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        /// <summary>
        /// Original Packet Level before it was processed (used by some engines)
        /// </summary>
        public byte OriginalPacketLevel { get; set; }

        /// <summary>
        /// Internal Stream identifier that can be used by Engines.
        /// Can be used to differentiate between different connections within the same capture file
        /// </summary>
        public byte StreamId { get; set; }

        /// <summary>
        /// Internal Packet Id used by the Engine
        /// </summary>
        public ushort PacketId { get; set; }

        /// <summary>
        /// Size in bytes of the actual data
        /// </summary>
        public uint PacketDataSize { get; set; }

        /// <summary>
        /// Synchronization number that is used to mark packet data as belonging together
        /// </summary>
        public uint PacketSync { get; set; } // Only UInt16 is used in FFXI

        /// <summary>
        /// Actual timestamp of the capture data
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Virtualized timestamp that is interpolated for video linking in case there are multiple packets with the same timestamp
        /// </summary>
        public DateTime VirtualTimeStamp { get; set; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once MemberCanBePrivate.Global
        /// <summary>
        /// Original String that was parsed from capture data (for text log files)
        /// </summary>
        public string OriginalTimeString { get; set; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once MemberCanBePrivate.Global

        /// <summary>
        /// For FFXI: Contains the ZoneId extracted from the previous capture data that is relevant for this packet
        /// </summary>
        public ushort CapturedZoneId { get; set; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once MemberCanBePrivate.Global

        /// <summary>
        /// When true, the packet is marked as a failed parse (displays in red)
        /// </summary>
        public bool MarkedAsInvalid { get; set; }

        /// <summary>
        /// When true, this packet shows up as dimmed in the packet list (used for filters)
        /// </summary>
        public bool MarkedAsDimmed { get; set; }

        // ReSharper disable once InconsistentNaming
        /// <summary>
        /// The Packet Parser object attached to this packet data
        /// </summary>
        public PacketParser PP;

        private int _cursor;
        /// <summary>
        /// Current Byte location within the data
        /// Settings this will reset BitCursor to zero as well
        /// </summary>
        public int Cursor { get => _cursor; set { _cursor = value; BitCursor = 0; } }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once MemberCanBePrivate.Global
        /// <summary>
        /// Bit offset relative to the current Cursor byte
        /// This is used by the Get Bit functions
        /// </summary>
        public byte BitCursor { get; set; }

        public bool IsTruncatedByParser { get; set; } = false;

        /// <summary>
        /// One Packet Data Object
        /// </summary>
        /// <param name="parent"></param>
        public PacketData(PacketList parent)
        {
            Parent = parent;
            Creator = null; 
            RawText = new List<string>();
            HeaderText = "Unknown Header";
            OriginalHeaderText = "";
            RawBytes = new List<byte>();
            PacketLogType = PacketLogTypes.Unknown;
            PacketLevel = 0x00;
            PacketId = 0x000;
            PacketDataSize = 0x0000;
            PacketSync = 0x0000;
            StreamId = 0;
            TimeStamp = DateTime.MinValue;;
            VirtualTimeStamp = DateTime.MinValue;
            OriginalTimeString = "";
            CapturedZoneId = 0;
            PP = null;
            Cursor = 0;
            BitCursor = 0;
        }

        ~PacketData()
        {
            // Forcefully clean up some memory
            RawText.Clear();
            RawBytes.Clear();
        }

        /// <summary>
        /// Add a formatted hex text line as data (generic)
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public int AddRawLineAsBytes(string s)
        {
            // Removes spaces and tabs
            var simpleLine = s.Replace(" ", "").Replace("\t", "");

            // Data should start after the first pipeline symbol
            var dataStartPos = simpleLine.IndexOf("|", StringComparison.InvariantCulture) + 1;
            if (simpleLine.Length < dataStartPos + 32)
            {
                // Data seems too short
                return 0;
            }
            var dataString = simpleLine.Substring(dataStartPos, 32); // max 32 hex digits expect

            var c = 0;
            for (var i = 0; i <= 0xf; i++)
            {
                var h = dataString.Substring(i * 2, 2);
                if (h != "--")
                {
                    if (byte.TryParse(h, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
                        RawBytes.Add(b);
                    c++;
                }
            }
            return c;

            /* Example:
            //        1         2         3         4         5         6         7         8         9
            01234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890

            [2018-05-16 18:11:35] Outgoing packet 0x015:
                    |  0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F      | 0123456789ABCDEF
                -----------------------------------------------------  ----------------------
                  0 | 15 10 9E 00 CF 50 A0 C3 04 0E 1C C2 46 BF 33 43    0 | .....P......F.3C
                  1 | 00 00 02 00 5D 00 00 00 49 97 B8 69 00 00 00 00    1 | ....]...I..i....
            ...
                  5 | 00 00 00 00 -- -- -- -- -- -- -- -- -- -- -- --    5 | ....------------
            */
        }

        /// <summary>
        /// Add a formatted hex text line as data (packeteer specific)
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public int AddRawPacketeerLineAsBytes(string s)
        {
            /* Example:
            //           1         2         3         4         5         6         7         8         9
            // 0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
            // [C->S] Id: 001A | HeaderSize: 28
            //     1A 0E ED 24 D5 10 10 01 D5 00 00 00 00 00 00 00  ..í$Õ...Õ.......
            */

            if (s.StartsWith("--"))
            {
                // Still a comment, get out of here
                return 0;
            }

            if (s.Length < 51)
            {
                // Doesn't look like a correct format
                return 0;
            }

            var c = 0;
            for (var i = 0; i <= 0xf; i++)
            {
                var h = s.Substring(4 + (i * 3), 2);
                // If this fails, we're probably at the end of the packet
                // Unlike Windower, Packeteer doesn't add dashes for the blanks
                if ((h != "--") && (h != "  ") && (h != " "))
                {
                    if (byte.TryParse(h, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
                        RawBytes.Add(b);

                    c++;
                }
            }
            return c;
        }

        /// <summary>
        /// Add a hex string as data
        /// </summary>
        /// <param name="hexData"></param>
        /// <returns></returns>
        public int AddRawHexStringDataAsBytes(string hexData)
        {
            var res = 0;
            try
            {
                RawBytes.Clear();
                var dataLine = hexData.Replace(" ", "").Replace("\r", "").Replace("\n", "").Replace("\t", "");
                for (var i = 0; i < (dataLine.Length - 1); i += 2)
                {
                    var num = dataLine.Substring(i, 2);

                    if (byte.TryParse(num, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
                        RawBytes.Add(b);

                    res++;
                }
            }
            catch
            {
                // Do Nothing
            }
            return res;
        }

        /// <summary>
        /// Prints a byte array as a formatted string of 16 bytes per line including a header and offset
        /// </summary>
        /// <returns></returns>
        public string PrintRawBytesAsHex()
        {
            const int valuesPerRow = 16;
            var res = string.Empty;
            res += "   |  0  1  2  3   4  5  6  7   8  9  A  B   C  D  E  F\r\n";
            res += "---+----------------------------------------------------\r\n";
            var lineNumber = 0;
            for (var i = 0; i < RawBytes.Count; i++)
            {
                if ((i % valuesPerRow) == 0)
                    res += lineNumber.ToString("X2") + " | ";

                res += RawBytes[i].ToString("X2");

                if ((i % valuesPerRow) == (valuesPerRow - 1))
                {
                    res += "\r\n";
                    lineNumber++;
                }
                else
                {
                    res += " ";
                    if ((i % 4) == 3)
                        res += " "; // Extra space every 4 bytes
                }
            }
            return res;
        }

        public byte GetByteAtPos(int pos)
        {
            if (pos > (RawBytes.Count - 1))
                return 0;
            Cursor = pos + 1;
            return RawBytes[pos];
        }

        public sbyte GetSByteAtPos(int pos)
        {
            if (pos > (RawBytes.Count - 1))
                return 0;
            Cursor = pos + 1;
            return unchecked((sbyte)RawBytes[pos]);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public bool GetBitAtPos(int pos, int bit)
        {
            if ((pos > (RawBytes.Count - 1)) || ((bit < 0) || (bit > 7)))
                return false;
            var b = RawBytes[pos];
            var bitmask = (byte)(0x01 << bit);
            Cursor = pos;
            BitCursor = (byte)bit;
            return ((b & bitmask) != 0);
        }

        // ReSharper disable once BuiltInTypeReferenceStyle
        public ushort GetUInt16AtPos(int pos, bool reversed = false)
        {
            if (pos > (RawBytes.Count - 2))
                return 0;
            Cursor = pos + 2;
            var bytes = RawBytes.GetRange(pos, 2).ToArray();
            if (reversed)
                bytes = bytes.Reverse().ToArray();
            return BitConverter.ToUInt16(bytes, 0);
        }

        // ReSharper disable once BuiltInTypeReferenceStyle
        public short GetInt16AtPos(int pos, bool reversed = false)
        {
            if (pos > (RawBytes.Count - 2))
                return 0;
            Cursor = pos + 2;
            var bytes = RawBytes.GetRange(pos, 2).ToArray();
            if (reversed)
                bytes = bytes.Reverse().ToArray();
            return BitConverter.ToInt16(bytes, 0);
        }

        // ReSharper disable once BuiltInTypeReferenceStyle
        public uint GetUInt32AtPos(int pos, bool reversed = false)
        {
            if (pos > (RawBytes.Count - 4))
                return 0;
            Cursor = pos + 4;
            var bytes = RawBytes.GetRange(pos, 4).ToArray();
            if (reversed)
                bytes = bytes.Reverse().ToArray();
            return BitConverter.ToUInt32(bytes, 0);
        }

        // ReSharper disable once BuiltInTypeReferenceStyle
        public int GetInt32AtPos(int pos, bool reversed = false)
        {
            if (pos > (RawBytes.Count - 4))
                return 0;
            Cursor = pos + 4;
            var bytes = RawBytes.GetRange(pos, 4).ToArray();
            if (reversed)
                bytes = bytes.Reverse().ToArray();
            return BitConverter.ToInt32(bytes, 0);
        }

        // ReSharper disable once BuiltInTypeReferenceStyle
        public ulong GetUInt64AtPos(int pos, bool reversed = false)
        {
            if (pos > (RawBytes.Count - 8))
                return 0;
            Cursor = pos + 8;
            var bytes = RawBytes.GetRange(pos, 8).ToArray();
            if (reversed)
                bytes = bytes.Reverse().ToArray();
            return BitConverter.ToUInt64(bytes, 0);
        }

        // ReSharper disable once BuiltInTypeReferenceStyle
        public long GetInt64AtPos(int pos, bool reversed = false)
        {
            if (pos > (RawBytes.Count - 8))
                return 0;
            Cursor = pos + 8;
            var bytes = RawBytes.GetRange(pos, 8).ToArray();
            if (reversed)
                bytes = bytes.Reverse().ToArray();
            return BitConverter.ToInt64(bytes, 0);
        }


        public string GetTimeStampAtPos(int pos, bool reversed = false)
        {
            var res = "???";
            if (pos > (RawBytes.Count - 4))
                return res;

            try
            {
                var dt = GetUInt32AtPos(pos, reversed);
                // Unix timestamp is seconds past epoch
                var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(dt).ToLocalTime();
                //res = dtDateTime.ToLongDateString() + " " + dtDateTime.ToLongTimeString();
                res = dtDateTime.ToString("yyyy/MM(MMM)/dd - HH:mm:ss");
            }
            catch
            {
                res = "ERROR";
            }
            Cursor = pos + 4;
            return res;
        }

        public string GetStringAtPos(int pos, int maxSize = -1)
        {
            var res = string.Empty;
            var i = 0;
            while (((i + pos) < RawBytes.Count) && (RawBytes[pos + i] != 0) && ((maxSize == -1) || (res.Length < maxSize)))
            {
                res += (char)RawBytes[pos + i];
                i++;
            }
            if (maxSize < 0)
                Cursor = pos + res.Length;
            else
                Cursor = pos + maxSize;
            return res;
        }

        public string GetDataAtPos(int pos, int size)
        {
            var res = "";
            var i = 0;
            while (((i + pos) < RawBytes.Count) && (i < size) && (i < 256))
            {
                res += RawBytes[i + pos].ToString("X2") + " ";
                i++;
            }
            Cursor = pos + size;
            return res;
        }

        public byte[] GetDataBytesAtPos(int pos, int size)
        {
            var res = new List<byte>();
            var i = 0;
            while (((i + pos) < RawBytes.Count) && (i < size) && (i < 256))
            {
                res.Add(RawBytes[i + pos]);
                i++;
            }
            Cursor = pos + size;
            return res.ToArray();
        }

        // ReSharper disable once InconsistentNaming
        public string GetIP4AtPos(int pos, bool reversed = false)
        {
            if (pos > RawBytes.Count - 4)
                return "";
            Cursor = pos + 4;
            if (reversed)
                return RawBytes[pos + 3] + "." + RawBytes[pos + 2] + "." + RawBytes[pos + 1] + "." + RawBytes[pos + 0];
            else
                return RawBytes[pos + 0] + "." + RawBytes[pos + 1] + "." + RawBytes[pos + 2] + "." + RawBytes[pos + 3];
        }

        // ReSharper disable once BuiltInTypeReferenceStyle
        [SuppressMessage("ReSharper", "BuiltInTypeReferenceStyle")]
        public long GetBitsAtPos(int pos, int bitOffset, int bitsSize)
        {
            long res = 0;
            var p = pos;
            var b = bitOffset;
            var restBits = bitsSize;
            // Minimum 1 bit
            if (restBits < 1)
                restBits = 1;
            Int64 mask = 1;
            while (restBits > 0)
            {
                while (b >= 8)
                {
                    b -= 8;
                    p++;
                }
                // Add Mask Value if Bit is set
                if (GetBitAtPos(p, b))
                    res += mask;
                restBits--;
                mask <<= 1;
                b++;
            }
            return res;
        }

        // ReSharper disable once BuiltInTypeReferenceStyle
        public Int64 GetBitsAtBitPos(int bitOffset, int bitsSize)
        {
            return GetBitsAtPos(bitOffset / 8, bitOffset % 8, bitsSize);
        }

        /// <summary>
        /// Special String encoding used by FFXI
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="encoded6BitKey"></param>
        /// <returns></returns>
        public string GetPackedString16AtPos(int pos, char[] encoded6BitKey)
        {
            var res = "";
            // Hex: B8 81 68 24  72 14 4F 10  54 0C 8F 00  00 00 00 00
            // Bits:
            // 101110 00
            // 1000 0001
            // 01 101000
            // 001001 00
            // 0111 0010
            // 00 010100
            // 010011 11
            // 0001 0000
            // 01 010100
            // 000011 00
            // 1000 1111
            // 00 000000

            // PackedString: TheNightsWatch (with no spaces)
            // PackedNum: 2E 08 05 ...
            // 101110  T
            // 001000  h
            // 000101  e
            //

            // A_  6F F0    011011 11-1111 0000  =>  1B 3F 00  =>  A
            // B_  73 F0    011100 11-1111 0000  =>  1C 3F 00  =>  B
            // F_  83 F0    100000 11-1111 0000  =>  20 3F 00  =>  F

            // EncodeLSStr : Array [0..63] of Char = (
            // #0 ,'a','b','c','d','e','f','g',  'h','i','j','k','l','m','n','o', // $00
            // 'p','q','r','s','t','u','v','w',  'x','y','z','A','B','C','D','E', // $10
            // 'F','G','H','I','J','K','L','M',  'N','O','P','Q','R','S','T','U', // $20
            // 'V','W','X','Y','Z',' ',' ',' ',  ' ',' ',' ',' ',' ',' ',' ', #0  // $30
            //  0   1   2   3   4   5   6   7     8   9   A   B   C   D   E   F
            // );
            var offset = 0;
            while ((offset / 8) < 15)
            {
                byte encodedChar = 0;
                byte bitMask = 0b00100000;
                for (var bit = 0; bit < 6; bit++)
                {
                    bool isSet = GetBitAtPos(pos + ((offset + bit) / 8), 7 - ((offset + bit) % 8));
                    if (isSet)
                        encodedChar += bitMask;
                    bitMask >>= 1;
                }
                // GetBitsAtPos(pos + (Offset / 8), (Offset % 8), 6);
                if ((encodedChar >= encoded6BitKey.Length) || (encodedChar <= 0))
                    break;
                var c = encoded6BitKey[encodedChar];
                if (c == 0)
                    break;
                res += c;
                offset += 6;
            }
            return res;
        }

        // ReSharper disable once BuiltInTypeReferenceStyle
        public float GetFloatAtPos(int pos, bool reversed = false)
        {
            if (pos > (RawBytes.Count - 4))
                return 0f;
            Cursor = pos + 4;
            var bytes = RawBytes.GetRange(pos, 4).ToArray();
            if (reversed)
                bytes = bytes.Reverse().ToArray();
            return BitConverter.ToSingle(bytes, 0);
        }

        // ReSharper disable once BuiltInTypeReferenceStyle
        public double GetDoubleAtPos(int pos, bool reversed = false)
        {
            if (pos > (RawBytes.Count - 8))
                return 0.0;
            Cursor = pos + 8;
            var bytes = RawBytes.GetRange(pos, 8).ToArray();
            if (reversed)
                bytes = bytes.Reverse().ToArray();
            return BitConverter.ToDouble(bytes, 0);
        }

        public int FindByte(byte aByte)
        {
            return RawBytes.IndexOf(aByte);
        }

        // ReSharper disable once BuiltInTypeReferenceStyle
        public int FindUInt16(ushort aUInt16, bool reversed = false)
        {
            for (var i = 0; i < (RawBytes.Count - 2); i++)
            {
                if (GetUInt16AtPos(i, reversed) == aUInt16)
                    return i;
            }
            return -1;
        }

        // ReSharper disable once BuiltInTypeReferenceStyle
        public int FindUInt32(uint aUInt32, bool reversed = false)
        {
            for (var i = 0; i < (RawBytes.Count - 4); i++)
            {
                if (GetUInt32AtPos(i, reversed) == aUInt32)
                    return i;
            }
            return -1;
        }

        // ReSharper disable once BuiltInTypeReferenceStyle
        public int FindUInt64(ulong aUInt64, bool reversed = false)
        {
            for (int i = 0; i < (RawBytes.Count - 8); i++)
            {
                if (GetUInt64AtPos(i, reversed) == aUInt64)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Parses a string to a DateTime using "yyyy-mm-dd HH:nn:ss" format
        /// </summary>
        /// <param name="s"></param>
        /// <param name="res"></param>
        /// <returns></returns>
        public bool DateTimeParse(string s, out DateTime res)
        {
            res = DateTime.MinValue;

            if (s.Length != 19)
                return false;

            try
            {
                // 0         1
                // 01234567890123456789
                // 2018-05-16 18:11:35
                var yyyy = int.Parse(s.Substring(0, 4));
                var mm = int.Parse(s.Substring(5, 2));
                var dd = int.Parse(s.Substring(8, 2));
                var hh = int.Parse(s.Substring(11, 2));
                var nn = int.Parse(s.Substring(14, 2));
                var ss = int.Parse(s.Substring(17, 2));
                res = new DateTime(yyyy, mm, dd, hh, nn, ss);
                return true;
            }
            catch
            {
                // Ignored
            }

            return false;
        }

        public bool CompileData(string packetLogFileFormats)
        {
            return Parent?.ParentTab?.Engine?.CompileData(this, packetLogFileFormats) ?? false;
        }

        public void CompileSpecial(PacketList packetList)
        {
            Parent?.ParentTab?.Engine?.CompileSpecial(this, packetList);
        }

        public bool MatchesSearch(SearchParameters p)
        {
            if ((PacketLogType == PacketLogTypes.Incoming) && (!p.SearchIncoming))
                return false;
            if ((PacketLogType == PacketLogTypes.Outgoing) && (!p.SearchOutgoing))
                return false;

            var res = true;

            if (p.SearchByPacketId)
                res = (PacketId == p.SearchPacketId);

            if ((res) && (p.SearchByPacketLevel))
                res = ((PacketLevel == p.SearchPacketLevel) || (OriginalPacketLevel == p.SearchPacketLevel));

            if ((res) && (p.SearchBySync))
                res = (PacketSync == p.SearchSync);

            if ((res) && (p.SearchByByte))
                res = (RawBytes.IndexOf(p.SearchByte) >= 0);

            if ((res) && (p.SearchByUInt16))
            {
                res = false;
                for (var i = 0; i < RawBytes.Count - 2; i++)
                {
                    var n = GetUInt16AtPos(i);
                    if (n != p.SearchUInt16) 
                        continue;

                    res = true;
                    break;
                }
            }

            if ((res) && (p.SearchByUInt24))
            {
                res = false;
                for (var i = 0; i < RawBytes.Count - 3; i++)
                {
                    var rd = GetDataBytesAtPos(i, 3).ToList();
                    rd.Add(0);
                    var d = BitConverter.ToUInt32(rd.ToArray(), 0);
                    if (d != p.SearchUInt24) 
                        continue;

                    res = true;
                    break;
                }
            }

            if ((res) && (p.SearchByUInt32))
            {
                res = false;
                for (var i = 0; i < RawBytes.Count - 4; i++)
                {
                    var n = GetUInt32AtPos(i);
                    if (n != p.SearchUInt32) 
                        continue;

                    res = true;
                    break;
                }
            }

            if ((res) && (PP != null) && (p.SearchByParsedData) && (p.SearchParsedFieldValue != string.Empty))
            {
                res = false;
                foreach (var f in PP.ParsedView)
                {
                    if (p.SearchParsedFieldName != string.Empty)
                    {
                        // Field Name Specified
                        res = (f.Var.ToLower().Contains(p.SearchParsedFieldName) && f.Data.ToLower().Contains(p.SearchParsedFieldValue));
                    }
                    else
                    {
                        // No field name defined
                        res = f.Data.ToLower().Contains(p.SearchParsedFieldValue);
                    }
                    if (res)
                        break;
                }
            }

            return res;
        }

        public virtual string GenerateDataExportName()
        {
            var exportName = "";
            switch (PacketLogType)
            {
                case PacketLogTypes.Incoming:
                    exportName += "i";
                    break;
                case PacketLogTypes.Outgoing:
                    exportName += "o";
                    break;
                case PacketLogTypes.Unknown:
                default:
                    exportName += "u";
                    break;
            }

            exportName += PacketLevel + "-" + PacketId.ToString("X4");
            return exportName;
        }
    }
}