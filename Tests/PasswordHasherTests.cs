using Xunit;
using BusinessLogic.Logic;
using System.Text;

namespace Tests.Logic
{
    public class PasswordHasherTests
    {
        [Fact]
        public void CreateAndVerify_WhenPasswordIsCorrect_ShouldReturnTrue()
        {
            // Arrange
            string password = "MySecurePassword123!";
            byte[] hash, salt;

            // Act - Crear
            PasswordHasher.CreatePasswordHash(password, out hash, out salt);

            // Act - Verificar
            bool result = PasswordHasher.VerifyPasswordHash(password, hash, salt);

            // Assert
            Assert.True(result, "El password correcto debería verificarse exitosamente.");
            Assert.NotNull(hash);
            Assert.NotNull(salt);
            Assert.NotEmpty(hash);
            Assert.NotEmpty(salt);
        }

        [Fact]
        public void Verify_WhenPasswordIsIncorrect_ShouldReturnFalse()
        {
            // Arrange
            string correctPassword = "RightPassword";
            string wrongPassword = "WrongPassword";
            byte[] hash, salt;

            PasswordHasher.CreatePasswordHash(correctPassword, out hash, out salt);

            // Act
            bool result = PasswordHasher.VerifyPasswordHash(wrongPassword, hash, salt);

            // Assert
            Assert.False(result, "Un password incorrecto NO debería verificarse.");
        }

        [Fact]
        public void Create_WhenCalledTwiceWithSamePassword_ShouldGenerateDifferentSalts()
        {
            // Arrange
            string password = "SamePassword";
            byte[] hash1, salt1;
            byte[] hash2, salt2;

            // Act
            PasswordHasher.CreatePasswordHash(password, out hash1, out salt1);
            PasswordHasher.CreatePasswordHash(password, out hash2, out salt2);

            // Assert
            Assert.NotEqual(salt1, salt2); // Las sales deben ser aleatorias y únicas
            Assert.NotEqual(hash1, hash2); // Por ende, los hashes finales también deben ser distintos
        }
    }
}