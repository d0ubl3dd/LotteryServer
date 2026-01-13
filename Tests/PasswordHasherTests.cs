using Xunit;
using BusinessLogic.Logic;

namespace Tests.Logic
{
    public class PasswordHasherTests
    {
        [Fact]
        public void CreateAndVerify_WhenPasswordIsCorrect_ShouldReturnTrue()
        {
            string password = "MySecurePassword123!";
            byte[] hash, salt;

            PasswordHasher.CreatePasswordHash(password, out hash, out salt);

            bool result = PasswordHasher.VerifyPasswordHash(password, hash, salt);

            Assert.True(result, "El password correcto debería verificarse exitosamente.");
            Assert.NotNull(hash);
            Assert.NotNull(salt);
            Assert.NotEmpty(hash);
            Assert.NotEmpty(salt);
        }

        [Fact]
        public void Verify_WhenPasswordIsIncorrect_ShouldReturnFalse()
        {
            string correctPassword = "RightPassword";
            string wrongPassword = "WrongPassword";
            byte[] hash, salt;

            PasswordHasher.CreatePasswordHash(correctPassword, out hash, out salt);

            bool result = PasswordHasher.VerifyPasswordHash(wrongPassword, hash, salt);

            Assert.False(result, "Un password incorrecto NO debería verificarse.");
        }

        [Fact]
        public void Create_WhenCalledTwiceWithSamePassword_ShouldGenerateDifferentSalts()
        {
            string password = "SamePassword";
            byte[] hash1, salt1;
            byte[] hash2, salt2;

            PasswordHasher.CreatePasswordHash(password, out hash1, out salt1);
            PasswordHasher.CreatePasswordHash(password, out hash2, out salt2);

            Assert.NotEqual(salt1, salt2);
            Assert.NotEqual(hash1, hash2);
        }
    }
}