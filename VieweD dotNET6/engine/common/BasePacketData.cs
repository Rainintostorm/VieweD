﻿using VieweD.Helpers.System;
using VieweD.Properties;

namespace VieweD.engine.common;

public class BasePacketData
{
    public ViewedProjectTab ParentProject { get; set; }
    public int ThisIndex { get; set; }
    public bool IsVisible { get; set; }
    public bool MarkedAsDimmed { get; set; }
    public bool MarkedAsInvalid { get; set; }
    public uint PacketId { get; set; }
    public int SyncId { get; set; }
    public string HeaderText { get; set; }
    public string OriginalHeaderText { get; set; }
    public PacketDataDirection PacketDataDirection { get; set; }
    public List<byte> ByteData { get; set; }
    public int PacketDataSize { get; set; }
    private int _cursor;
    private byte _bitCursor;

    public int Cursor
    {
        get => _cursor;
        set
        {
            _cursor = value;
            BitCursor = 0;
        }
    }

    public byte BitCursor
    {
        get => _bitCursor;
        set
        {
            var addBytes = (value >= 8) ? value / 8 : 0;
            if (addBytes > 0) Cursor += addBytes;
            _bitCursor = (byte)(value % 8);
        }
    }

    public List<ParsedField> ParsedData { get; set; }
    public List<string> SourceText { get; set; }
    public string SourceMac { get; set; }
    public string SourceIp { get; set; }
    public ushort SourcePort { get; set; }
    public string DestinationMac { get; set; }
    public string DestinationIp { get; set; }
    public ushort DestinationPort { get; set; }
    public DateTime TimeStamp { get; set; }
    public DateTime VirtualTimeStamp { get; set; }

    public BasePacketData(ViewedProjectTab parentProject)
    {
        ParentProject = parentProject;
        ThisIndex = -1;
        IsVisible = true;
        MarkedAsDimmed = false;
        MarkedAsInvalid = false;
        PacketId = 0;
        SyncId = 0;
        HeaderText = "Header";
        OriginalHeaderText = HeaderText;
        PacketDataDirection = PacketDataDirection.Incoming;
        ByteData = new List<byte>();
        PacketDataSize = 0;
        ParsedData = new List<ParsedField>();
        SourceText = new List<string>();
        SourceMac = string.Empty;
        SourceIp = string.Empty;
        SourcePort = 0;
        DestinationMac = string.Empty;
        DestinationIp = string.Empty;
        DestinationPort = 0;
        TimeStamp = DateTime.MinValue;
        VirtualTimeStamp = DateTime.MinValue;
        Cursor = 0;
        BitCursor = 0;
    }

    public ParsedField? GetParsedFieldByByteIndex(int index, bool preferSelectedFields = false)
    {
        // If enabled, check in selected fields first
        if (preferSelectedFields)
        {
            foreach (var parsed in ParsedData)
            {
                if ((parsed.HasValue == false) || (parsed.IsSelected == false))
                    continue;

                if ((index >= parsed.StartingByte) && (index <= parsed.EndingByte))
                    return parsed;
            }
        }

        // Do a normal first come, first served
        foreach (var parsed in ParsedData)
        {
            if (!parsed.HasValue) 
                continue;

            if ((index >= parsed.StartingByte) && (index <= parsed.EndingByte)) 
                return parsed;
        }

        return null;
    }

    public override string ToString()
    {
        return HeaderText;
    }

    public byte GetByteAtPos(int pos)
    {
        if (pos > (ByteData.Count - 1)) return 0;
        Cursor = pos + 1;
        return ByteData[pos];
    }

    public sbyte GetSByteAtPos(int pos)
    {
        if (pos > (ByteData.Count - 1)) return 0;
        Cursor = pos + 1;
        return unchecked((sbyte)ByteData[pos]);
    }

    public bool GetBitAtPos(int pos, int bit)
    {
        if ((pos > (ByteData.Count - 1)) || ((bit < 0) || (bit > 7))) return false;
        var b = ByteData[pos];
        var bitmask = (byte)(0x01 << bit);
        Cursor = pos;
        BitCursor = (byte)(bit + 1);
        return ((b & bitmask) != 0);
    }

    public ushort GetUInt16AtPos(int pos, bool reversed = false)
    {
        if (pos > (ByteData.Count - 2)) return 0;
        Cursor = pos + 2;
        var bytes = ByteData.GetRange(pos, 2).ToArray();
        if (reversed) bytes = bytes.Reverse().ToArray();
        return BitConverter.ToUInt16(bytes, 0);
    }

    public short GetInt16AtPos(int pos, bool reversed = false)
    {
        if (pos > (ByteData.Count - 2)) return 0;
        Cursor = pos + 2;
        var bytes = ByteData.GetRange(pos, 2).ToArray();
        if (reversed) bytes = bytes.Reverse().ToArray();
        return BitConverter.ToInt16(bytes, 0);
    }

