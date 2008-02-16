using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Text;

namespace HeavyDuck.Eve.AssetManager
{
    public static class EveTypes
    {
        private static Dictionary<long, EveItemCategory> m_categories;
        private static Dictionary<long, EveItemGroup> m_groups;
        private static Dictionary<long, EveItemType> m_items;
        private static bool m_has_prices = false;

        public static void Initialize()
        {
            SQLiteConnection conn = null;
            SQLiteCommand cmd = null;
            SQLiteDataAdapter adapter = null;
            DataTable table;

            // create the dictionaries
            m_categories = new Dictionary<long, EveItemCategory>(30);
            m_groups = new Dictionary<long, EveItemGroup>(800);
            m_items = new Dictionary<long, EveItemType>(17000);

            try
            {
                // open connection
                conn = new SQLiteConnection("Data Source=" + Program.CcpDatabasePath);
                conn.Open();
                cmd = conn.CreateCommand();
                adapter = new SQLiteDataAdapter(cmd);

                // categories
                cmd.CommandText = "SELECT * FROM invCategories";
                table = new DataTable("Categories");
                adapter.Fill(table);
                foreach (DataRow row in table.Rows)
                {
                    EveItemCategory category = EveItemCategory.FromRow(row);
                    m_categories[category.CategoryID] = category;
                }
                table.Dispose();

                // groups
                cmd.CommandText = "SELECT * FROM invGroups";
                table = new DataTable("Groups");
                adapter.Fill(table);
                foreach (DataRow row in table.Rows)
                {
                    EveItemGroup group = EveItemGroup.FromRow(row);
                    m_groups[group.GroupID] = group;
                }
                table.Dispose();

                // items
                cmd.CommandText = "SELECT * FROM invTypes";
                table = new DataTable("Types");
                adapter.Fill(table);
                foreach (DataRow row in table.Rows)
                {
                    EveItemType type = EveItemType.FromRow(row);
                    m_items[type.TypeID] = type;
                }
                table.Dispose();
            }
            finally
            {
                if (adapter != null) adapter.Dispose();
                if (cmd != null) cmd.Dispose();
                if (conn != null) conn.Dispose();
            }
        }

        /// <summary>
        /// Sets market prices for item types.
        /// </summary>
        /// <param name="prices">A dictionary containing typeID and price pairs.</param>
        public static void SetPrices(IDictionary<long, float> prices)
        {
            EveItemType item;

            foreach (KeyValuePair<long, float> price in prices)
            {
                if (m_items.TryGetValue(price.Key, out item))
                    item.MarketPrice = price.Value;
            }

            m_has_prices = true;
        }

        /// <summary>
        /// Gets a value that indicates whether any market prices have been loaded.
        /// </summary>
        public static bool HasPrices
        {
            get { return m_has_prices; }
        }

        public static Dictionary<long, EveItemCategory> Categories
        {
            get { return m_categories; }
        }

        public static Dictionary<long, EveItemGroup> Groups
        {
            get { return m_groups; }
        }

        public static Dictionary<long, EveItemType> Items
        {
            get { return m_items; }
        }
    }

    /// <summary>
    /// Represents an EVE item category.
    /// </summary>
    public class EveItemCategory
    {
        public int CategoryID { get; private set; }
        public string CategoryName { get; private set; }
        public string Description { get; private set; }
        public bool Published { get; private set; }

        private EveItemCategory(DataRow row)
        {
            CategoryID = Convert.ToInt32(row["categoryID"]);
            CategoryName = row["categoryName"].ToString();
            Description = row["description"].ToString();
            Published = Convert.ToBoolean(row["published"]);
        }

        public static EveItemCategory FromRow(DataRow row)
        {
            return new EveItemCategory(row);
        }
    }

