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
        private const string REPORT_CSS = @"body { margin: 0; padding: 20px; background-color: #EEE; font: normal 10pt Verdana,sans-serif; } table { border-collapse: collapse; background-color: white; border: 1px solid gray; font-size: 1em; } th, td { padding: 2px 4px; border: 1px solid gray; } tr.group th { font-weight: bold; text-align: left; padding-top: 5px; padding-bottom: 5px; background-color: #FFE; }";
        public delegate string GroupHeaderFormatCallback(object groupValue, DataRowView row);

        public static void GenerateHtmlReport(DataTable data, string outputPath, string title, string groupBy, string orderBy, GroupHeaderFormatCallback formatCallback, params string[] outputColumns)
        {
            DataView view;
            XmlWriter writer;
            XmlWriterSettings writerSettings;
            List<DataColumn> columns;
            string fullOrderBy;
            object currentGroup = null;

            // construct column list
            if (outputColumns == null)
            {
                columns = new List<DataColumn>(data.Columns.Count);
                foreach (DataColumn column in data.Columns)
                    columns.Add(column);
            }
            else
            {
                columns = new List<DataColumn>(outputColumns.Length);
                foreach (string name in outputColumns)
                    columns.Add(data.Columns[name]);
            }
                
            // create a view with the sort we need
            if (!orderBy.Contains(groupBy))
                fullOrderBy = string.Format("{0} ASC, {1}", groupBy, orderBy);
            else
                fullOrderBy = orderBy;
            view = new DataView(data, null, fullOrderBy, DataViewRowState.CurrentRows);

            // open the output file
            using (FileStream output = File.Open(outputPath, FileMode.Create, FileAccess.Write))
            {
                // configure writer
                writerSettings = new XmlWriterSettings();
                writerSettings.Encoding = new UTF8Encoding(false);
                writerSettings.Indent = true;
                writerSettings.IndentChars = "  ";
                writerSettings.OmitXmlDeclaration = true;
                writer = XmlWriter.Create(output, writerSettings);

                // start the document
                writer.WriteStartElement("html");
                writer.WriteStartElement("head");
                writer.WriteElementString("title", title);
                writer.WriteStartElement("style");
                writer.WriteAttributeString("type", "text/css");
                writer.WriteString(REPORT_CSS);
                writer.WriteEndElement(); // style
                writer.WriteEndElement(); // head
                writer.WriteStartElement("body");
                writer.WriteElementString("h1", title);
                writer.WriteStartElement("table");

                // loop through the rows
                for (int i = 0; i < view.Count; ++i)
                {
                    // check whether we've found a new group
                    if (!view[i][groupBy].Equals(currentGroup))
                    {
                        // swap to the new group
                        currentGroup = view[i][groupBy];

                        // write the group row
                        writer.WriteStartElement("tr");
                        writer.WriteAttributeString("class", "group");
                        writer.WriteStartElement("th");
                        writer.WriteAttributeString("colspan", columns.Count.ToString());
                        writer.WriteValue(formatCallback(currentGroup, view[i]));
                        writer.WriteEndElement(); // th
                        writer.WriteEndElement(); // tr
                    }

                    // item row
                    writer.WriteStartElement("tr");
                    foreach (DataColumn col in columns)
                        writer.WriteElementString("td", view[i][col.Ordinal].ToString());
                    writer.WriteEndElement(); // tr
                }

                // finish the document
                writer.WriteEndDocument();

                // let's make sure this gets all finished
                writer.Flush();
            }
        }
    }
}
