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
        // 🟢 Sistemdeki tüm mümkün izinler burada tanımlı — yeni bir yetki eklemek
        // istediğinde sadece bu listeye eklemen yeterli.
        public static readonly List<PermissionDefinition> All = new()
        {
            new() { Key = "ViewData",       Label = "View Data",        Description = "Can view the station repository and analytics.", Group = "Data" },
            new() { Key = "ExportExcel",    Label = "Export Excel",     Description = "Can export station data to Excel.",               Group = "Data" },
            new() { Key = "ManualDataEntry",Label = "Manual Data Entry",Description = "Can add and edit stations manually.",             Group = "Data" },
            new() { Key = "DeleteStations", Label = "Delete Stations",  Description = "Can delete individual or bulk station records.",  Group = "Data" },
            new() { Key = "ExcelUpload",    Label = "Excel Upload",     Description = "Can bulk import stations via spreadsheet.",       Group = "Data" },
            new() { Key = "ViewAnalytics",  Label = "View Analytics",   Description = "Can access the analytics dashboard.",             Group = "Insights" },
            new() { Key = "ViewMaps",       Label = "View Maps",        Description = "Can access the map view.",                        Group = "Insights" },
            new() { Key = "ManageUsers",    Label = "Manage Users",     Description = "Can create, edit, and delete user accounts.",     Group = "Administration" },
            new() { Key = "ManagePermissions", Label = "Manage Permissions", Description = "Can assign or revoke permissions for other users.", Group = "Administration" },
            new() { Key = "SystemConsole",  Label = "System Console",   Description = "Can access system-level maintenance tools.",      Group = "Administration" },
        };

        // 🟢 "Genel"/varsayılan şablon — yeni bir standart kullanıcıya bu izinler verilir.
        // Admin panelinden bir kullanıcıya "Apply Default" dendiğinde bu set uygulanır.
        public static readonly List<string> DefaultUserPermissions = new()
        {
            "ViewData", "ExportExcel", "ViewAnalytics", "ViewMaps"
        };

        public static readonly List<string> DefaultAdminPermissions =
            All.Select(p => p.Key).ToList();
    }
}