using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Text;
using System.Xml;
using HeavyDuck.Utilities.Data;

namespace HeavyDuck.Eve.AssetManager
{
    public delegate void GenerateReportDelegate(DataTable data, string title, string outputPath);

    internal static class Reporter
    {
        private const string REPORT_CSS = @"body { margin: 0; padding: 20px; background-color: #EEE; font: normal 10pt Verdana,sans-serif; } h1 { margin: 0; } p { margin: 0 0 8px 0; } table { margin: 10px 0; border-collapse: collapse; background-color: white; font-size: 1em; } th, td { padding: 2px 4px; border: 1px solid #DDD; } .group th { font-weight: bold; text-align: left; padding-top: 5px; padding-bottom: 5px; color: white; background-color: #333; } .subgroup th { text-align: left; font-weight: bold; } .bold, .bold td { font-weight: bold; } .error { color: red; font-weight: bold; } .r { text-align: right; } .l { font-size: larger; } .s { width: 2em; } .g { color: #CCC; }";
        private const string SUBGROUP_YELLOW_CSS = ".subgroup th { background-color: #FFC; }";
        private const string SUBGROUP_GRAY_CSS = ".subgroup th { color: white; background-color: #666; }";
        private const string QUESTION_HTML = "<small class=\"error\">?</small>";
        private const string ISK_HTML = "<small class=\"g\">ISK</small> ";

        public static void GenerateAssetsByLocationReport(DataTable data, string title, string outputPath)
        {
            GenerateGenericReport(data, title, outputPath, "locationName", "categoryName");
        }

        public static void GenerateAssetsByCategoryReport(DataTable data, string title, string outputPath)
        {
            GenerateGenericReport(data, title, outputPath, "categoryName", "groupName");
        }

        public static void GenerateGenericReport(DataTable data, string title, string outputPath, string groupColumn, string subGroupColumn)
        {
            DataView view;
            XmlWriter writer;

            // group by something
            data = TableHelper.GroupBy(data, groupColumn + ", " + subGroupColumn + ", typeName, typeID, _marketPriceUnit", "quantity");

            // create a view with the sort we need
            view = new DataView(data, null, groupColumn + ", " + subGroupColumn + ", typeName", DataViewRowState.CurrentRows);

            // open the output file
            using (FileStream output = File.Open(outputPath, FileMode.Create, FileAccess.Write))
            {
                // configure writer
                writer = CreateWriter(output);

                // start the document
                WriteToBody(writer, title, SUBGROUP_YELLOW_CSS);
                writer.WriteStartElement("table");

                // grouping variables
                string currentGroup = null;
                string currentSubGroup = null;
                Dictionary<string, long> quantityTotals = null;
                Dictionary<string, decimal> valueTotals = null;

                // loop through the rows
                foreach (DataRowView row in view)
                {
                    // read some values
                    string group = row[groupColumn].ToString();
                    string subGroup = row[subGroupColumn].ToString();
                    int typeID = Convert.ToInt32(row["typeID"]);
                    long quantity = Convert.ToInt64(row["quantity"]);
                    decimal? marketPrice = TableHelper.GetNullableValue<decimal>(row["_marketPriceUnit"]);
                    decimal value = quantity * Program.GetCompositePrice(EveTypes.Items[typeID], marketPrice);

                    // make group/subgroup names ??? if they are blank
                    if (string.IsNullOrEmpty(group)) group = "???";
                    if (string.IsNullOrEmpty(subGroup)) subGroup = "???";

                    // group
                    if (group != currentGroup)
                    {
                        // summary rows for previous group
                        if (currentGroup != null) WriteGenericTotalRows(writer, quantityTotals.Keys, quantityTotals, valueTotals);

                        // set the new group
                        currentGroup = group;
                        currentSubGroup = null;
                        quantityTotals = new Dictionary<string, long>();
                        valueTotals = new Dictionary<string, decimal>();

                        // write the group row
                        writer.WriteStartElement("tr");
                        writer.WriteAttributeString("class", "group");
                        writer.WriteStartElement("th");
                        writer.WriteAttributeString("colspan", "3");
                        writer.WriteString(group);
                        writer.WriteEndElement(); // th
                        writer.WriteEndElement(); // tr
                    }

                    // sub-group
                    if (subGroup != currentSubGroup)
                    {
                        currentSubGroup = subGroup;

                        WriteSubGroupRow(writer, subGroup, 3);
                    }

                    // calculate and sum value, quantity
                    if (quantityTotals.ContainsKey(subGroup))
                        quantityTotals[subGroup] += quantity;
                    else
                        quantityTotals[subGroup] = quantity;
                    if (valueTotals.ContainsKey(subGroup))
                        valueTotals[subGroup] += value;
                    else
                        valueTotals[subGroup] = value;

                    // the actual thingy row
                    writer.WriteStartElement("tr");
                    WriteElementStringWithClass(writer, "td", "r", FormatInt64(quantity));
                    writer.WriteElementString("td", row["typeName"].ToString());
                    WriteElementRawWithClass(writer, "td", "r", ISK_HTML + FormatDecimal1(value));
                    writer.WriteEndElement();
                }

                // summary rows for last group, if any
                if (currentGroup != null) WriteGenericTotalRows(writer, quantityTotals.Keys, quantityTotals, valueTotals);

                // finish the document
                writer.WriteEndDocument();
                writer.Flush();
            }
        }

