using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Xml;
using HeavyDuck.Utilities.Data;

namespace HeavyDuck.Eve.AssetManager
{
    internal static class Reporter
    {
        private const string REPORT_CSS = @"body { margin: 0; padding: 20px; background-color: #EEE; font: normal 10pt Verdana,sans-serif; } h1, p { margin: 0; } table { margin: 10px 0; border-collapse: collapse; background-color: white; font-size: 1em; } th, td { padding: 2px 4px; border: 1px solid #DDD; } tr.group th { font-weight: bold; text-align: left; padding-top: 5px; padding-bottom: 5px; color: white; background-color: #333; } tr.subgroup th { text-align: left; font-weight: bold; } .r { text-align: right; } .l { font-size: larger; }";

        public static void GenerateLoadoutReport(DataTable data, string outputPath)
        {
            DataView view;
            XmlWriter writer;
            DataColumn flagNameColumn, slotOrderColumn, classColumn;

            // stealthily modify the assets table so we can sort the slots in the order we want
            flagNameColumn = data.Columns["flagName"];
            slotOrderColumn = data.Columns.Add("slotOrder", typeof(string));
            classColumn = data.Columns.Add("slotClass", typeof(string));
            foreach (DataRow row in data.Rows)
            {
                string flag = row[flagNameColumn].ToString().ToLower();
                if (flag.StartsWith("hislot"))
                {
                    row[slotOrderColumn] = "1" + flag;
                    row[classColumn] = "hislot";
                }
                else if (flag.StartsWith("medslot"))
                {
                    row[slotOrderColumn] = "2" + flag;
                    row[classColumn] = "medslot";
                }
                else if (flag.StartsWith("loslot"))
                {
                    row[slotOrderColumn] = "3" + flag;
                    row[classColumn] = "loslot";
                }
                else if (flag.StartsWith("rigslot"))
                {
                    row[slotOrderColumn] = "4" + flag;
                    row[classColumn] = "rigslot";
                }
                else if (flag == "dronebay")
                {
                    row[slotOrderColumn] = "5" + flag;
                    row[classColumn] = flag;
                }
                else
                {
                    row[slotOrderColumn] = flag;
                    row[classColumn] = flag;
                }
            }

            // create a view with the sort we need
            view = new DataView(data, null, "characterName, locationName, containerName, slotOrder, typeName", DataViewRowState.CurrentRows);

            // open the output file
            using (FileStream output = File.Open(outputPath, FileMode.Create, FileAccess.Write))
            {
                // configure writer
                writer = CreateWriter(output);

                // start the document
                writer.WriteStartElement("html");
                writer.WriteStartElement("head");
                writer.WriteElementString("title", "Ship Loadouts");

                // CSS
                WriteCss(writer, REPORT_CSS);
                WriteCss(writer, "tr.hislot th { background-color: #FDD; } tr.medslot th { background-color: #CDF; } tr.loslot th { background-color: #FFC; } tr.dronebay th { background-color: #EFE; } tr.rigslot th { background-color: #FCF; } tr.cargo th,tr.cargo td { background-color: #EEE; }");

                // finish ye olde head
                writer.WriteEndElement(); // head

                // start body and write standard stuff
                writer.WriteStartElement("body");
                writer.WriteElementString("h1", "Ship Loadouts");
                WriteVersionP(writer);
                writer.WriteStartElement("table");

                // the group variables
                string currentShip = null;
                string currentSlotClass = null;

                // loop through the rows
                for (int i = 0; i < view.Count; ++i)
                {
                    string ship, slotClass;

                    // read stuff we are grouping by
                    ship = view[i]["containerName"].ToString();
                    slotClass = view[i]["slotClass"].ToString();

                    // check whether we've found a new group
                    if (ship != currentShip)
                    {
                        // register the new group
                        currentShip = ship;
                        currentSlotClass = null;

                        // write the group row
                        writer.WriteStartElement("tr");
                        writer.WriteAttributeString("class", "group");
                        writer.WriteStartElement("th");
                        writer.WriteAttributeString("colspan", "3");
                        writer.WriteRaw(string.Format("{0}'s <span class=\"l\">{1}</span> {2} in {3}", view[i]["characterName"], ship.Substring(0, ship.LastIndexOf('#') - 1), ship.Substring(ship.LastIndexOf('#')), view[i]["locationName"]));
                        writer.WriteEndElement(); // th
                        writer.WriteEndElement(); // tr
                    }

                    // check if we're starting a new slot group
                    if (slotClass != currentSlotClass)
                    {
                        currentSlotClass = slotClass;

                        // write the slot group row
                        writer.WriteStartElement("tr");
                        writer.WriteAttributeString("class", "subgroup " + slotClass);
                        writer.WriteStartElement("th");
                        writer.WriteAttributeString("colspan", "3");
                        writer.WriteString(slotClass);
                        writer.WriteEndElement(); // th
                        writer.WriteEndElement(); // tr
                    }

                    // item row
                    writer.WriteStartElement("tr");
                    if (slotClass == "cargo") writer.WriteAttributeString("class", "cargo");
                    writer.WriteStartElement("td");
                    writer.WriteAttributeString("class", "r");
                    if (view[i]["singleton"].Equals(false)) writer.WriteString(FormatInt64(view[i]["quantity"]));
                    writer.WriteEndElement(); // td
                    writer.WriteElementString("td", view[i]["typeName"].ToString());
                    writer.WriteElementString("td", view[i]["groupName"].ToString());
                    writer.WriteEndElement(); // tr
                }

                // finish the document
                writer.WriteEndDocument();
                writer.Flush();
            }
        }