    public uint GetUInt32AtPos(int pos, bool reversed = false)
    {
        if (pos > (ByteData.Count - 4)) return 0;
        Cursor = pos + 4;
        var bytes = ByteData.GetRange(pos, 4).ToArray();
        if (reversed) bytes = bytes.Reverse().ToArray();
        return BitConverter.ToUInt32(bytes, 0);
    }

    public int GetInt32AtPos(int pos, bool reversed = false)
    {
        if (pos > (ByteData.Count - 4)) return 0;
        Cursor = pos + 4;
        var bytes = ByteData.GetRange(pos, 4).ToArray();
        if (reversed) bytes = bytes.Reverse().ToArray();
        return BitConverter.ToInt32(bytes, 0);
    }

    public ulong GetUInt64AtPos(int pos, bool reversed = false)
    {
        if (pos > (ByteData.Count - 8)) return 0;
        Cursor = pos + 8;
        var bytes = ByteData.GetRange(pos, 8).ToArray();
        if (reversed) bytes = bytes.Reverse().ToArray();
        return BitConverter.ToUInt64(bytes, 0);
    }

    public long GetInt64AtPos(int pos, bool reversed = false)
    {
        if (pos > (ByteData.Count - 8)) return 0;
        Cursor = pos + 8;
        var bytes = ByteData.GetRange(pos, 8).ToArray();
        if (reversed) bytes = bytes.Reverse().ToArray();
        return BitConverter.ToInt64(bytes, 0);
    }