    /// <summary>
    /// Represents an EVE item group.
    /// </summary>
    public class EveItemGroup
    {
        public int GroupID { get; private set; }
        public int CategoryID { get; private set; }
        public string GroupName { get; private set; }
        public string Description { get; private set; }
        public bool UseBasePrice { get; private set; }
        public bool AllowManufacture { get; private set; }
        public bool AllowRecycler { get; private set; }
        public bool Anchored { get; private set; }
        public bool Anchorable { get; private set; }
        public bool FittableNonSingleton { get; private set; }
        public bool Published { get; private set; }

        private EveItemGroup(DataRow row)
        {
            GroupID = Convert.ToInt32(row["groupID"]);
            CategoryID = Convert.ToInt32(row["categoryID"]);
            GroupName = row["groupName"].ToString();
            Description = row["description"].ToString();
            UseBasePrice = Convert.ToBoolean(row["useBasePrice"]);
            AllowManufacture = Convert.ToBoolean(row["allowManufacture"]);
            AllowRecycler = Convert.ToBoolean(row["allowRecycler"]);
            Anchored = Convert.ToBoolean(row["anchored"]);
            Anchorable = Convert.ToBoolean(row["anchorable"]);
            FittableNonSingleton = Convert.ToBoolean(row["fittableNonSingleton"]);
            Published = Convert.ToBoolean(row["published"]);
        }

        public EveItemCategory Category
        {
            get { return EveTypes.Categories[CategoryID]; }
        }

        public static EveItemGroup FromRow(DataRow row)
        {
            return new EveItemGroup(row);
        }
    }

    /// <summary>
    /// Represents an EVE item type.
    /// </summary>
    public class EveItemType
    {
        public int TypeID { get; private set; }
        public int GroupID { get; private set; }
        public string TypeName { get; private set; }
        public string Description { get; private set; }
        public float Radius { get; private set; }
        public float Mass { get; private set; }
        public float Volume { get; private set; }
        public float Capacity { get; private set; }
        public int PortionSize { get; private set; }
        public int RaceID { get; private set; }
        public float BasePrice { get; private set; }
        public int MarketGroupID { get; private set; }
        public bool Published { get; private set; }

        private float? m_marketPrice = null;

        private EveItemType(DataRow row)
        {
            TypeID = Convert.ToInt32(row["typeID"]);
            GroupID = Convert.ToInt32(row["groupID"]);
            TypeName = row["typeName"].ToString();
            Description = row["description"].ToString();
            Radius = Convert.ToSingle(row["radius"]);
            Mass = Convert.ToSingle(row["mass"]);
            Volume = Convert.ToSingle(row["volume"]);
            Capacity = Convert.ToSingle(row["capacity"]);
            PortionSize = Convert.ToInt32(row["portionSize"]);
            RaceID = row.IsNull("raceID") ? -1 : Convert.ToInt32(row["raceID"]);
            BasePrice = Convert.ToSingle(row["basePrice"]);
            MarketGroupID = row.IsNull("marketGroupID") ? -1 : Convert.ToInt32(row["marketGroupID"]);
            Published = Convert.ToBoolean(row["published"]);
        }

        public EveItemGroup Group
        {
            get { return EveTypes.Groups[GroupID]; }
        }

        public EveItemCategory Category
        {
            get { return Group.Category; }
        }

        /// <summary>
        /// Gets the item's price, as determined by market data and current user options.
        /// </summary>
        public float CompositePrice
        {
            get
            {
                if (Category.CategoryName == "Blueprint" && Program.OptionsDialog["Pricing.ZeroBlueprints"].ValueAsBoolean)
                    return 0f;
                else if (m_marketPrice.HasValue)
                    return m_marketPrice.Value;
                else if (Group.UseBasePrice && Program.OptionsDialog["Pricing.UseBasePrice"].ValueAsBoolean)
                    return BasePrice / PortionSize;
                else
                    return 0f;
            }
        }

        /// <summary>
        /// Gets or sets the market value of this item.
        /// </summary>
        public float? MarketPrice
        {
            get { return m_marketPrice; }
            set { m_marketPrice = value; }
        }

        public static EveItemType FromRow(DataRow row)
        {
            return new EveItemType(row);
        }
    }
}
