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
        InvalidInput,
        UserNotFound,
        IncorrectPassword,
        AccountBanned,
        AccountLocked
    }
}
