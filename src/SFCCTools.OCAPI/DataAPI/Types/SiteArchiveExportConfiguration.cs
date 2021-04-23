using System.Collections.Generic;
using Newtonsoft.Json;

namespace SFCCTools.OCAPI.DataAPI.Types
{
    public class SiteArchiveExportConfiguration
    {
        public readonly ExportDataUnitsConfiguration DataUnits = new ExportDataUnitsConfiguration();
        public string ExportFile;
        public bool OverwriteExportFile;
    }

    public class ExportDataUnitsConfiguration
    {
        public readonly ExportGlobalDataConfiguration GlobalData = new ExportGlobalDataConfiguration();
        public Dictionary<string, bool> CatalogStaticResources;
        public Dictionary<string, bool> Catalogs;
        public Dictionary<string, bool> CustomerLists;
        public Dictionary<string, bool> InventoryLists;
        public Dictionary<string, bool> Libraries;
        public Dictionary<string, bool> LibraryStaticResources;
        public Dictionary<string, bool> PriceBooks;
        public Dictionary<string, ExportSitesConfiguration> Sites;
    }

    public class ExportSitesConfiguration
    {
        public bool AbTests;
        public bool ActiveDataFeeds;
        public bool All;
        public bool CacheSettings;
        public bool CampaignsAndPromotions;
        public bool Content;
        public bool Coupons;
        public bool CustomObjects;
        public bool CustomerCdnSettings;
        public bool CustomerGroups;
        public bool DistributedCommerceExtensions;
        public bool DynamicFileResources;
        public bool GiftCertificates;
        public bool OcapiSettings;
        public bool PaymentMethods;
        public bool PaymentProcessors;
        public bool RedirectUrls;
        public bool SearchSettings;
        public bool Shipping;
        public bool SiteDescriptor;
        public bool SitePreferences;
        public bool SitemapSettings;
        public bool Slots;
        public bool SortingRules;
        public bool SourceCodes;
        public bool StaticDynamicAliasMappings;
        public bool Stores;
        public bool Tax;
        public bool UrlRules;
    }

    public class ExportGlobalDataConfiguration
    {
        public bool AccessRoles;
        public bool CscSettings;
        public bool CsrfWhitelists;
        public bool CustomPreferenceGroups;
        public bool CustomQuotaSettings;
        public bool CustomTypes;
        public bool Geolocations;
        public bool GlobalCustomObjects;
        public bool JobSchedules;
        public bool JobSchedulesDeprecated;
        public bool Locales;
        public bool MetaData;
        public bool OauthProviders;
        public bool OcapiSettings;
        public bool PageMetaTags;
        public bool Preferences;
        public bool PriceAdjustmentLimits;
        public bool Services;
        public bool SortingRules;
        public bool StaticResources;
        public bool SystemTypeDefinitions;
        public bool Users;
        public bool WebdavClientPermissions;
    }
}