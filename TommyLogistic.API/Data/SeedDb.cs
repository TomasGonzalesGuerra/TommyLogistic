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
        await CheckUserAsync("Ino Sarutobi", "ino@tommy.com", "928 172 129", "https://www.nicepng.com/png/full/847-8475061_ino-yamanaka-naruto-blazing.png", UserEnum.Driver, "NIN-777");
        await CheckUserAsync("Mila Azul", "mila@yopmail.com", "382 314 620", "https://wikisbios.com/wp-content/uploads/2022/11/1669490405_657_Mila-Azul-Height-Weight-Bio-Wiki-Age-Photo-Instagram.jpg", UserEnum.Operator);
        await CheckUserAsync("Sai Ambu", "miha@yopmail.com", "377 314 620", "https://www.nicepng.com/png/full/399-3995389_sai-by-kakashidoe-sai-by-kakashidoe-sai-naruto.png", UserEnum.Supervisor);
        
        var naruto = await CheckUserAsync("Naruto Uzumaki", "naruto@yopmail.com", "322 311 460", "https://play-lh.googleusercontent.com/QT1k7Q1JS114SUNJoxoR0admTsC1EUx54hOa7tdYUu_z6MkTIYJ7FEtRXn7XZ-4l3nzj3st9hVxHqT63L0Uktw", UserEnum.Client);
        var angelina = await CheckUserAsync("Angelina Jolie", "angelina@yopmail.com", "322 311 620", "https://th.bing.com/th/id/R.ea08e41477d34ca50ea1d471ae9a24c1?rik=syM1YQV3YAeA8A&riu=http%3a%2f%2fwww.pngall.com%2fwp-content%2fuploads%2f4%2fAngelina-Jolie-PNG-Download-Image-180x180.png", UserEnum.Client);

        await CheckCompaniesAsync(naruto);
        await CheckCompaniesAsync(angelina);

        await CheckOrdersAsync();
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

            // Si es Driver, creamos su entidad Driver
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
                RegisterDate = DateTime.Now,
                UserID = user.Id
            });
            await _datacontext.SaveChangesAsync();
        }
    }

    private async Task CheckOrdersAsync()
    {
        if (await _datacontext.Orders.AnyAsync()) return;

        var companies = await _datacontext.Companies.ToListAsync();
        var drivers = await _datacontext.Drivers.ToListAsync();

        if (!companies.Any() || !drivers.Any()) return;

        var random = new Random();

        // Arrays de datos para aleatoriedad
        string[] nombres = { "Kakashi Hatake", "Sakura Haruno", "Sasuke Uchiha", "Shikamaru Nara", "Jiraiya Sama" };
        string[] direcciones = { "Calle Ninja 123", "Av. Hokage 456", "Barrio Uchiha 789", "Torre Administrativa 10", "Bosque de la Muerte 5" };
        string[] distritos = { "Konoha", "Suna", "Kiri", "Kumo", "Iwa" };
        string[] descripciones = { "Set de Kunais", "Pergaminos de Invocación", "Ramen Instantáneo", "Botiquín Médico", "Capa de Akatsuki" };
        OrderStatus[] estados = { OrderStatus.Registered, OrderStatus.OnTheWay, OrderStatus.Delivered, OrderStatus.PickedUp };

        for (int i = 1; i <= 20; i++)
        {
            var company = companies[random.Next(companies.Count)];
            var driver = drivers[random.Next(drivers.Count)];
            var estado = estados[random.Next(estados.Length)];

            _datacontext.Orders.Add(new Order
            {
                CompanyID = company.Id,
                DriverID = driver.UserID,
                Quantity = random.Next(1, 10),
                RegistrationDate = DateTime.Now.AddDays(-random.Next(1, 30)),
                TrackingCode = $"TL-{1000 + i}",
                RecipientName = nombres[random.Next(nombres.Length)],
                RecipientAddress = direcciones[random.Next(direcciones.Length)],
                RecipientDistrict = distritos[random.Next(distritos.Length)],
                RecipientPhone = $"9{random.Next(10000000, 99999999)}",
                OrderStatus = estado,
                DeliveryType = i % 2 == 0 ? DeliveryType.ToDay : DeliveryType.ToMorrow,
                PackageDescription = descripciones[random.Next(descripciones.Length)],
                Invoiced = random.Next(0, 2) == 1,
                DeliveryAttempts = estado == OrderStatus.Delivered ? 1 : 0
            });
        }

        await _datacontext.SaveChangesAsync();
    }
}

