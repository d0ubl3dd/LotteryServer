using BusinessLogic.Logic;

namespace BusinessLogic
{
    public static class LotteryServerContext
    {
        public static ILobbyManager LobbyManager { get; set; }
        public static ISessionManager SessionManager { get; set; }
    }
}