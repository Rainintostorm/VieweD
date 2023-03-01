﻿namespace VieweD.engine.common;

public class ParsedField
{
    public bool HasValue { get; set; }
    public int StartingByte { get; set; }
    public int EndingByte { get; set; }
    public string DisplayedByteOffset { get; set; }
    public string FieldName { get; set; }
    public string FieldValue { get; set; }
    public int NestingDepth { get; set; }
    public Color FieldColor { get; set; }
    public bool IsSelected { get; set; }

    public ParsedField()
    {
        HasValue = false;
        StartingByte = -1;
        EndingByte = -1;
        DisplayedByteOffset = string.Empty;
        FieldName = string.Empty;
        FieldValue = string.Empty;
        NestingDepth = 0;
        FieldColor = SystemColors.WindowText;
        IsSelected = false;
    }

    public bool MatchSearch(SearchParameters search, BasePacketData data)
    {
        // nothing to search?
        if ((search.SearchOutgoing == false) && (search.SearchIncoming == false))
            return false;

        // Check if field name is searched
        var res = (search.SearchParsedFieldName == "") || FieldName.Contains(search.SearchParsedFieldName, StringComparison.InvariantCultureIgnoreCase);

        // Only field value is searched
        res = res && ((search.SearchParsedFieldValue == "") || FieldValue.Contains(search.SearchParsedFieldValue, StringComparison.InvariantCultureIgnoreCase));

        /*
        // check values
        if (HasValue)
        {
            res = res && ((!search.SearchByByte) || (data.FindByte(search.SearchByte)));
        }
        */

        return res;
    }
}