        public static void GenerateMaterialReport(DataTable data, string outputPath)
        {
            XmlWriter writer;
            DataTable summary;
            DataRow summaryRow;
            DataView view, summaryView;

            // set up the summary data table
            summary = new DataTable("Material Summary");
            summary.Columns.Add("typeName", typeof(string));
            summary.Columns.Add("groupName", typeof(string));
            summary.Columns.Add("quantity", typeof(long));
            summary.Columns["quantity"].DefaultValue = 0;
            summary.PrimaryKey = new DataColumn[] { summary.Columns["typeName"] };
            
            // group calculate the source data
            data = TableHelper.GroupBy(data, "groupName, typeName, locationName", "quantity");

            // create the view
            view = new DataView(data, null, "locationName, groupName, typeName", DataViewRowState.CurrentRows);

            using (FileStream output = File.Open(outputPath, FileMode.Create, FileAccess.Write))
            {
                // create the writer
                writer = CreateWriter(output);

                // start the document
                writer.WriteStartElement("html");
                writer.WriteStartElement("head");
                writer.WriteElementString("title", "Materials");
                
                // CSS
                WriteCss(writer, REPORT_CSS);
                WriteCss(writer, ".subgroup th { background-color: #FFC; }");

                // finish the header
                writer.WriteEndElement(); // head

                // start ye body
                writer.WriteStartElement("body");
                writer.WriteElementString("h1", "Materials");
                WriteVersionP(writer);
                writer.WriteStartElement("table");

                // the stuff for grouping
                string currentLocation = null;
                string currentGroup = null;

                // loop
                foreach (DataRowView row in view)
                {
                    // read the grouping infos
                    string location = row["locationName"].ToString();
                    string group = row["groupName"].ToString();
                    string type = row["typeName"].ToString();

                    // check the big group
                    if (location != currentLocation)
                    {
                        currentLocation = location;
                        currentGroup = null;

                        // write the group row
                        writer.WriteStartElement("tr");
                        writer.WriteAttributeString("class", "group");
                        writer.WriteStartElement("th");
                        writer.WriteAttributeString("colspan", "3");
                        writer.WriteAttributeString("class", "l");
                        writer.WriteString(location);
                        writer.WriteEndElement(); // th
                        writer.WriteEndElement(); // tr
                    }

                    // check the little group
                    if (group != currentGroup)
                    {
                        currentGroup = group;

                        // write the subgroup row
                        WriteSubGroupRow(writer, group, 3);
                    }

                    // write the row
                    writer.WriteStartElement("tr");
                    writer.WriteElementString("td", type);
                    WriteElementStringWithClass(writer, "td", "r", FormatInt64(row["quantity"]));
                    WriteElementStringWithClass(writer, "td", "r", "$");
                    writer.WriteEndElement(); // tr

                    // sum
                    summaryRow = summary.Rows.Find(type);
                    if (summaryRow == null)
                    {
                        summaryRow = summary.NewRow();
                        summaryRow["groupName"] = group;
                        summaryRow["typeName"] = type;
                        summaryRow["quantity"] = row["quantity"];

                        summary.Rows.Add(summaryRow);
                    }
                    else
                    {
                        summaryRow["quantity"] = Convert.ToInt64(summaryRow["quantity"]) + Convert.ToInt64(row["quantity"]);
                    }
                }

                // create the summary view
                summaryView = new DataView(summary, null, "groupName, typeName", DataViewRowState.CurrentRows);

                // write the summary header
                writer.WriteStartElement("tr");
                writer.WriteAttributeString("class", "group");
                writer.WriteStartElement("th");
                writer.WriteAttributeString("colspan", "3");
                writer.WriteAttributeString("class", "l");
                writer.WriteString("Summary");
                writer.WriteEndElement(); // th
                writer.WriteEndElement(); // tr

                // summary stuff
                currentGroup = null;

                // loop the summary rows
                foreach (DataRowView row in summaryView)
                {
                    // read the strings
                    string group = row["groupName"].ToString();
                    string type = row["typeName"].ToString();

                    // group
                    if (group != currentGroup)
                    {
                        currentGroup = group;

                        // write the subgroup row
                        WriteSubGroupRow(writer, group, 3);
                    }

                    // write the row
                    writer.WriteStartElement("tr");
                    writer.WriteElementString("td", type);
                    WriteElementStringWithClass(writer, "td", "r", FormatInt64(row["quantity"]));
                    WriteElementStringWithClass(writer, "td", "r", "$");
                    writer.WriteEndElement(); // tr
                }

                // finish
                writer.WriteEndDocument();
                writer.Flush();
            }
        }

