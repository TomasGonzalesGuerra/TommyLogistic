using TommyLogistic.API.Data;
using TommyLogistic.Api.Helpers;
using TommyLogistic.Shared.Enums;
using TommyLogistic.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace TommyLogistic.Api.Data;

public class SeedDb(LogisticDataContext datacontext, IUserHelper userHelper)
{
    private readonly IUserHelper _userHelper = userHelper;
    private readonly LogisticDataContext _datacontext = datacontext;

    public async Task SeedAsync()
    {
        await _datacontext.Database.EnsureCreatedAsync();
        await CheckRolesAsync();

        await CheckAdminAsync("El Tommy", "eltommy@yopmail.com", "322 311 420", "naruto.gif", UserEnum.Admin);
        await CheckUserAsync("Brad Pitt", "brad@yopmail.com", "322 311 462", "https://www.famousbirthdays.com/headshots/brad-pitt-9.jpg", UserEnum.Driver, "ABC-123");
        await CheckUserAsync("Bob Marley", "bob@yopmail.com", "322 314 620", "https://upload.wikimedia.org/wikipedia/commons/7/7a/SpongeBob_SquarePants_character.png", UserEnum.Driver, "REG-420");
        await CheckUserAsync("Boruto el Puto", "boruto@yopmail.com", "987 654 231", "https://tierragamer.com/wp-content/uploads/2025/07/El-verdadero-autor-de-boruto-no-es-masahi-kishimoto-2-768x432.webp", UserEnum.Driver, "BOR-666");
        await CheckUserAsync("Ang del Aire", "ang@yopmail.com", "987 456 321", "https://i.pinimg.com/564x/29/b5/4d/29b54db491b9f9499f76268c58afac4e.jpg", UserEnum.Driver, "ANG-777");
        await CheckUserAsync("Toph de Tierra", "toph@yopmail.com", "951 425 687", "https://image.tensorartassets.com/cdn-cgi/image/anim=true,plain=false,w=500,q=85/model_showcase/608513797151344272/d477d208-f808-5a10-453b-d813769bd9df.jpeg", UserEnum.Driver, "TOP-874");
        await CheckUserAsync("Zuko del Fuego", "zuko@yopmail.com", "987 412 365", "https://i.pinimg.com/474x/ab/79/ea/ab79ea0dcbfbfbd5a299f7fa07f31066.jpg", UserEnum.Driver, "ZUK-677");
        await CheckUserAsync("Makima de Tommy", "makima@yopmail.com", "987 654 321", "https://img.anmosugoi.com/file/media-sugoi/2022/10/Makima-Chainsaw-Man-3.jpg", UserEnum.Driver, "MAK-666");
        await CheckUserAsync("Power de Tommy", "powder@yopmail.com", "987 654 322", "https://image.tensorartassets.com/model_showcase/608861109681797221/7d2aad9a-9ce0-9923-ed18-437f869ad135.jpeg", UserEnum.Driver, "POW-666");
        await CheckUserAsync("Mila Azul", "mila@yopmail.com", "382 314 620", "https://globalzonetoday.com/wp-content/uploads/Mila-Azul-Model.jpg", UserEnum.Operator);
        await CheckUserAsync("Sai Ambu", "sai@yopmail.com", "377 314 620", "https://www.nicepng.com/png/full/399-3995389_sai-by-kakashidoe-sai-by-kakashidoe-sai-naruto.png", UserEnum.Supervisor);
        
        User ino = await CheckUserAsync("Ino Sarutobi", "ino@yopmail.com", "928 172 129", "https://www.nicepng.com/png/full/847-8475061_ino-yamanaka-naruto-blazing.png", UserEnum.Client);
        User sarada = await CheckUserAsync("Sarada Uchiha", "sarada@yopmail.com", "963 852 147", "https://i.pinimg.com/236x/66/2d/71/662d717b1134b7a00116727bdcba82be.jpg", UserEnum.Client);
        User naruto = await CheckUserAsync("Naruto Uzumaki", "naruto@yopmail.com", "322 311 460", "https://play-lh.googleusercontent.com/QT1k7Q1JS114SUNJoxoR0admTsC1EUx54hOa7tdYUu_z6MkTIYJ7FEtRXn7XZ-4l3nzj3st9hVxHqT63L0Uktw", UserEnum.Client);
        User angelina = await CheckUserAsync("Angelina Jolie", "angelina@yopmail.com", "322 311 620", "https://th.bing.com/th/id/R.ea08e41477d34ca50ea1d471ae9a24c1?rik=syM1YQV3YAeA8A&riu=http%3a%2f%2fwww.pngall.com%2fwp-content%2fuploads%2f4%2fAngelina-Jolie-PNG-Download-Image-180x180.png", UserEnum.Client);

        await CheckCompaniesAsync(naruto);
        await CheckCompaniesAsync(angelina);
        await CheckCompaniesAsync(ino);
        await CheckCompaniesAsync(sarada);
    }

    private async Task CheckRolesAsync()
    {
        await _userHelper.CheckRoleAsync(UserEnum.Admin.ToString());
        await _userHelper.CheckRoleAsync(UserEnum.Supervisor.ToString());
        await _userHelper.CheckRoleAsync(UserEnum.Operator.ToString());
        await _userHelper.CheckRoleAsync(UserEnum.Driver.ToString());
        await _userHelper.CheckRoleAsync(UserEnum.Client.ToString());
    }

    private async Task<User> CheckAdminAsync(string fullName, string email, string phone, string imageName, UserEnum userType)
    {
        var user = await _userHelper.GetUserAsync(email);

        if (user == null)
        {
            string imagePath = $"https://schoolbook2024.blob.core.windows.net/users/{imageName}";

            user = new User
            {
                FullName = fullName,
                Email = email,
                UserName = email,
                PhoneNumber = phone,
                Address = string.Empty,
                Document = string.Empty,
                Photo = imagePath,
                UserType = userType,
            };

            await _userHelper.AddUserAsync(user, "123456");
            await _userHelper.AddUserToRoleAsync(user, userType.ToString());
        }

        return user;
    }
    
    private async Task<User> CheckUserAsync(string fullName, string email, string phone, string imagePath, UserEnum userType, string placa = null)
    {
        var user = await _userHelper.GetUserAsync(email);
        if (user == null)
        {
            user = new User
            {
                FullName = fullName,
                Email = email,
                UserName = email,
                PhoneNumber = phone,
                Address = "Calle Falsa 123",
                Document = "123456789",
                Photo = imagePath,
                UserType = userType,
            };

            await _userHelper.AddUserAsync(user, "123456");
            await _userHelper.AddUserToRoleAsync(user, userType.ToString());

            if (userType == UserEnum.Driver)
            {
                _datacontext.Drivers.Add(new Driver
                {
                    UserID = user.Id,
                    Placa = placa ?? "SIN-PLACA",
                    Available = true
                });
                await _datacontext.SaveChangesAsync();
            }
        }
        return user;
    }

    private async Task CheckCompaniesAsync(User user)
    {
        if (user == null) return;
        var company = await _datacontext.Companies.FirstOrDefaultAsync(c => c.UserID == user.Id);
        if (company == null)
        {
            _datacontext.Companies.Add(new Company
            {
                Activa = true,
                RegisterDate = DateTime.UtcNow,
                UserID = user.Id
            });
            await _datacontext.SaveChangesAsync();
        }
    }

}