//    public async Task SeedAsync()
//    {
//        await _datacontext.Database.EnsureCreatedAsync();
//        await CheckRolesAsync();
//        await CheckAdminAsync("El Tommy", "eltommy@yopmail.com", "322 311 420", "naruto.gif", UserEnum.Admin);
//        await CheckUserAsync("Naruto", "naruto@yopmail.com", "322 311 460", "https://tse2.mm.bing.net/th/id/OIP.qmzNgDc5Qif3CKQhHPz0CwHaJe?w=1600&h=2048&rs=1&pid=ImgDetMain&o=7&rm=3", UserEnum.Client);
//        await CheckUserAsync("Brad Pitt", "brad@yopmail.com", "322 311 462", "https://www.famousbirthdays.com/headshots/brad-pitt-9.jpg", UserEnum.Driver);
//        await CheckUserAsync("Angelina Jolie", "angelina@yopmail.com", "322 311 620", "https://th.bing.com/th/id/R.ea08e41477d34ca50ea1d471ae9a24c1?rik=syM1YQV3YAeA8A&riu=http%3a%2f%2fwww.pngall.com%2fwp-content%2fuploads%2f4%2fAngelina-Jolie-PNG-Download-Image-180x180.png&ehk=9PpQoKwZZnToSx13c8BKIY7KF8ZVFGienK24OKAGXTk%3d&risl=&pid=ImgRaw&r=0", UserEnum.Client);
//        await CheckUserAsync("Bob Marley", "bob@yopmail.com", "322 314 620", "https://th.bing.com/th/id/R.0775505a15a8846b6aa0930ab5e0d8dd?rik=wC1YaVdSPWOamQ&riu=http%3a%2f%2fwww.myfirstrecord.co.uk%2frecordpress%2fwp-content%2fuploads%2f2011%2f05%2fBob-Marley1-150x150.jpg&ehk=nOBcCpQOgc8iSniug5PCieOqW5fNq3ja%2faS%2bCWw9xxE%3d&risl=&pid=ImgRaw&r=0", UserEnum.Driver);
//        await CheckUserAsync("Mila Azul", "mila@yopmail.com", "382 314 620", "https://wikisbios.com/wp-content/uploads/2022/11/1669490405_657_Mila-Azul-Height-Weight-Bio-Wiki-Age-Photo-Instagram.jpg", UserEnum.Operator);
//        await CheckUserAsync("Sai Ambu", "miha@yopmail.com", "377 314 620", "https://tse4.mm.bing.net/th/id/OIP.ZltgcqHOJxCsj2Pf9IhKqQAAAA?rs=1&pid=ImgDetMain&o=7&rm=3", UserEnum.Supervisor);
//        await CheckUserAsync("Hynata Hyuga", "hyna@tommy.com", "928 172 126", "https://pt.quizur.com/_image?href=https://img.quizur.com/f/img6149da08ee4b74.87549065.jpg?lastEdited=1632229911&w=600&h=600&f=webp", UserEnum.Operator);
//        await CheckUserAsync("Ino Sarutobi", "ino@tommy.com", "928 172 129", "https://th.bing.com/th/id/R.cbca06d335b58ddea8eafd6f1207f994?rik=XIcp71ShwjKQ3g&riu=http%3a%2f%2ficons.iconseeker.com%2fpng%2f128%2fnaruto-vol-2%2fyamanaka-ino.png&ehk=m2lPEUWXtlWr9W%2fne%2bmOtCfLTzCrULFN5%2bNL%2b%2fPVciI%3d&risl=&pid=ImgRaw&r=0", UserEnum.Driver);
//    }
