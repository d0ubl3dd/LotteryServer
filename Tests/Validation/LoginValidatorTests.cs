using Xunit;
using BusinessLogic.Validation;
using DataAccess; // Para User
using Tests.Builders; // Usamos tu Builder existente

namespace Tests.Validation
{
    public class LoginValidatorTests
    {
        // ==========================================
        // PRUEBAS: ValidateLoginAttempt
        // ==========================================

        [Fact]
        public void Validate_WhenUsernameIsEmpty_ShouldReturnEmptyUserName()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Username vacío.
             * ✔ Salida Esperada: Enum EmptyUserName.
             */
            var result = LoginValidator.ValidateLoginAttempt("", "pass", new User());
            Assert.Equal(LoginValidationResult.EmptyUserName, result);
        }

        [Fact]
        public void Validate_WhenPasswordIsEmpty_ShouldReturnEmptyPassword()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Password vacío.
             * ✔ Salida Esperada: Enum EmptyPassword.
             */
            var result = LoginValidator.ValidateLoginAttempt("User", "", new User());
            Assert.Equal(LoginValidationResult.EmptyPassword, result);
        }

        [Fact]
        public void Validate_WhenNicknameTooShort_ShouldReturnNicknameTooShort()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: "Bob" (3 letras, mínimo es 4).
             * ✔ Salida Esperada: Enum NicknameTooShort.
             */
            var result = LoginValidator.ValidateLoginAttempt("Bob", "pass", new User());
            Assert.Equal(LoginValidationResult.NicknameTooShort, result);
        }

        [Fact]
        public void Validate_WhenUserIsNull_ShouldReturnUserNotFound()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Objeto User encontrado es null.
             * ✔ Salida Esperada: Enum UserNotFound.
             */
            var result = LoginValidator.ValidateLoginAttempt("ValidUser", "pass", null);
            Assert.Equal(LoginValidationResult.UserNotFound, result);
        }

        [Fact]
        public void Validate_WhenUserIsLocked_ShouldReturnAccountLocked()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Usuario con isLocked = true.
             * ✔ Salida Esperada: Enum AccountLocked.
             */
            var lockedUser = new UserBuilder().Locked().Build();
            var result = LoginValidator.ValidateLoginAttempt("ValidUser", "pass", lockedUser);

            Assert.Equal(LoginValidationResult.AccountLocked, result);
        }

        [Fact]
        public void Validate_WhenPasswordIncorrect_ShouldReturnIncorrectPassword()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Password "Wrong" vs Hash de "Right".
             * ✔ Salida Esperada: Enum IncorrectPassword.
             */
            // El builder crea el usuario con password "PasswordSeguro123" por defecto
            var user = new UserBuilder().WithPassword("PasswordSeguro123").Build();

            var result = LoginValidator.ValidateLoginAttempt("ValidUser", "WrongPass", user);

            Assert.Equal(LoginValidationResult.IncorrectPassword, result);
        }

        [Fact]
        public void Validate_WhenCredentialsCorrect_ShouldReturnSuccess()
        {
            /* DOCUMENTACIÓN
             * ✔ Entrada: Password coincide con el Hash.
             * ✔ Salida Esperada: Enum Success.
             */
            string pass = "MySecretPass";
            var user = new UserBuilder().WithPassword(pass).Build();

            var result = LoginValidator.ValidateLoginAttempt("ValidUser", pass, user);

            Assert.Equal(LoginValidationResult.Success, result);
        }
    }
}