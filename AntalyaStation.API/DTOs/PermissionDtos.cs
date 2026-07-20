namespace AntalyaStation.API.DTOs
{
    public class PermissionDefinitionDto
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
    }

    public class PermissionCatalogResponseDto
    {
        public List<PermissionDefinitionDto> AllPermissions { get; set; } = new();
        public List<string> DefaultUserPermissions { get; set; } = new();
        public List<string> DefaultAdminPermissions { get; set; } = new();
    }

    public class UserPermissionsDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new();
    }

    public class UpdateUserPermissionsDto
    {
        public List<string> Permissions { get; set; } = new();
    }
}