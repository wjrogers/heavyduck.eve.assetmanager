using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Text;

namespace HeavyDuck.Eve.AssetManager
{
    public static class EveTypes
    {
        public static Dictionary<int, EveItemCategory> Categories { get; private set; }
        public static Dictionary<int, EveItemGroup> Groups { get; private set; }
        public static Dictionary<int, EveItemType> Items { get; private set; }
        public static Dictionary<int, EveMapRegion> Regions { get; private set; }
        public static Dictionary<int, EveMapSolarSystem> SolarSystems { get; private set; }

        private static bool m_has_prices = false;

        /// <summary>
        /// Initializes the EveTypes cache by reading static game information from the CCP database.
        /// </summary>
        /// <param name="connectionString">The connection string for the CCP database.</param>
        public static void Initialize(string connectionString)
        {
            // create the dictionaries
            Categories = new Dictionary<int, EveItemCategory>(30);
            Groups = new Dictionary<int, EveItemGroup>(800);
            Items = new Dictionary<int, EveItemType>(17000);
            Regions = new Dictionary<int, EveMapRegion>(70);
            SolarSystems = new Dictionary<int, EveMapSolarSystem>(5400);

            using (SQLiteConnection conn = new SQLiteConnection(connectionString))
            {
                // open connection
                conn.Open();

                // load each type into its dictionary
                LoadEveType(conn, "SELECT * FROM invCategories", EveItemCategory.FromRow, Categories);
                LoadEveType(conn, "SELECT * FROM invGroups", EveItemGroup.FromRow, Groups);
                LoadEveType(conn, "SELECT * FROM invTypes", EveItemType.FromRow, Items);
                LoadEveType(conn, "SELECT * FROM mapRegions", EveMapRegion.FromRow, Regions);
                LoadEveType(conn, "SELECT * FROM mapSolarSystems", EveMapSolarSystem.FromRow, SolarSystems);
            }
        }

        private delegate IEveType FromRowDelegate(DataRow row);
        private delegate void LoadEveTypeCallback(IEveType item);

        private static void LoadEveType<T>(SQLiteConnection conn, string sql, FromRowDelegate fromRow, IDictionary<int, T> dictionary) where T : IEveType
        {
            IEveType item;

            using (DataTable table = new DataTable())
            {
                using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(sql, conn))
                    adapter.Fill(table);

                foreach (DataRow row in table.Rows)
                {
                    item = fromRow(row);
                    dictionary[item.ID] = (T)item;
                }
            }
        }

        /// <summary>
        /// Sets market prices for item types.
        /// </summary>
        /// <param name="prices">A dictionary containing typeID and price pairs.</param>
        public static void SetPrices(IDictionary<int, float> prices)
        {
            EveItemType item;

            foreach (KeyValuePair<int, float> price in prices)
            {
                if (Items.TryGetValue(price.Key, out item))
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
    }

    public interface IEveType
    {
        int ID { get; }
    }

    /// <summary>
    /// Represents an EVE item category.
    /// </summary>
    public class EveItemCategory : IEveType
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

        int IEveType.ID { get { return CategoryID; } }
    }

    /// <summary>
    /// Represents an EVE item group.
    /// </summary>
    public class EveItemGroup : IEveType
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

        int IEveType.ID { get { return GroupID; } }
    }

    /// <summary>
    /// Represents an EVE item type.
    /// </summary>
    public class EveItemType : IEveType
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

        int IEveType.ID { get { return TypeID; } }
    }

    /// <summary>
    /// Represents an EVE region.
    /// </summary>
    public class EveMapRegion : IEveType
    {
        public int RegionID { get; private set; }
        public string RegionName { get; private set; }
        public int FactionID { get; private set; }
        public float Radius { get; private set; }

        private EveMapRegion(DataRow row)
        {
            RegionID = Convert.ToInt32(row["regionID"]);
            RegionName = row["regionName"].ToString();
            FactionID = row.IsNull("factionID") ? -1 : Convert.ToInt32(row["factionID"]);
            Radius = Convert.ToSingle(row["radius"]);
        }

        public static EveMapRegion FromRow(DataRow row)
        {
            return new EveMapRegion(row);
        }

        int IEveType.ID { get { return RegionID; } }
    }

    /// <summary>
    /// Represents an EVE solar system.
    /// </summary>
    public class EveMapSolarSystem : IEveType
    {
        public int SolarSystemID { get; private set; }
        public string SolarSystemName { get; private set; }
        public int ConstellationID { get; private set; }
        public int RegionID { get; private set; }
        public float Security { get; private set; }
        public int FactionID { get; private set; }
        public float Radius { get; private set; }
        public string SecurityClass { get; private set; }

        private EveMapSolarSystem(DataRow row)
        {
            SolarSystemID = Convert.ToInt32(row["solarSystemID"]);
            SolarSystemName = Convert.ToString(row["solarSystemName"]);
            ConstellationID = Convert.ToInt32(row["constellationID"]);
            RegionID = Convert.ToInt32(row["regionID"]);
            Security = Convert.ToSingle(row["security"]);
            FactionID = row.IsNull("factionID") ? -1 : Convert.ToInt32(row["factionID"]);
            Radius = Convert.ToSingle(row["radius"]);
            SecurityClass = Convert.ToString(row["securityClass"]);
        }

        public EveMapRegion Region
        {
            get { return EveTypes.Regions[RegionID]; }
        }

        public static EveMapSolarSystem FromRow(DataRow row)
        {
            return new EveMapSolarSystem(row);
        }

        int IEveType.ID { get { return SolarSystemID; } }
    }
}