        private static void WriteGenericTotalRows(XmlWriter writer, IEnumerable<string> items, IDictionary<string, long> quantityTotals, IDictionary<string, decimal> valueTotals)
        {
            List<string> sorted;
            long grandTotalQuantity = 0;
            decimal grandTotalValue = 0;

            // write the header
            WriteSubGroupRow(writer, "Total", 3);

            // sort the items
            sorted = new List<string>(items);
            sorted.Sort();

            // iterate them
            foreach (string key in sorted)
            {
                grandTotalQuantity += quantityTotals[key];
                    grandTotalValue += valueTotals[key];

                writer.WriteStartElement("tr");
                WriteElementStringWithClass(writer, "td", "r", FormatInt64(quantityTotals[key]));
                writer.WriteElementString("td", key);
                WriteElementRawWithClass(writer, "td", "r", ISK_HTML + FormatDecimal1(valueTotals[key]));
                writer.WriteEndElement(); // tr
            }

            // write the grand total
            writer.WriteStartElement("tr");
            writer.WriteAttributeString("class", "bold");
            WriteElementStringWithClass(writer, "td", "r", FormatInt64(grandTotalQuantity));
            writer.WriteElementString("td", "Grand Total");
            WriteElementRawWithClass(writer, "td", "r", ISK_HTML + FormatDecimal1(grandTotalValue));
            writer.WriteEndElement(); // tr
        }

