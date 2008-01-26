using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Xml;

namespace HeavyDuck.Eve.AssetManager
{
    internal static class Reporter
    {
        private const string REPORT_CSS = @"body { margin: 0; padding: 20px; background-color: #EEE; font: normal 10pt Verdana,sans-serif; } h1, p { margin: 0; } table { margin: 10px 0; border-collapse: collapse; background-color: white; font-size: 1em; } th, td { padding: 2px 4px; border: 1px solid #DDD; } tr.group th { font-weight: bold; text-align: left; padding-top: 5px; padding-bottom: 5px; color: white; background-color: #333; } tr.subgroup th { text-align: left; font-weight: bold; } .r { text-align: right; }";

        public delegate string GroupHeaderFormatCallback(object groupValue, DataRowView row);

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
                        writer.WriteString(string.Format("{0}'s {1} in {2}", view[i]["characterName"], ship, view[i]["locationName"]));
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

                // let's make sure this gets all finished
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
            writer.WriteStartElement("p");
            writer.WriteRaw(string.Format("Report generated by <a href=\"{0}\">EVE Asset Manager</a> {1}", AboutForm.HOMEPAGE, AboutForm.GetVersionString(false)));
            writer.WriteEndElement(); // p
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
