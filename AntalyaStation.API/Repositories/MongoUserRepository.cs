using AntalyaStation.API.Data;
using AntalyaStation.API.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace AntalyaStation.API.Repositories;

public class MongoUserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _users;

    public MongoUserRepository(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _users = database.GetCollection<User>("Users");
    }

    public async Task<User?> GetByUsernameAsync(string username)
        => await _users.Find(u => u.Username == username).FirstOrDefaultAsync();

    public async Task<User?> GetByIdAsync(string id)
        => await _users.Find(u => u.Id == id).FirstOrDefaultAsync();

    public async Task AddAsync(User user)
        => await _users.InsertOneAsync(user);

    public async Task<bool> UpdateProfileAsync(string id, string username, string fullName, string email, string phoneNumber)
    {
        var update = Builders<User>.Update
            .Set(u => u.Username, username)
            .Set(u => u.FullName, fullName)
            .Set(u => u.Email, email)
            .Set(u => u.PhoneNumber, phoneNumber);

        var result = await _users.UpdateOneAsync(u => u.Id == id, update);
        return result.ModifiedCount > 0 || result.MatchedCount > 0;
    }

    public async Task<bool> UpdatePasswordAsync(string id, string passwordHash, string passwordSalt)
    {
        var update = Builders<User>.Update
            .Set(u => u.PasswordHash, passwordHash)
            .Set(u => u.PasswordSalt, passwordSalt)
            .Set(u => u.LastPasswordChangeDate, DateTime.UtcNow);

        var result = await _users.UpdateOneAsync(u => u.Id == id, update);
        return result.ModifiedCount > 0;
    }

    public async Task<List<User>> GetAllAsync()
        => await _users.Find(Builders<User>.Filter.Empty).SortBy(u => u.Username).ToListAsync();

    public async Task<bool> UpdateRoleAsync(string id, string role)
    {
        var update = Builders<User>.Update.Set(u => u.Role, role);
        var result = await _users.UpdateOneAsync(u => u.Id == id, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _users.DeleteOneAsync(u => u.Id == id);
        return result.DeletedCount > 0;
    }

    public async Task<bool> IsUsernameTakenAsync(string username, string? excludeUserId = null)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Username, username);
        if (!string.IsNullOrEmpty(excludeUserId))
            filter &= Builders<User>.Filter.Ne(u => u.Id, excludeUserId);

        var count = await _users.CountDocumentsAsync(filter);
        return count > 0;
    }
    public async Task<bool> UpdatePermissionsAsync(string id, List<string> permissions)
    {
        var update = Builders<User>.Update.Set(u => u.Permissions, permissions);
        var result = await _users.UpdateOneAsync(u => u.Id == id, update);
        return result.ModifiedCount > 0;
    }public async Task<long> CountAllAsync()
    {
        return await _users.CountDocumentsAsync(_ => true);
    }

    public async Task<long> CountByRoleAsync(string role)
    {
        return await _users.CountDocumentsAsync(u => u.Role == role);
    }
}