    public string GetTimeStampAtPos(int pos, bool reversed = false)
    {
        var res = Resources.TypeUnknown;
        if (pos > (ByteData.Count - 4)) return res;
        try
        {
            var dt = GetUInt32AtPos(pos, reversed);
            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(dt).ToLocalTime();
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
        while (((i + pos) < ByteData.Count) && (ByteData[pos + i] != 0) && ((maxSize == -1) || (res.Length < maxSize)))
        {
            res += (char)ByteData[pos + i];
            i++;
        }

        if (maxSize < 0) Cursor = pos + res.Length;
        else Cursor = pos + maxSize;
        return res;
    }

    public string GetDataAtPos(int pos, int size)
    {
        var res = "";
        var i = 0;
        while (((i + pos) < ByteData.Count) && (i < size) && (i < 256))
        {
            res += ByteData[i + pos].ToString("X2") + " ";
            i++;
        }

        Cursor = pos + size;
        return res;
    }

    public byte[] GetDataBytesAtPos(int pos, int size)
    {
        var res = new List<byte>();
        var i = 0;
        while (((i + pos) < ByteData.Count) && (i < size) && (i < 256))
        {
            res.Add(ByteData[i + pos]);
            i++;
        }

        Cursor = pos + size;
        return res.ToArray();
    }

    public string GetIp4AtPos(int pos, bool reversed = false)
    {
        if (pos > ByteData.Count - 4) return "";
        Cursor = pos + 4;
        if (reversed)
            return ByteData[pos + 3] + "." + ByteData[pos + 2] + "." + ByteData[pos + 1] + "." + ByteData[pos + 0];
        else return ByteData[pos + 0] + "." + ByteData[pos + 1] + "." + ByteData[pos + 2] + "." + ByteData[pos + 3];
    }

    public long GetBitsAtPos(int pos, int bitOffset, int bitsSize)
    {
        long res = 0;
        var p = pos;
        var b = bitOffset;
        var restBits = bitsSize;
        if (restBits < 1) restBits = 1;
        Int64 mask = 1;
        while (restBits > 0)
        {
            while (b >= 8)
            {
                b -= 8;
                p++;
            }

            if (GetBitAtPos(p, b)) res += mask;
            restBits--;
            mask <<= 1;
            b++;
        }

        return res;
    }

    public long GetBitsAtBitPos(int bitOffset, int bitsSize)
    {
        return GetBitsAtPos(bitOffset / 8, bitOffset % 8, bitsSize);
    }

    public float GetFloatAtPos(int pos, bool reversed = false)
    {
        if (pos > (ByteData.Count - 4)) return 0f;
        Cursor = pos + 4;
        var bytes = ByteData.GetRange(pos, 4).ToArray();
        if (reversed) bytes = bytes.Reverse().ToArray();
        return BitConverter.ToSingle(bytes, 0);
    }

    public double GetDoubleAtPos(int pos, bool reversed = false)
    {
        if (pos > (ByteData.Count - 8)) return 0.0;
        Cursor = pos + 8;
        var bytes = ByteData.GetRange(pos, 8).ToArray();
        if (reversed) bytes = bytes.Reverse().ToArray();
        return BitConverter.ToDouble(bytes, 0);
    }

    public Half GetHalfAtPos(int pos, bool reversed = false)
    {
        if (pos > (ByteData.Count - 2)) return (Half)0f;
        Cursor = pos + 2;
        var bytes = ByteData.GetRange(pos, 2).ToArray();
        if (reversed) bytes = bytes.Reverse().ToArray();
        return BitConverter.ToHalf(bytes, 0);
    }

    public void AddParsedField(bool hasValue, int startByte, int endByte, string displayPosition, string displayName,
        string displayValue, int nestingDepth, Color? colorOverride = null)
    {
        var newParsed = new ParsedField()
        {
            HasValue = hasValue, StartingByte = startByte, EndingByte = endByte, DisplayedByteOffset = displayPosition,
            FieldName = displayName, FieldValue = displayValue, NestingDepth = nestingDepth
        };
        if (colorOverride != null)
        {
            newParsed.FieldColor = (Color)colorOverride;
        }
        else
        {
            var maxCol = Math.Min(PacketColors.DataColors.Count, Properties.Settings.Default.ColFieldCount);
            var colorId = (ParsedData.Count % maxCol);
            newParsed.FieldColor = PacketColors.DataColors[colorId];
        }

        ParsedData.Add(newParsed);
    }

    public void AddParsedError(string errorPosition, string errorName, string errorDescription, int nestingDepth,
        Color? colorOverride = null)
    {
        var col = colorOverride ?? Color.Red;
        AddParsedField(false, -1, -1, errorPosition, errorName, errorDescription, nestingDepth, col);
    }

    public void AddUnparsedFields()
    {
        var firstAdded = false;

        void AddFirstUnparsed()
        {
            if (firstAdded) return;
            firstAdded = true;
            AddParsedError("", "Unparsed", "Unparsed data below", 0);
        }

        for (int i = 0; i < ByteData.Count; i++)
        {
            var bytesLeft = ByteData.Count - i;
            if (bytesLeft > 0)
            {
                var p1 = GetParsedFieldByByteIndex(i);
                if (p1 == null)
                {
                    if (bytesLeft >= 2)
                    {
                        var p2 = GetParsedFieldByByteIndex(i + 1);
                        if (p2 == null)
                        {
                            var parseShort = true;
                            if (bytesLeft >= 4)
                            {
                                var p3 = GetParsedFieldByByteIndex(i + 2);
                                var p4 = GetParsedFieldByByteIndex(i + 3);
                                if ((p3 == null) && (p4 == null))
                                {
                                    parseShort = false;
                                    AddFirstUnparsed();
                                    var fourBytes = GetUInt32AtPos(i);
                                    AddParsedField(true, i, i + 3, i.ToHex(2), "??_uint32",
                                        fourBytes.ToHex() + " - " + fourBytes, 1);
                                    i += 3;
                                }
                            }

                            if (parseShort)
                            {
                                AddFirstUnparsed();
                                var twoBytes = GetUInt16AtPos(i);
                                AddParsedField(true, i, i + 1, i.ToHex(2), "??_uint16",
                                    twoBytes.ToHex() + " - " + twoBytes, 1);
                                i += 1;
                            }
                        }
                    }
                    else
                    {
                        AddFirstUnparsed();
                        var b = GetByteAtPos(i);
                        AddParsedField(true, i, i + 0, i.ToHex(2), "??_byte",
                            b.ToHex() + " - " + b, 1);
                    }
                }
            }
        }
    }

    public virtual string GetPacketName()
    {
        return GetPacketName(PacketDataDirection, PacketId);
    }

    public virtual string GetPacketName(PacketDataDirection direction, uint id)
    {
        switch (direction)
        {
            case PacketDataDirection.Unknown: return Resources.TypeUnknown;
            case PacketDataDirection.Outgoing:
                return ParentProject.DataLookup.NLU(DataLookups.LuPacketOut)
                    .GetValue(id, Resources.TypeUnknown);
            case PacketDataDirection.Incoming:
                return ParentProject.DataLookup.NLU(DataLookups.LuPacketIn)
                    .GetValue(id, Resources.TypeUnknown);
        }

        return "<error>";
    }

    public virtual void BuildHeaderText()
    {
        var timeString = string.Empty;
        if (TimeStamp.Ticks > 0) timeString = TimeStamp.ToString(ParentProject.TimeStampFormat);
        string directionString;
        switch (PacketDataDirection)
        {
            case PacketDataDirection.Outgoing:
                directionString = Resources.DirectionOut;
                break;
            case PacketDataDirection.Incoming:
                directionString = Resources.DirectionIn;
                break;
            default:
                directionString = Resources.DirectionUnknown;
                break;
        }

        HeaderText = timeString + " " + directionString + " " + PacketId.ToHex(3) + " - " + GetPacketName();
    }

    public Color GetDataColor(int fieldIndex)
    {
        if (fieldIndex < 0)
            fieldIndex = int.MaxValue + fieldIndex;

        if (PacketColors.DataColors.Count > 0)
            return PacketColors.DataColors[fieldIndex % PacketColors.DataColors.Count];

        return Color.MediumPurple;
    }

    public bool HasSelectedFields()
    {
        foreach (var parsedField in ParsedData)
        {
            if (parsedField.HasValue && parsedField.IsSelected)
                return true;
        }
        return false;
    }
}