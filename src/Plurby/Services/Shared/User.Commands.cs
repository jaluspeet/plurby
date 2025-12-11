using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Plurby.Services.Shared
{
    public class AddOrUpdateUserCommand
    {
        public Guid? Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string NickName { get; set; }
        public UserRole? Role { get; set; }
        public string Password { get; set; }
    }

    public class DeleteUserCommand
    {
        public Guid Id { get; set; }
    }

    public partial class SharedService
    {
        public async Task<Guid> Handle(AddOrUpdateUserCommand cmd)
        {
            var user = await _dbContext.Users
                .Where(x => x.Id == cmd.Id)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                user = new User
                {
                    Email = cmd.Email,
                };
                _dbContext.Users.Add(user);
            }

            user.Email = cmd.Email;
            user.FirstName = cmd.FirstName;
            user.LastName = cmd.LastName;
            user.NickName = cmd.NickName;
            if (cmd.Role.HasValue)
            {
                user.Role = cmd.Role.Value;
            }

            // Only update password if provided
            if (!string.IsNullOrEmpty(cmd.Password))
            {
                var sha256 = SHA256.Create();
                user.Password = Convert.ToBase64String(sha256.ComputeHash(Encoding.ASCII.GetBytes(cmd.Password)));
            }

            await _dbContext.SaveChangesAsync();

            return user.Id;
        }

        public async Task Handle(DeleteUserCommand cmd)
        {
            var user = await _dbContext.Users
                .Where(x => x.Id == cmd.Id)
                .FirstOrDefaultAsync();

            if (user != null)
            {
                _dbContext.Users.Remove(user);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}