﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using VieweD.Helpers.System;

namespace VieweD.Engine.Common
{
    public class EngineBase : IComparable<EngineBase>
    {
        /// <summary>
        /// The main project TabPage
        /// </summary>
        public PacketTabPage ParentTab { get; set; }

        /// <summary>
        /// Unique ID for this engine
        /// </summary>
        public virtual string EngineId { get; } = "null";

        /// <summary>
        /// Full name of the engine
        /// </summary>
        public virtual string EngineName { get; } = "NoName";

        /// <summary>
        /// Does this engine support XML-style rules
        /// </summary>
        public virtual bool HasRulesFile { get; } = false;

        /// <summary>
        /// Does this engine support manual decryption selection?
        /// The main program will try to pull the default value that is associated with the rules file if enabled
        /// </summary>
        public virtual bool HasDecrypt { get; } = false;

        /// <summary>
        /// List of possible Decryption handler IDs
        /// </summary>
        public virtual List<string> DecryptionHandlerList { get; } = new List<string>();

        /// <summary>
        /// List of supported file extension that this engine supports (extension, description)
        /// </summary>
        public virtual Dictionary<string,string> FileExtensions { get; protected set; } = new Dictionary<string,string>();

        /// <summary>
        /// List containing all data types for this engine to insert (old style)
        /// </summary>
        public virtual List<string> EditorDataTypes { get; protected set; } = new List<string>();

        /// <summary>
        /// List of names of possible tools that need to be added to the menu 
        /// </summary>
        public virtual List<string> ToolNamesList { get; protected set; } = new List<string>();

        /// <summary>
        /// This List can be used to return error messages back to the main program
        /// For example, if a file fails to load, it will display this
        /// </summary>
        public List<string> ErrorMessages { get; protected set; } = new List<string>();

        /// <summary>
        /// Name of a currently running Tool while it's parsing the packets
        /// </summary>
        public string CurrentRunningToolName { get; set; } = string.Empty;

        /// <summary>
        /// Lookup Data for this engine
        /// </summary>
        public readonly DataLookups DataLookups = new DataLookups();

        /// <summary>
        /// Checks if you are allowed to search by Packet Level
        /// </summary>
        public virtual bool AllowedPacketLevelSearch { get; } = false;

        /// <summary>
        /// Checks if you are allowed to search by Packet SyncId
        /// </summary>
        public virtual bool AllowedPacketSyncSearch { get; } = false;

        /// <summary>
        /// Minimum allowed packet id for searching
        /// </summary>
        public virtual ushort PacketIdMinimum { get; } = ushort.MinValue;

        /// <summary>
        /// Maximum allowed packet id for searching
        /// </summary>
        public virtual ushort PacketIdMaximum { get; } = ushort.MaxValue;

        public virtual byte PacketLevelMaximum { get; } = byte.MinValue;

        public static string DepthSpacerVertical = "⁞";
        public static string DepthSpacerHorizontalSingle = "── ";
        public static string DepthSpacerHorizontalTop    = "┌─ ";
        public static string DepthSpacerHorizontalMiddle = "├─ ";
        public static string DepthSpacerHorizontalBottom = "└─ ";

        public static string StripSpacer(string s)
        {
            string res = s;
            res = res.Replace(DepthSpacerVertical, "");
            res = res.Replace(DepthSpacerHorizontalSingle, "");
            res = res.Replace(DepthSpacerHorizontalTop, "");
            res = res.Replace(DepthSpacerHorizontalMiddle, "");
            res = res.Replace(DepthSpacerHorizontalBottom, "");
            res = res.Trim();
            return res;
        }

        /// <summary>
        /// Mapping to use to convert a target port number to a StreamId used by the parsers
        /// </summary>
        public Dictionary<ushort, byte> PortToStreamIdMapping = new Dictionary<ushort, byte>();

        /// <summary>
        /// Base creator, only used when adding loading/parsing the engines
        /// </summary>
        public EngineBase()
        {
            ParentTab = null;
        }

        /// <summary>
        /// Attach a engine to a project TabPage
        /// </summary>
        /// <param name="parent"></param>
        public EngineBase(PacketTabPage parent)
        {
            ParentTab = parent; // Is set to null in case of registration
        }
        
        /// <summary>
        /// Used for sorting by engine name in the main program
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        int IComparable<EngineBase>.CompareTo(EngineBase other)
        {
            return string.CompareOrdinal(this.EngineName, other.EngineName);
        }

        /// <summary>
        /// Does this engine support appending data to existing project
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        public virtual bool CanAppend(PacketTabPage project)
        {
            return false;
        }

        /// <summary>
        /// If you want to customize the general loading box (used when filling the listbox)
        /// </summary>
        /// <param name="text"></param>
        /// <param name="color"></param>
        public virtual void GetLoadListBoxFlavor(out string text, ref Color color)
        {
            // Nothing here
            text = "Populating Listbox ...";
        }

        /// <summary>
        /// Initialize Data Lookups based on EngineId
        /// </summary>
        public virtual void Init()
        {
            DataLookups.LoadLookups(EngineId);
        }

        /// <summary>
        /// Creates custom menu entries in the parse editor right-click popup menu
        /// </summary>
        /// <param name="miInsert">Popup menu "Insert/Replace" item to be used as root</param>
        /// <param name="editor">The editor form itself</param>
        public virtual void BuildEditorPopupMenu(ToolStripMenuItem miInsert, ParseEditorForm editor)
        {
            // Example:
            // var basic = editor.AddMenuItem(miInsert.DropDownItems, "Basic Types", "");
            // editor.AddMenuItem(basic.DropDownItems, "Byte (8 bit)", "byte%LOOKUP%;%POS%;%NAME%%COMMENT%","byte");
        }

