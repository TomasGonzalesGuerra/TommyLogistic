using TommyLogistic.API.Data;
using TommyLogistic.Shared.Entities;
using TommyLogistic.Shared.DTOs.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace TommyLogistic.Api.Helpers;

public class UserHelper(LogisticDataContext datacontext, UserManager<User> userManager, RoleManager<IdentityRole> roleManager, SignInManager<User> signInManager) : IUserHelper
{
    private readonly LogisticDataContext _datacontext = datacontext;
    private readonly UserManager<User> _userManager = userManager;
    private readonly RoleManager<IdentityRole> _roleManager = roleManager;
    private readonly SignInManager<User> _signInManager = signInManager;

    public async Task LogoutAsync() => await _signInManager.SignOutAsync();
    public async Task AddUserToRoleAsync(User user, string roleName) => await _userManager.AddToRoleAsync(user, roleName);
    public async Task<bool> IsUserInRoleAsync(User user, string roleName) => await _userManager.IsInRoleAsync(user, roleName);
    public async Task<SignInResult> LoginAsync(LoginDTO model) => await _signInManager.PasswordSignInAsync(model.Email!, model.Password!, false, false);
    public async Task<IdentityResult> UpdateUserAsync(User user) => await _userManager.UpdateAsync(user);
    public async Task<IdentityResult> AddUserAsync(User user, string password) => await _userManager.CreateAsync(user, password);
    public async Task<IdentityResult> ChangePasswordAsync(User user, string currentPassword, string newPassword) => await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

    public async Task CheckRoleAsync(string roleName)
    {
        bool roleExists = await _roleManager.RoleExistsAsync(roleName);
        if (!roleExists) await _roleManager.CreateAsync(new IdentityRole { Name = roleName });
    }

    public async Task<User> GetUserAsync(string email)
    {
        User? user = await _datacontext.Users.FirstOrDefaultAsync(u => u.Email == email);
        return user!;
    }

    public async Task<User> GetUserAsync(Guid userId)
    {
        User? user = await _datacontext.Users.FirstOrDefaultAsync(u => u.Id == userId.ToString());
        return user!;
    }
}