        private static XmlWriter CreateWriter(Stream output)
        {
            XmlWriterSettings settings;

            // the settings
            settings = new XmlWriterSettings();
            settings.Encoding = new UTF8Encoding(false);
            settings.Indent = true;
            settings.IndentChars = "  ";
            settings.OmitXmlDeclaration = true;

            // the writer
            return XmlWriter.Create(output, settings);
        }

        private static void WriteCss(XmlWriter writer, string styles)
        {
            writer.WriteStartElement("style");
            writer.WriteAttributeString("type", "text/css");
            writer.WriteString(styles);
            writer.WriteEndElement(); // style
        }

        private static void WriteVersionP(XmlWriter writer)
        {
            DateTime eveTime = DateTime.Now.ToUniversalTime();

            writer.WriteStartElement("p");
            writer.WriteRaw(string.Format("Report generated by <a href=\"{0}\">EVE Asset Manager</a> {1} at {2:HH:mm} EVE time on {2:d MMMM yyyy}.", AboutForm.HOMEPAGE, AboutForm.GetVersionString(false), eveTime));
            writer.WriteEndElement(); // p
        }

        private static void WriteSubGroupRow(XmlWriter writer, string header, int colspan)
        {
            writer.WriteStartElement("tr");
            writer.WriteAttributeString("class", "subgroup");
            writer.WriteStartElement("th");
            writer.WriteAttributeString("colspan", colspan.ToString());
            writer.WriteString(header);
            writer.WriteEndElement(); // th
            writer.WriteEndElement(); // tr
        }

        private static void WriteElementStringWithClass(XmlWriter writer, string localName, string classAttribute, string value)
        {
            writer.WriteStartElement(localName);
            writer.WriteAttributeString("class", classAttribute);
            writer.WriteString(value);
            writer.WriteEndElement(); // localName
        }

        private static string FormatInt32(object value)
        {
            return Convert.ToInt32(value).ToString("#,##0");
        }

        private static string FormatInt64(object value)
        {
            return Convert.ToInt64(value).ToString("#,##0");
        }
    }
}
