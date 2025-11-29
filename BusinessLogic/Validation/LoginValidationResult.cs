using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Validation
{
    public enum LoginValidationResult
    {
        Success,
        EmptyUserName,
        EmptyPassword,
        NicknameTooShort,
        UserNotFound,
        IncorrectPassword,
        AccountLocked
    }
}
