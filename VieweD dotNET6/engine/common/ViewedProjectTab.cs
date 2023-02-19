﻿using VieweD.Forms;
using VieweD.Helpers.System;
using System.Xml;
using VieweD.engine.serialize;
using VieweD.Properties;

namespace VieweD.engine.common;
public class ViewedProjectTab : TabPage
{
    // Visual Components
    internal FlickerFreeListBox PacketsListBox { get; }

    // Project Settings
    /// <summary>
    /// Directory of this project (derived from ProjectFile
    /// </summary>
    public string ProjectFolder => string.IsNullOrWhiteSpace(ProjectFile) ? "" : Path.GetDirectoryName(ProjectFile) ?? "";
    /// <summary>
    /// Name of this project (derived from ProjectFile)
    /// </summary>
    public string ProjectName => string.IsNullOrWhiteSpace(ProjectFile) ? "VieweD Project.pvd" : Path.GetFileNameWithoutExtension(ProjectFile);
    public string ProjectFile { get; set; }

    /// <summary>
    /// Actual file that has been loaded
    /// </summary>
    public string OpenedLogFile { get; set; }

    /// <summary>
    /// List of Data Packets
    /// </summary>
    public List<BasePacketData> LoadedPacketList { get; set; }

    /// <summary>
    /// Returns if project data has unsaved changes
    /// </summary>
    public bool IsDirty { get; set; }

    /// <summary>
    /// Current SyncId of selected Packet
    /// </summary>
    private int CurrentSyncId { get; set; }

    public BaseInputReader? InputReader { get; set; }
    public BaseParser? InputParser { get; set; }
    public DataLookups DataLookup { get; set; }
    public string TimeStampFormat { get; set; }

    /// <summary>
    /// Mapping to use to convert a target port number to a StreamId used by the parsers
    /// </summary>
    public Dictionary<ushort, byte> PortToStreamIdMapping { get; private set; } = new ();

    public ViewedProjectTab()
    {
        TimeStampFormat = "HH:mm:ss";

        #region CreatePacketListBox
        PacketsListBox = new FlickerFreeListBox();
        // Set ListBox Position
        PacketsListBox.Parent = this;
        PacketsListBox.Location = new Point(0, 0);
        PacketsListBox.Size = new Size(this.Width, this.Height);
        PacketsListBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        PacketsListBox.Dock = DockStyle.Fill;
        PacketsListBox.Font = Properties.Settings.Default.PacketListFont; // Add fixed sized font (to override the tab page itself)
        PacketsListBox.ItemHeight = (int)Math.Ceiling(PacketsListBox.Font.GetHeight());
        // lbPackets.Font = new Font("Consolas", 9); // Add fixed sized font (to override the tab page itself)
        PacketsListBox.DrawMode = DrawMode.OwnerDrawFixed;

        PacketsListBox.DrawItem += PacketsListBox_DrawItem;
        PacketsListBox.SelectedIndexChanged += PacketsListBox_SelectedIndexChanged;
        #endregion

        // Initialize Empty Project
        ProjectFile = Path.Combine(Directory.GetCurrentDirectory(), "project.pvd");
        OpenedLogFile = string.Empty;
        CurrentSyncId = -1;
        LoadedPacketList = new List<BasePacketData>();

        // Load Static Lookups
        DataLookup = new DataLookups();
        _ = DataLookup.LoadLookups("ffxi", true);

        ReIndexLoadedPackets();
        PopulateListBox();
        IsDirty = false;
        OnProjectDataChanged();
    }

    public void OnProjectDataChanged()
    {
        MainForm.Instance?.OnProjectDataChanged(this);
    }

    /// <summary>
    /// Recalculates all ThisIndex values for all items in the LoadedPacketList
    /// </summary>
    public void ReIndexLoadedPackets()
    {
        lock (LoadedPacketList)
        {
            for (var i = 0; i < LoadedPacketList.Count; i++)
            {
                LoadedPacketList[i].ThisIndex = i;
            }
        }
    }

