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

    public async Task<bool> UpdateProfileAsync(string id, string fullName, string email)
    {
        var update = Builders<User>.Update
            .Set(u => u.FullName, fullName)
            .Set(u => u.Email, email);

        var result = await _users.UpdateOneAsync(u => u.Id == id, update);
        return result.ModifiedCount > 0;
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
}