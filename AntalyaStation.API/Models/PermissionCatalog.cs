// AntalyaStation.API/Models/PermissionCatalog.cs
namespace AntalyaStation.API.Models
{
    public class PermissionDefinition
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
    }

    public static class PermissionCatalog
    {
        // 🟢 Key'ler artık sol menüdeki (NavMenu.razor) sayfalarla birebir eşleşiyor.
        // "ManagePermissions" bilerek listede YOK: sadece SuperAdmin'e ait, kimseye devredilemez,
        // dolayısıyla UI'de tik olarak da hiç gösterilmemeli.
        public static readonly List<PermissionDefinition> All = new()
        {
            // --- Sol menü: üst seviye linkler ---
            new() { Key = "ViewAnalytics", Label = "Analytics",     Description = "Access to data charts and visualization tools (/analysis)", Group = "Menu" },
            new() { Key = "ViewData",      Label = "View Data",     Description = "Access to central data repository and lists", Group = "Menu" },
            new() { Key = "ViewMaps",      Label = "Maps",          Description = "Access to geographic and mapping tools (/map)", Group = "Menu" },

            // --- Sol menü: Admin Management alt menüsü ---
            new() { Key = "DataImport",    Label = "Data Import",     Description = "Access to data integration and ingestion tools (/admin/import-data)", Group = "Administration" },
            new() { Key = "SystemConsole", Label = "System Console",  Description = "Access to system configuration and control panel (/admin/control-panel)", Group = "Administration" },
            new() { Key = "ManageUsers",   Label = "User Management", Description = "Access to user profiles and permission controls (/admin/user-management)", Group = "Administration" },

            // --- Sayfa içi aksiyon bazlı izinler (route değil, buton/işlem) ---
            new() { Key = "ExportExcel",     Label = "Export Excel",      Description = "Authority to download and export data as spreadsheet", Group = "Actions" },
        };

        public static readonly List<string> DefaultUserPermissions = new()
        {
            "ViewAnalytics", "ViewData", "ViewMaps"
        };

        // Admin varsayılanı: ManagePermissions hariç her şey
        public static readonly List<string> DefaultAdminPermissions =
            All.Select(p => p.Key).ToList();
    }
}