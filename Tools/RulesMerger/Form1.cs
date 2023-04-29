using System.Runtime.CompilerServices;
using System.Xml;

namespace RulesMerger
{
    public partial class Form1 : Form
    {
        private XmlDocument? TargetDoc { get; set; }
        private XmlDocument? SourceDoc1 { get; set; }
        private XmlDocument? SourceDoc2 { get; set; }


        public Form1()
        {
            InitializeComponent();
        }

        private XmlDocument? LoadDoc(string fileName, Label l)
        {
            try
            {
                var xml = new XmlDocument();
                xml.Load(fileName);
                l.Text = fileName;
                return xml;
            }
            catch
            {
                l.Text = @"<failed to load>";
                return null;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                TargetDoc = LoadDoc(openFileDialog1.FileName, label1);
                Properties.Settings.Default.TargetFile = openFileDialog1.FileName;
                Properties.Settings.Default.Save();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                SourceDoc1 = LoadDoc(openFileDialog1.FileName, label2);
                Properties.Settings.Default.SourceFile1 = openFileDialog1.FileName;
                Properties.Settings.Default.Save();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                SourceDoc2 = LoadDoc(openFileDialog1.FileName, label3);
                Properties.Settings.Default.SourceFile2 = openFileDialog1.FileName;
                Properties.Settings.Default.Save();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (File.Exists(Properties.Settings.Default.TargetFile))
                TargetDoc = LoadDoc(Properties.Settings.Default.TargetFile, label1);
            if (File.Exists(Properties.Settings.Default.SourceFile1))
                SourceDoc1 = LoadDoc(Properties.Settings.Default.SourceFile1, label2);
            if (File.Exists(Properties.Settings.Default.SourceFile2))
                SourceDoc2 = LoadDoc(Properties.Settings.Default.SourceFile2, label3);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (TargetDoc == null)
                return;

            treeView1.Nodes.Clear();
            var templateNode = treeView1.Nodes.Add("Templates");

            var ruleXmlNodes = TargetDoc.SelectNodes("/root/rule");
            if (ruleXmlNodes != null)
            {
                var ruleCounter = 0;
                foreach (XmlNode ruleXmlNode in ruleXmlNodes)
                {
                    ruleCounter++;
                    var portString = "";
                    var serverXmlNode = ruleXmlNode.SelectSingleNode("server");
                    if (serverXmlNode != null)
                    {
                        portString = serverXmlNode?.Attributes?.GetNamedItem("port")?.Value ?? "???";
                    }
                    var ruleNode = treeView1.Nodes.Add($"Rule #{ruleCounter} @ Port {portString}");

                    var s2cPacketNodes = ruleXmlNode.SelectNodes("s2c/packet");
                    var c2sPacketNodes = ruleXmlNode.SelectNodes("c2s/packet");
                    var s2cNode = ruleNode.Nodes.Add("S2C");
                    var c2sNode = ruleNode.Nodes.Add("C2S");

                    if (s2cPacketNodes != null)
                    {
                        foreach (XmlNode packetNode in s2cPacketNodes)
                        {
                            if (packetNode == null)
                                continue;

                            var packetType = packetNode?.Attributes?.GetNamedItem("type")?.Value ?? "0xFFFF";
                            var packetName = packetNode?.Attributes?.GetNamedItem("desc")?.Value ?? "unknown";
                            var node = s2cNode.Nodes.Add(packetName + " (" + packetType + ")");
                            node.Tag = packetNode;
                        }
                    }

                    if (c2sPacketNodes != null)
                    {
                        foreach (XmlNode packetNode in c2sPacketNodes)
                        {
                            if (packetNode == null)
                                continue;

                            var packetType = packetNode?.Attributes?.GetNamedItem("type")?.Value ?? "0xFFFF";
                            var packetName = packetNode?.Attributes?.GetNamedItem("desc")?.Value ?? "unknown";
                            var node = c2sNode.Nodes.Add(packetName + " (" + packetType + ")");
                            node.Tag = packetNode;
                        }
                    }

                }
            }

            // Sort the result by name
            treeView1.Sort();
        }
    }
}