        public static void GenerateLoadoutReport(DataTable data, string title, string outputPath)
        {
            DataView view;
            XmlWriter writer;
            DataColumn flagNameColumn, slotOrderColumn, classColumn;

            // remove all the rows we don't care about
            PruneTable(data, "containerCategory IS NOT NULL AND containerCategory = 'Ship'");

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
            view = new DataView(data, null, "containerName, locationName, containerID, slotOrder, typeName", DataViewRowState.CurrentRows);

            // open the output file
            using (FileStream output = File.Open(outputPath, FileMode.Create, FileAccess.Write))
            {
                // configure writer
                writer = CreateWriter(output);

                // start the document
                WriteToBody(writer, title, "tr.hislot th { background-color: #FDD; } tr.medslot th { background-color: #CDF; } tr.loslot th { background-color: #FFC; } tr.dronebay th { background-color: #EFE; } tr.rigslot th { background-color: #FCF; } tr.cargo th,tr.cargo td { background-color: #EEE; }");
                writer.WriteStartElement("table");

                // the group variables
                long currentShipID = -1;
                string currentSlotClass = null;
                string location;

                // loop through the rows
                for (int i = 0; i < view.Count; ++i)
                {
                    long shipID;
                    string shipName, slotClass;

                    // read stuff we are grouping by
                    shipID = Convert.ToInt64(view[i]["containerID"]);
                    shipName = view[i]["containerName"].ToString();
                    slotClass = view[i]["slotClass"].ToString();

                    // check whether we've found a new group
                    if (shipID != currentShipID)
                    {
                        // register the new group
                        currentShipID = shipID;
                        currentSlotClass = null;

                        // the location name might be null, show "???" instead
                        location = view[i]["locationName"].ToString();
                        if (string.IsNullOrEmpty(location)) location = "???";

                        // write the group row
                        writer.WriteStartElement("tr");
                        writer.WriteAttributeString("class", "group");
                        writer.WriteStartElement("th");
                        writer.WriteAttributeString("colspan", "3");
                        writer.WriteRaw(string.Format("<span class=\"l\">{1}</span> #{2} / {0} / {3}", view[i]["characterName"], shipName, shipID, location));
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

        public static void GenerateMaterialReport(DataTable data, string title, string outputPath)
        {
            XmlWriter writer;
            DataTable summary;
            DataRow summaryRow;
            DataView view, summaryView;

            // remove all the rows we don't care about
            PruneTable(data, "categoryName = 'Material'");

            // set up the summary data table
            summary = new DataTable("Material Summary");
            summary.Columns.Add("typeName", typeof(string));
            summary.Columns.Add("groupName", typeof(string));
            summary.Columns.Add("quantity", typeof(long));
            summary.Columns.Add("value", typeof(decimal));
            summary.Columns["quantity"].DefaultValue = 0;
            summary.Columns["value"].DefaultValue = 0;
            summary.PrimaryKey = new DataColumn[] { summary.Columns["typeName"] };
            
            // group calculate the source data
            data = TableHelper.GroupBy(data, "groupName, typeID, typeName, _marketPriceUnit, locationName", "quantity");

            // create the view
            view = new DataView(data, null, "locationName, groupName, typeName", DataViewRowState.CurrentRows);

            using (FileStream output = File.Open(outputPath, FileMode.Create, FileAccess.Write))
            {
                // create the writer
                writer = CreateWriter(output);

                // start writing
                WriteToBody(writer, title, SUBGROUP_YELLOW_CSS);
                writer.WriteStartElement("table");

                // the stuff for grouping
                string currentLocation = null;
                string currentGroup = null;

                // loop
                foreach (DataRowView row in view)
                {
                    // read the infos
                    string location = row["locationName"].ToString();
                    string group = row["groupName"].ToString();
                    string type = row["typeName"].ToString();
                    int typeID = Convert.ToInt32(row["typeID"]);
                    long quantity = Convert.ToInt64(row["quantity"]);
                    decimal? marketPrice = TableHelper.GetNullableValue<decimal>(row["_marketPriceUnit"]);
                    decimal value = quantity * Program.GetCompositePrice(EveTypes.Items[typeID], marketPrice);

                    // tweak blank locations to say "???" instead
                    if (string.IsNullOrEmpty(location)) location = "???";

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
                    WriteElementStringWithClass(writer, "td", "r", FormatInt64(quantity));
                    writer.WriteElementString("td", type);
                    WriteElementRawWithClass(writer, "td", "r", FormatIskValue(value));
                    writer.WriteEndElement(); // tr

                    // sum
                    summaryRow = summary.Rows.Find(type);
                    if (summaryRow == null)
                    {
                        summaryRow = summary.NewRow();
                        summaryRow["groupName"] = group;
                        summaryRow["typeName"] = type;
                        summaryRow["quantity"] = quantity;
                        summaryRow["value"] = value;

                        summary.Rows.Add(summaryRow);
                    }
                    else
                    {
                        summaryRow["quantity"] = Convert.ToInt64(summaryRow["quantity"]) + quantity;
                        summaryRow["value"] = Convert.ToDecimal(summaryRow["value"]) + value;
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
                    long quantity = Convert.ToInt64(row["quantity"]);
                    decimal value = Convert.ToDecimal(row["value"]);

                    // group
                    if (group != currentGroup)
                    {
                        currentGroup = group;

                        // write the subgroup row
                        WriteSubGroupRow(writer, group, 3);
                    }

                    // write the row
                    writer.WriteStartElement("tr");
                    WriteElementStringWithClass(writer, "td", "r", FormatInt64(quantity));
                    writer.WriteElementString("td", type);
                    WriteElementRawWithClass(writer, "td", "r", FormatIskValue(value));
                    writer.WriteEndElement(); // tr
                }

                // do yet another groupby down to each, err, group
                long totalItems = 0;
                decimal totalValue = 0;
                DataTable groupTotals = TableHelper.GroupBy(summary, "groupName", "quantity", "value");
                DataView groupTotalsView = new DataView(groupTotals, null, "groupName", DataViewRowState.CurrentRows);

                // summary total
                WriteSubGroupRow(writer, "Grand Total", 3);
                foreach (DataRowView row in groupTotalsView)
                {
                    string group = row["groupName"].ToString();
                    long quantity = Convert.ToInt64(row["quantity"]);
                    decimal value = Convert.ToDecimal(row["value"]);

                    // totals
                    totalItems += quantity;
                    totalValue += value;

                    // row
                    writer.WriteStartElement("tr");
                    WriteElementStringWithClass(writer, "td", "r", FormatInt64(quantity));
                    writer.WriteElementString("td", group);
                    WriteElementRawWithClass(writer, "td", "r", FormatIskValue(value));
                    writer.WriteEndElement(); // tr
                }
                writer.WriteStartElement("tr");
                writer.WriteAttributeString("class", "bold");
                WriteElementStringWithClass(writer, "td", "r", FormatInt64(totalItems));
                writer.WriteElementString("td", "All Materials");
                WriteElementRawWithClass(writer, "td", "r", FormatIskValue(totalValue));
                writer.WriteEndElement(); // tr

                // finish
                writer.WriteEndDocument();
                writer.Flush();
            }
        }

        public static void GeneratePosReport(DataTable data, string title, string outputPath)
        {
            XmlWriter writer;
            DataView view;
            DataTable fuelData;

            // first, query the fuel data from the static db
            using (SQLiteConnection conn = new SQLiteConnection(Program.CcpDatabaseConnectionString))
            {
                conn.Open();
                fuelData = new DataTable("Fuel Data");

                // query fuels
                using (SQLiteDataAdapter adapter = new SQLiteDataAdapter("SELECT * FROM invControlTowerResources r JOIN invControlTowerResourcePurposes p ON p.purpose = r.purpose JOIN invTypes t ON t.typeID = r.resourceTypeID", conn))
                    adapter.Fill(fuelData);

                // load fuel prices
                AssetCache.LoadMarketPrices(fuelData, fuelData.Columns["resourceTypeID"], null);

                // set primary key
                fuelData.PrimaryKey = new DataColumn[] { fuelData.Columns["controlTowerTypeID"], fuelData.Columns["resourceTypeID"] };
            }

            // create the view
            view = new DataView(data, "containerGroup = 'Control Tower'", "locationName, containerName", DataViewRowState.CurrentRows);

            // begin writing the actual report
            using (FileStream output = File.Open(outputPath, FileMode.Create, FileAccess.Write))
            {
                // create the writer
                writer = CreateWriter(output);

                // start writing
                WriteToBody(writer, title, SUBGROUP_GRAY_CSS, ".low td { color: #600; background-color: #FEC; } .empty td { background-color: #FCC; }");
                writer.WriteStartElement("table");

                // grouping info
                string currentTower = null;
                Dictionary<int, long> itemCounts = new Dictionary<int, long>();
                DataRow[] fuelRows = null;

                // loopity
                foreach (DataRowView row in view)
                {
                    // read data
                    string tower = row["containerName"].ToString();
                    int typeID = Convert.ToInt32(row["typeID"]);
                    long quantity = Convert.ToInt64(row["quantity"]);
                    
                    // group
                    if (tower != currentTower)
                    {
                        // actually write the rows for the previous tower, if any
                        if (currentTower != null) WritePosReportGroup(writer, itemCounts, fuelRows);

                        // read a couple more pieces of data about the new tower
                        string location = row["locationName"].ToString();
                        int locationID = Convert.ToInt32(row["locationID"]);
                        int towerTypeID = Convert.ToInt32(row["containerTypeID"]);
                        long? factionID;
                        decimal security;

                        // get the factionID and security level of the solar system
                        using (SQLiteConnection conn = new SQLiteConnection(Program.CcpDatabaseConnectionString))
                        {
                            conn.Open();

                            using (SQLiteCommand cmd = new SQLiteCommand("SELECT s.security, r.factionID FROM mapSolarSystems s JOIN mapRegions r ON r.regionID = s.regionID WHERE s.solarSystemID = " + locationID.ToString(), conn))
                            {
                                using (SQLiteDataReader reader = cmd.ExecuteReader())
                                {
                                    reader.Read();
                                    security = Convert.ToDecimal(reader[0]);
                                    factionID = reader.IsDBNull(1) ? (long?)null : Convert.ToInt64(reader[1]);
                                }
                            }
                        }

                        // build our little fuel row select
                        StringBuilder filter = new StringBuilder();
                        filter.AppendFormat("controlTowerTypeID = {0}", towerTypeID);
                        filter.AppendFormat(" AND (minSecurityLevel IS NULL OR minSecurityLevel < {0})", security);
                        if (factionID.HasValue)
                            filter.AppendFormat(" AND (factionID IS NULL OR factionID = {0})", factionID);

                        // switch to the new tower
                        currentTower = tower;
                        itemCounts = new Dictionary<int, long>();
                        fuelRows = fuelData.Select(filter.ToString(), "purpose, minSecurityLevel, typeName");

                        // write the big name header
                        writer.WriteStartElement("tr");
                        writer.WriteAttributeString("class", "group");
                        writer.WriteStartElement("th");
                        writer.WriteAttributeString("colspan", "6");
                        writer.WriteRaw(string.Format("<span class=\"l\">{0}</span> / {1}", string.IsNullOrEmpty(location) ? "???" : location, tower));
                        writer.WriteEndElement(); // th
                        writer.WriteEndElement(); // tr
                    }

                    // count items
                    if (itemCounts.ContainsKey(typeID))
                        itemCounts[typeID] += quantity;
                    else
                        itemCounts[typeID] = quantity;
                }

                // write the duration summary for the *last* tower, if any
                if (currentTower != null) WritePosReportGroup(writer, itemCounts, fuelRows);

                // finish doc
                writer.WriteEndDocument();
                writer.Flush();
            }
        }

        private static void WritePosReportGroup(XmlWriter writer, Dictionary<int, long> itemCounts, DataRow[] fuelRows)
        {
            Dictionary<string, TimeSpan> minDurations = new Dictionary<string, TimeSpan>();
            Dictionary<string, decimal> totalCosts = new Dictionary<string, decimal>();

            // the fuel group header
            WriteSubGroupRow(writer, "Fuel", 6);

            // write a row for each type of fuel the POS requires
            foreach (DataRow fuelRow in fuelRows)
            {
                int fuelTypeID = Convert.ToInt32(fuelRow["resourceTypeID"]);
                string fuelName = fuelRow["typeName"].ToString();
                string fuelPurposeText = fuelRow["purposeText"].ToString();
                long fuelQuantity = Convert.ToInt64(fuelRow["quantity"]);
                long quantity = itemCounts.ContainsKey(fuelTypeID) ? itemCounts[fuelTypeID] : 0;
                decimal? fuelMarketPrice = TableHelper.GetNullableValue<decimal>(fuelRow["_marketPriceUnit"]);
                decimal fuelValue = fuelQuantity * Program.GetCompositePrice(EveTypes.Items[fuelTypeID], fuelMarketPrice) * 24;
                TimeSpan duration = TimeSpan.FromHours(quantity / (double)fuelQuantity);

                // keep track of the minimum run-time for each need
                if (!minDurations.ContainsKey(fuelPurposeText) || duration < minDurations[fuelPurposeText])
                    minDurations[fuelPurposeText] = duration;

                // keep track of the total cost of each purpose
                if (!totalCosts.ContainsKey(fuelPurposeText))
                    totalCosts[fuelPurposeText] = fuelValue;
                else
                    totalCosts[fuelPurposeText] += fuelValue;

                // write the row
                writer.WriteStartElement("tr");
                WritePosReportDurationClass(writer, duration);
                WriteElementStringWithClass(writer, "td", "r", FormatInt64(quantity));
                writer.WriteElementString("td", fuelName);
                writer.WriteElementString("td", fuelPurposeText);
                WriteElementRawWithClass(writer, "td", "r", FormatInt32(fuelQuantity) + "<small>/hour</small>");
                WriteElementRawWithClass(writer, "td", "r", FormatIskValue(fuelValue) + "<small>/day</small>");
                WriteElementRawWithClass(writer, "td", "r", FormatTimeSpan(duration));
                writer.WriteEndElement(); // tr
            }

            // the group header
            WriteSubGroupRow(writer, "Run Time Summary", 6);

            // write a row for each purpose as a summary
            foreach (string purpose in minDurations.Keys)
            {
                writer.WriteStartElement("tr");
                WritePosReportDurationClass(writer, minDurations[purpose]);
                writer.WriteElementString("td", "");
                writer.WriteStartElement("td");
                writer.WriteAttributeString("colspan", "3");
                writer.WriteString(purpose);
                writer.WriteEndElement(); // td
                WriteElementRawWithClass(writer, "td", "r", FormatIskValue(totalCosts[purpose]) + "<small>/day</small>");
                WriteElementRawWithClass(writer, "td", "r", FormatTimeSpan(minDurations[purpose]));
                writer.WriteEndElement(); // tr
            }
        }

        private static void WritePosReportDurationClass(XmlWriter writer, TimeSpan duration)
        {
            // make low supplies nice and highlitted
            if (duration.TotalHours < 1)
                writer.WriteAttributeString("class", "low empty");
            else if (duration.TotalDays < 3)
                writer.WriteAttributeString("class", "low");
        }

        #region Private Methods

        private static void PruneTable(DataTable table, string rowFilter)
        {
            DataRow[] badRows = table.Select("NOT (" + rowFilter + ")");
            foreach (DataRow row in badRows)
                table.Rows.Remove(row);
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

        private static void WriteToBody(XmlWriter writer, string title, params string[] extraCss)
        {
            // start the document
            writer.WriteRaw("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.1//EN\" \"http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd\">\n");
            writer.WriteStartElement("html", "http://www.w3.org/1999/xhtml");
            writer.WriteStartElement("head");
            writer.WriteElementString("title", title);
            writer.WriteStartElement("meta");
            writer.WriteAttributeString("http-equiv", "Content-Type");
            writer.WriteAttributeString("content", "text/html;charset=utf-8");
            writer.WriteEndElement(); // meta

            // CSS
            WriteCss(writer, REPORT_CSS);
            foreach (string css in extraCss)
                WriteCss(writer, css);

            // finish the header
            writer.WriteEndElement(); // head

            // start ye body
            writer.WriteStartElement("body");
            writer.WriteElementString("h1", title);
            WriteVersionP(writer);
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

        private static void WriteElementRawWithClass(XmlWriter writer, string localName, string classAttribute, string value)
        {
            writer.WriteStartElement(localName);
            writer.WriteAttributeString("class", classAttribute);
            writer.WriteRaw(value);
            writer.WriteEndElement(); // localName
        }

        private static void WriteSpacerCell(XmlWriter writer)
        {
            WriteElementStringWithClass(writer, "td", "s", " ");
        }

        private static string FormatTimeSpan(TimeSpan value)
        {
            if (value.Days > 0)
                return string.Format("{0}<small>d</small> {1}<small>h</small>", value.Days, value.Hours);
            else
                return string.Format("<span class=\"error\">{0}<small>h<small></span>", value.Hours);
        }

        private static string FormatInt32(object value)
        {
            return Convert.ToInt32(value).ToString("#,##0");
        }

        private static string FormatInt64(object value)
        {
            return Convert.ToInt64(value).ToString("#,##0");
        }

        private static string FormatDecimal1(object value)
        {
            return Convert.ToDecimal(value).ToString("#,##0.0");
        }

        private static string FormatDecimal2(object value)
        {
            return Convert.ToDecimal(value).ToString("#,##0.00");
        }

        private static string FormatDecimal3(object value)
        {
            return Convert.ToDecimal(value).ToString("#,##0.000");
        }

        private static string FormatIskValue(decimal value)
        {
            if (value == 0)
                return QUESTION_HTML;
            else
                return ISK_HTML + FormatDecimal1(value);
        }

        #endregion
    }
}
