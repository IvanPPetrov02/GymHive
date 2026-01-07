using BLL.DTOs;
using BLL.Encryption;
using BLL.Entities;
using BLL.ManagerInterfaces;
using BLL.RepositoryInterfaces;
using BLL.Services;

namespace BLL
{
    public class UserManager : IUserManager
    {
        private readonly IUserDAO _userDao;
        private readonly IJwtService _jwtService;

        public UserManager(IUserDAO userDao, IJwtService jwtService)
        {
            _userDao = userDao;
            _jwtService = jwtService;
        }

        public async Task<string> RegisterUserAsync(UserRegisterDTO userDto)
        {
            var existingUser = await _userDao.GetUserByEmailAsync(userDto.Email);
            if (existingUser != null)
            {
                return "User already exists";
            }

            var newUser = new User
            {
                Email = userDto.Email,
                Password = PassHash.HashPassword(userDto.Password),
                Name = userDto.Name,
                Surname = userDto.Surname
            };

            await _userDao.CreateUserAsync(newUser);
            return "User created";
        }

        public async Task<string?> AuthenticateUserAsync(string email, string password)
        {
            var user = await _userDao.GetUserByEmailAsync(email);
            if (user == null || !PassHash.ValidatePassword(password, user.Password ?? string.Empty))
            {
                return null;
            }

            return _jwtService.GenerateJwtToken(user);
        }

        public async Task UpdateUserDetailsAsync(string uuid, UserUpdateDTO userDto)
        {
            var user = await _userDao.GetUserByIdAsync(uuid);
            if (user == null)
                throw new InvalidOperationException("User not found.");

            if (userDto.Email != null && userDto.Email != user.Email)
                user.Email = userDto.Email;

            if (userDto.Name != null && userDto.Name != user.Name)
                user.Name = userDto.Name;

            if (userDto.Surname != null && userDto.Surname != user.Surname)
                user.Surname = userDto.Surname;

            await _userDao.UpdateUserAsync(user);
        }

        public async Task DeleteUserAsync(string uuid)
        {
            try
            {
                var user = await _userDao.GetUserByIdAsync(uuid);
                if (user == null)
                {
                    throw new InvalidOperationException("User not found.");
                }

                await _userDao.DeleteUserAsync(uuid);
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<User?> GetUserByIdAsync(string uuid)
        {
            var user = await _userDao.GetUserByIdAsync(uuid);
            return user;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _userDao.GetAllUsersAsync();
        }

        public async Task ActivateOrDeactivateUserAsync(string uuid, bool isActive)
        {
            var user = await _userDao.GetUserByIdAsync(uuid);
            if (user != null)
            {
                user.IsActive = isActive;
                await _userDao.UpdateUserAsync(user);
            }
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _userDao.GetUserByEmailAsync(email);
        }

        public async Task ChangePasswordAsync(string uuid, string newPassword, string oldPassword)
        {
            var user = await _userDao.GetUserByIdAsync(uuid);
            if (user == null)
            {
                throw new InvalidOperationException("User not found.");
            }

            if (!PassHash.ValidatePassword(oldPassword, user.Password ?? string.Empty))
            {
                throw new UnauthorizedAccessException("Old password is incorrect.");
            }

            user.Password = PassHash.HashPassword(newPassword);
            await _userDao.UpdateUserAsync(user);
        }

        public async Task<User?> GetLoggedUserAsync(string userId)
        {
            return await GetUserByIdAsync(userId);
        }

        async Task<User?> IUserManager.GetUserByIdAsync(string uuid)
        {
            return await _userDao.GetUserByIdAsync(uuid);
        }

        async Task<IEnumerable<User>> IUserManager.GetAllUsersAsync()
        {
            return await _userDao.GetAllUsersAsync();
        }

        async Task<User?> IUserManager.GetUserByEmailAsync(string email)
        {
            return await _userDao.GetUserByEmailAsync(email);
        }

        async Task<User?> IUserManager.GetLoggedUserAsync(string userId)
        {
            return await _userDao.GetUserByIdAsync(userId);
        }

        public async Task UpdateUserRoleAsync(string uuid, Role role)
        {
            var user = await _userDao.GetUserByIdAsync(uuid);
            if (user == null)
            {
                throw new InvalidOperationException("User not found.");
            }

            user.Role = role;
            await _userDao.UpdateUserAsync(user);
        }

        public async Task UpdateUserGymIdAsync(string uuid, int? gymId)
        {
            var user = await _userDao.GetUserByIdAsync(uuid);
            if (user == null)
            {
                throw new InvalidOperationException("User not found.");
            }

            user.GymId = gymId;
            await _userDao.UpdateUserAsync(user);
        }
    }
}
