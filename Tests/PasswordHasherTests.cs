using Xunit;
using BusinessLogic.Logic;
using System;
using System.Linq;

namespace Tests.Logic
{
    public class PasswordHasherTests
    {
        [Theory]
        [InlineData("password")]
        [InlineData("123456")]
        [InlineData("SuperSecurePassword!@#")]
        [InlineData("ñandú")]
        [InlineData("  spaces  ")]
        public void CreateAndVerify_WhenPasswordIsCorrect_ShouldReturnTrue(string password)
        {
            PasswordHasher.CreatePasswordHash(password, out byte[] hash, out byte[] salt);

            bool result = PasswordHasher.VerifyPasswordHash(password, hash, salt);

            Assert.True(result);
        }

        [Theory]
        [InlineData("password", "Password")]
        [InlineData("123456", "1234567")]
        [InlineData("test", "Test")]
        [InlineData("secure", " secure")]
        public void VerifyPasswordHash_WhenPasswordIsIncorrect_ShouldReturnFalse(string original, string wrong)
        {
            PasswordHasher.CreatePasswordHash(original, out byte[] hash, out byte[] salt);

            bool result = PasswordHasher.VerifyPasswordHash(wrong, hash, salt);

            Assert.False(result);
        }

        [Fact]
        public void CreatePasswordHash_ShouldGenerateValidLengths()
        {
            PasswordHasher.CreatePasswordHash("test", out byte[] hash, out byte[] salt);

            Assert.NotNull(hash);
            Assert.NotNull(salt);
            Assert.Equal(64, hash.Length);
            Assert.Equal(128, salt.Length);
        }

        [Fact]
        public void CreatePasswordHash_SamePassword_ShouldGenerateDifferentSalts()
        {
            string password = "TestPassword";

            PasswordHasher.CreatePasswordHash(password, out byte[] hash1, out byte[] salt1);
            PasswordHasher.CreatePasswordHash(password, out byte[] hash2, out byte[] salt2);

            Assert.NotEqual(salt1, salt2);
            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void VerifyPasswordHash_WhenHashIsTampered_ShouldReturnFalse()
        {
            string password = "Test";
            PasswordHasher.CreatePasswordHash(password, out byte[] hash, out byte[] salt);

            hash[0] = (byte)(hash[0] + 1);

            bool result = PasswordHasher.VerifyPasswordHash(password, hash, salt);

            Assert.False(result);
        }

        [Fact]
        public void VerifyPasswordHash_WhenSaltIsTampered_ShouldReturnFalse()
        {
            string password = "Test";
            PasswordHasher.CreatePasswordHash(password, out byte[] hash, out byte[] salt);

            salt[0] = (byte)(salt[0] + 1);

            bool result = PasswordHasher.VerifyPasswordHash(password, hash, salt);

            Assert.False(result);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void VerifyPasswordHash_WhenInputEmpty_ShouldHandleGracefullyOrThrow(string emptyInput)
        {
            if (emptyInput == null)
            {
                PasswordHasher.CreatePasswordHash("valid", out byte[] h, out byte[] s);
                Assert.Throws<ArgumentNullException>(() => PasswordHasher.VerifyPasswordHash(null, h, s));
            }
            else
            {
                PasswordHasher.CreatePasswordHash(emptyInput, out byte[] hash, out byte[] salt);
                Assert.True(PasswordHasher.VerifyPasswordHash(emptyInput, hash, salt));
            }
        }
    }
}