    /// <summary>
    /// Updates the PacketListBox with the currently visible PacketData
    /// </summary>
    public void PopulateListBox()
    {
        Cursor = Cursors.WaitCursor;
        lock (LoadedPacketList)
        {
            PacketsListBox.BeginUpdate();
            PacketsListBox.Items.Clear();
            for (var i = 0; i < LoadedPacketList.Count; i++)
            {
                var packetData = LoadedPacketList[i];
                if (packetData.IsVisible == false)
                    continue;
                PacketsListBox.Items.Add(packetData);
                if ((i % 50) == 0)
                    OnPopulateProgressUpdate(i, LoadedPacketList.Count);
            }
            PacketsListBox.EndUpdate();
            OnPopulateProgressUpdate(1, 1);
        }
        Cursor = Cursors.Default;
    }

    /// <summary>
    /// SelectedIndexChanged event for the PacketListBox
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void PacketsListBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        PacketsListBox.Invalidate();
        if (PacketsListBox.Items[PacketsListBox.SelectedIndex] is BasePacketData pd)
        {
            CurrentSyncId = pd.SyncId;
            MainForm.Instance?.ShowPacketData(pd);
        }
        else
        {
            MainForm.Instance?.ShowPacketData(null);
        }
    }

    /// <summary>
    /// DrawItem event for the PacketListBox
    /// This is gives the Packet List it's flair
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void PacketsListBox_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (!(sender is ListBox lb))
            return;

        // Check if we are drawing inside out list bounds, if not, just draw the background
        if ((e.Index < 0) || (e.Index >= PacketsListBox.Items.Count))
        {
            e.DrawBackground();
            return;
        }

        // Check if the attached object of the list box item to be drawn is a BasePacketData object
        // If not, just draw the background
        if (PacketsListBox.Items[e.Index] is not BasePacketData pd)
        {
            // Draw the background of the ListBox control for each item that isn't a valid one.
            e.DrawBackground();
            return;
        }

        var barOn = CurrentSyncId == pd.SyncId;
        var isSelected = (e.Index == lb.SelectedIndex);
        Color textCol;
        Color backCol;
        Color barCol;

        // Determine the color of the brush to draw each item based on the index of the item to draw.
        var packetDirectionForColors = pd.PacketDataDirection;
        // If a packet is marked as invalid, always draw it the same as a unknown packet color
        if (pd.MarkedAsInvalid)
            packetDirectionForColors = PacketDataDirection.Unknown;

        switch (packetDirectionForColors)
        {
            case PacketDataDirection.Incoming:
                textCol = PacketColors.ColorFontIn;
                if (isSelected)
                {
                    backCol = PacketColors.ColorSelectIn;
                    textCol = PacketColors.ColorSelectedFontIn;
                }
                else
                if (barOn)
                    backCol = PacketColors.ColorSyncIn;
                else
                    backCol = PacketColors.ColorBackIn;
                barCol = PacketColors.ColorBarIn;
                break;
            case PacketDataDirection.Outgoing:
                textCol = PacketColors.ColorFontOut;
                if (isSelected)
                {
                    backCol = PacketColors.ColorSelectOut;
                    textCol = PacketColors.ColorSelectedFontOut;
                }
                else
                if (barOn)
                    backCol = PacketColors.ColorSyncOut;
                else
                    backCol = PacketColors.ColorBackOut;
                barCol = PacketColors.ColorBarOut;
                break;
            case PacketDataDirection.Unknown:
            default:
                textCol = PacketColors.ColorFontUnknown;
                if (isSelected)
                {
                    backCol = PacketColors.ColorSelectUnknown;
                    textCol = PacketColors.ColorSelectedFontUnknown;
                }
                else
                if (barOn)
                    backCol = PacketColors.ColorSyncUnknown;
                else
                    backCol = PacketColors.ColorBackUnknown;
                barCol = PacketColors.ColorBarUnknown;
                break;
        }

        // Change colors if the packet is "dimmed"
        if (pd.MarkedAsDimmed)
        {
            // Grab the background color's luminance to determine if it's considered a light or a dark color
            Colorspace.RgbToHls(backCol, out _, out var l, out _);
            var isDark = l < 0.5;

            // We use two different ways to adjust the background color based on if it's a light or dark color
            if (isDark)
                backCol = Color.FromArgb(backCol.A, backCol.R / 2, backCol.G / 2, backCol.B / 2);
            else
                backCol = Color.FromArgb(
                    backCol.A,
                    backCol.R > 128 ? 255 : backCol.R * 2,
                    backCol.G > 128 ? 255 : backCol.G * 2,
                    backCol.B > 128 ? 255 : backCol.B * 2);

            // Text color is always a RGB average of the foreground and background colors
            textCol = Color.FromArgb(textCol.A, (backCol.R + textCol.R) / 2, (backCol.G + textCol.G) / 2, (backCol.B + textCol.B) / 2);

            // NOTE: We currently don't change the bar color as it looks unusually weird if in the same sync
            // barCol = Color.FromArgb(barCol.A, barCol.R / 4, barCol.G / 4, barCol.B / 4);
        }

        // Create the brushes we need based on the selected colors
        Brush textBrush = new SolidBrush(textCol);
        Brush backBrush = new SolidBrush(backCol);
        Brush barBrush = new SolidBrush(barCol);

        // Draw the background of the ListBox control for each item.
        e.Graphics.FillRectangle(backBrush, e.Bounds);

        // Header text
        var s = lb.Items[e.Index].ToString();
        // s = pd.VirtualTimeStamp.ToString() + "." + pd.VirtualTimeStamp.Millisecond.ToString("0000");

        var icon1 = new Rectangle(e.Bounds.Left, e.Bounds.Top + ((e.Bounds.Height - Resources.mini_unk_icon.Height) / 2), Resources.mini_unk_icon.Width, Resources.mini_unk_icon.Height);
        var icon2 = icon1 with { X = icon1.Left + icon1.Width, Y = icon1.Top };

        // Draw the video strip icon if this packet is within a video segment
        if (IsInVideoTimeRange(DateTime.MaxValue))
        {
            e.Graphics.DrawImage(Resources.mini_video_icon, icon2);
        }

        // Change the text location based on the number of icons that we display
        Rectangle textBounds;
        if (HasVideoAttached())
        {
            textBounds = new Rectangle(e.Bounds.Left + (icon1.Width * 2), e.Bounds.Top, e.Bounds.Width - (icon1.Width * 2), e.Bounds.Height);
        }
        else
        {
            textBounds = new Rectangle(e.Bounds.Left + (icon1.Width), e.Bounds.Top, e.Bounds.Width - (icon1.Width), e.Bounds.Height);
        }

        switch (PacketColors.PacketListStyle)
        {
            case 1:
                // Colored arrows
                if (pd.PacketDataDirection == PacketDataDirection.Incoming)
                {
                    e.Graphics.DrawImage(Resources.mini_in_icon, icon1);
                }
                else
                if (pd.PacketDataDirection == PacketDataDirection.Outgoing)
                {
                    e.Graphics.DrawImage(Resources.mini_out_icon, icon1);
                }
                else
                {
                    e.Graphics.DrawImage(Resources.mini_unk_icon, icon1);
                }
                break;
            case 2:
                // transparent arrows
                if (pd.PacketDataDirection == PacketDataDirection.Incoming)
                {
                    e.Graphics.DrawImage(Resources.mini_in_ticon, icon1);
                }
                else
                if (pd.PacketDataDirection == PacketDataDirection.Outgoing)
                {
                    e.Graphics.DrawImage(Resources.mini_out_ticon, icon1);
                }
                else
                {
                    e.Graphics.DrawImage(Resources.mini_unk_ticon, icon1);
                }
                break;
            default:
                // No icons, just text
                textBounds = e.Bounds;
                if (pd.PacketDataDirection == PacketDataDirection.Incoming)
                {
                    s = "<= " + s;
                }
                else
                if (pd.PacketDataDirection == PacketDataDirection.Outgoing)
                {
                    s = "=> " + s;
                }
                else
                {
                    s = "?? " + s;
                }
                break;
        }

        // Draw the current item text based on the current Font and the custom brush settings.
        e.Graphics.DrawString(s, e.Font!, textBrush, textBounds, StringFormat.GenericDefault);

        // Check if the Sync Bar needs to be drawn
        if (barOn)
        {
            var barSize = 8;
            // If it's also the selected item, double the width
            if (isSelected)
                barSize = 16;
            e.Graphics.FillRectangle(barBrush, new Rectangle(e.Bounds.Right - barSize, e.Bounds.Top, barSize, e.Bounds.Height));
        }

        // If the ListBox has focus, draw a focus rectangle around the selected item.
        e.DrawFocusRectangle();
    }

    /// <summary>
    /// Returns true if given timeOffset falls instead a video section
    /// </summary>
    /// <param name="timeOffset"></param>
    /// <returns></returns>
    private bool IsInVideoTimeRange(DateTime timeOffset)
    {
        // TODO: Do actual calculations
        return false;
        //return timeOffset > DateTime.MinValue;
    }

    /// <summary>
    /// Returns if this project has at least one valid video segment attached
    /// </summary>
    /// <returns></returns>
    private bool HasVideoAttached()
    {
        // TODO: Do actual check
        return true;
    }

    /// <summary>
    /// Called when source has been opened and is ready to start adding data packets
    /// </summary>
    /// <param name="inputReader"></param>
    public virtual void OnInputSourceOpened(BaseInputReader inputReader)
    {
        lock (LoadedPacketList)
        {
            LoadedPacketList.Clear();
        }
    }

    /// <summary>
    /// Called when the InputReader failed to read or compile the source
    /// </summary>
    /// <param name="inputReader"></param>
    /// <param name="errorMessage"></param>
    public virtual void OnInputError(BaseInputReader inputReader, string errorMessage)
    {
        // Do nothing
    }

    /// <summary>
    /// Called when the input reader has finished reading the source, or if forcefully closed
    /// </summary>
    /// <param name="inputReader"></param>
    public virtual void OnInputSourceClosing(BaseInputReader inputReader)
    {
        ReIndexLoadedPackets();
        PopulateListBox();
    }

    /// <summary>
    /// Called when the input reader has parsed a new piece of packet data
    /// </summary>
    /// <param name="inputReader"></param>
    /// <param name="packetData"></param>
    public virtual void OnInputDataRead(BaseInputReader inputReader, BasePacketData packetData)
    {
        lock (LoadedPacketList)
        {
            LoadedPacketList.Add(packetData);
        }
    }

    public void OnInputProgressUpdate(BaseInputReader inputReader, int position, int maxValue)
    {
        // NOTE: it's possible to manipulate the value based on the reader
        MainForm.Instance?.UpdateStatusBarProgress(position, maxValue,null,null);
    }
    public void OnPopulateProgressUpdate(int position, int maxValue)
    {
        // NOTE: it's possible to manipulate the value based on the reader
        MainForm.Instance?.UpdateStatusBarProgress(position, maxValue, Resources.PopulateListBox, null);
    }

    public void OnParseProgressUpdate(BaseParser parser, int position, int maxValue)
    {
        // NOTE: it's possible to manipulate the value based on the reader
        MainForm.Instance?.UpdateStatusBarProgress(position, maxValue, Resources.ParsePackets, null);
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

    /// <summary>
    /// Opens the project settings dialog
    /// </summary>
    /// <returns>True if OK or Save was pressed</returns>
    public bool OpenSettings()
    {
        using var settings = new ProjectSettingsDialog();
        settings.AssignProject(this);
        return (settings.ShowDialog() == DialogResult.OK);
    }

    public bool PacketDataDirectionDialog(BasePacketData packetData, out PacketDataDirection selectedDirection)
    {
        // TODO: Make new Dialog Form
        selectedDirection = packetData.PacketDataDirection;
        return false;
        // Ask for type
        /*
        var askDlgRes = DialogResult.Cancel;
        using (PacketTypeSelectForm askDlg = new PacketTypeSelectForm())
        {
            askDlg.lHeaderData.Text = s;
            askDlgRes = askDlg.ShowDialog();
        }

        if (askDlgRes == DialogResult.Yes)
        {
            preferredDirection = PacketDataDirection.Incoming;
            isUndefinedPacketDirection = false;
            packetData.PacketDataDirection = preferredDirection;
        }
        else
        if (askDlgRes == DialogResult.No)
        {
            preferredDirection = PacketDataDirection.Outgoing;
            isUndefinedPacketDirection = false;
            packetData.PacketDataDirection = preferredDirection;
        }
        */
    }

    public bool SaveProjectSettingsFile(string fileName, string projectFolder)
    {
        try
        {
            var settings = new ProjectSettings();

            settings.ProjectFile = Helper.MakeRelative(projectFolder, ProjectFile);
            settings.LogFile = Helper.MakeRelative(projectFolder, OpenedLogFile);
            settings.InputReaderName = InputReader?.Name ?? "";
            settings.ParserName = InputParser?.Name ?? "";
            settings.RulesFile = Helper.MakeRelative(projectFolder, InputParser?.Rules?.LoadedRulesFileName ?? "");
            settings.VideoSettings.VideoFile = Helper.MakeRelative(projectFolder, "");

            var writer = new System.Xml.Serialization.XmlSerializer(typeof(ProjectSettings));

            using var fileStream = File.Create(fileName);

            writer.Serialize(fileStream, settings);
            fileStream.Close();
            return true;
        }
        catch
        {
            //
        }
        return false;
    }

    private ProjectSettings? LoadLegacyProjectSettings(List<string> sl)
    {
        var res = new ProjectSettings();
        try
        {
            foreach (string s in sl)
            {
                var fields = s.Split(';');
                if (fields.Length < 2)
                    continue;
                var fieldType = fields[0].ToLower();
                var fieldVal = fields[1];
                if (fieldType == "packetlog")
                {
                    res.LogFile = Helper.TryMakeFullPath(ProjectFolder, fieldVal);
                }
                else
                if (fieldType == "rules")
                {
                    res.RulesFile = Helper.TryMakeFullPath(ProjectFolder, fieldVal);
                }
                else
                if (fieldType == "video")
                {
                    res.VideoSettings.VideoFile = Helper.TryMakeFullPath(ProjectFolder, fieldVal);
                }
                else
                if (fieldType == "youtube")
                {
                    res.VideoSettings.VideoUrl = fieldVal;
                }
                else
                if (fieldType == "offset")
                {
                    if (NumberHelper.TryFieldParse(fieldVal, out int n))
                        res.VideoSettings.VideoOffset = TimeSpan.FromMilliseconds(n);
                }
                else
                if (fieldType == "packedsource")
                {
                    res.ProjectUrl = fieldVal;
                }
                else
                if (fieldType == "tags")
                {
                    res.Tags = fieldVal.Split(",").ToList();
                }
                else
                if (fieldType == "decrypt")
                {
                    res.DecryptionName = fieldVal;
                }
                /*
                else
                if (fieldType == "pin")
                {
                    // not used for reading inside this program
                }
                else
                if (fieldType == "pout")
                {
                    // not used for reading inside this program
                }
                else
                {
                    continue;
                }
                */
            }
        }
        catch
        {
            return null;
        }
        return res;
    }

    public ProjectSettings? LoadProjectSettingsFile(string fileName)
    {
        try
        {
            var testText = File.ReadAllText(fileName);
            var oldStyle = testText.Contains("packetlog;");
            var newStyle = testText.Contains("<?xml ");

            if (newStyle)
            {
                var reader = new System.Xml.Serialization.XmlSerializer(typeof(ProjectSettings));
                using var file = File.OpenRead(fileName);
                var res = reader.Deserialize(file) as ProjectSettings;
                file.Close();
                if (res != null)
                {
                    var pFolder = Path.GetDirectoryName(fileName) ?? "";
                    res.ProjectFile = Helper.TryMakeFullPath(pFolder, res.ProjectFile);
                    res.LogFile = Helper.TryMakeFullPath(pFolder, res.LogFile);
                    res.RulesFile = Helper.TryMakeFullPath(pFolder, res.RulesFile);
                    res.VideoSettings.VideoFile = Helper.TryMakeFullPath(pFolder, res.VideoSettings.VideoFile);
                }
                return res;
            }

            if (oldStyle)
            {
                var lines = File.ReadAllLines(fileName).ToList();
                var res = LoadLegacyProjectSettings(lines);
                if (res != null)
                {
                    // We managed to load a old format, backup the file
                    try
                    {
                        File.Copy(fileName, fileName + ".old");
                    }
                    catch
                    {
                        // Ignore
                    }
                }
                return res;
            }
        }
        catch
        {
            // Don't show a error, just return null
        }
        return null;
    }

}