        /// <summary>
        /// Handler that is called from the menu entries when clicked to create the actual text to be inserted
        /// </summary>
        /// <param name="source">Source string</param>
        /// <param name="posField">Current pos textBox value of the editor</param>
        /// <param name="nameField">Current fieldName textBox value of the editor</param>
        /// <param name="lookupField">Current lookup comboBox.Text value of the editor</param>
        /// <param name="commentField">Current comment textBox value of the editor</param>
        /// <returns>The modified string</returns>
        public virtual string EditorReplaceString(string source, string posField, string nameField, string lookupField, string commentField)
        {
            // Example
            // source = source.Replace("%NAME%", !string.IsNullOrWhiteSpace(nameField) ? nameField : "");
            
            return source;
        }

        /// <summary>
        /// Compile Raw Packet Data to fill in meta data (parse packet ID and such)
        /// </summary>
        /// <param name="packetData">Packet Data</param>
        /// <param name="packetLogFileFormats">Expected Sub-Format for this packet, mostly used for plain text formats</param>
        /// <returns></returns>
        public virtual bool CompileData(PacketData packetData, string packetLogFileFormats)
        {
            return false;
        }

        /// <summary>
        /// If there needs to be anything special handled outside of regular parsing (e.g. keep track of specific game values that are not required to view data), you can do this here
        /// </summary>
        /// <param name="packetData"></param>
        /// <param name="packetList"></param>
        public virtual void CompileSpecial(PacketData packetData, PacketList packetList)
        {
            
        }

        /// <summary>
        /// Returns a proper parser for this packet
        /// </summary>
        /// <param name="packetData"></param>
        /// <returns></returns>
        public virtual PacketParser GetParser(PacketData packetData)
        {
            return null;
        }

        /// <summary>
        /// Create a rules reading for target project
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        public virtual RulesReader CreateRulesReader(PacketTabPage parent)
        {
            return null;
        }

        /// <summary>
        /// Create a TabPage for use in the program settings dialog
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        public virtual EngineSettingsTab CreateSettingsTab(TabControl parent)
        {
            var newTab = new EngineSettingsTab(parent) { Text = EngineName };

            return newTab;
        }

        public virtual PacketRule CreatePacketRule(RulesGroup parent, byte streamId, byte level, ushort packetId, string description, XmlNode node)
        {
            return new PacketRule(parent, streamId, level, packetId, description, node);
        }

        public virtual bool LoadFromStream(PacketList packetList, Stream fileStream, string sourceFileName, string rulesFileName, string decryptVersion)
        {
            return false;
        }

        public virtual void RunTool(PacketTabPage currentTabPage, string toolName)
        {
            // Implement tools by overriding this
        }

        public virtual PacketRule CreateNewUserPacketRule(PacketTabPage currentTabPage, PacketLogTypes packetLogType, PacketFilterListEntry packetId, string packetName)
        {
            if ((currentTabPage == null) || (currentTabPage.PLLoaded == null) ||
                (currentTabPage.PLLoaded.Rules == null))
                return null;

            var currentRulesReader = currentTabPage.PLLoaded.Rules;
            var verifyPacket = currentRulesReader.GetPacketRule(packetLogType, packetId.StreamId, packetId.Level, (ushort)packetId.Id);

            // Already exists, don't create a new one
            if (verifyPacket != null)
                return verifyPacket;

            if (!currentRulesReader.RuleGroups.TryGetValue(packetId.StreamId, out var ruleGroup))
                return null;

            var packetGroupDirectionNode = packetLogType == PacketLogTypes.Incoming ? ruleGroup.S2C :
                packetLogType == PacketLogTypes.Outgoing ? ruleGroup.C2S : null;

            var directionSet = packetLogType == PacketLogTypes.Incoming ? currentRulesReader.S2C :
                packetLogType == PacketLogTypes.Outgoing ? currentRulesReader.C2S : null;

            if ((packetGroupDirectionNode == null) || (directionSet == null))
                return null;

            var doc = ruleGroup.RootNode.OwnerDocument;
            var newXmlElement = doc.CreateElement("packet", packetGroupDirectionNode.NamespaceURI);
            XmlHelper.AddAttribute(newXmlElement, "type", "0x" + packetId.Id.ToString("X3"));
            XmlHelper.AddAttribute(newXmlElement, "level", "0x" + packetId.Level.ToString("X2"));
            XmlHelper.AddAttribute(newXmlElement, "desc", packetName);

            newXmlElement.InnerXml = GetDefaultNewPacketRuleNodeContents(packetId);

            packetGroupDirectionNode.AppendChild(newXmlElement);

            var newPacketRule = CreatePacketRule(ruleGroup, packetId.StreamId, packetId.Level, (ushort)packetId.Id, packetName, newXmlElement);

            directionSet.Add(newPacketRule.LookupKey, newPacketRule);

            newPacketRule.Build();

            return newPacketRule;
        }

        public virtual string GetDefaultNewPacketRuleNodeContents(PacketFilterListEntry packetId)
        {
            return "<!-- New Packet -->";
        }

        /// <summary>
        /// Get the rules' stream id based on a port number
        /// </summary>
        /// <param name="port"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public byte GetExpectedStreamIdByPort(ushort port, byte defaultValue)
        {
            return PortToStreamIdMapping.TryGetValue(port, out var id) ? id : defaultValue;
        }

        /// <summary>
        /// Makes sure that a port is a valid option, adds it at the end if not yet registered
        /// </summary>
        /// <param name="port"></param>
        public void RegisterPort(ushort port)
        {
            if (!PortToStreamIdMapping.TryGetValue(port, out _))
                PortToStreamIdMapping.Add(port, (byte)PortToStreamIdMapping.Count);
        }
    }
}
