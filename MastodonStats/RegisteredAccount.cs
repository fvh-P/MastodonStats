using Mastonet;
using Mastonet.Entities;
using Newtonsoft.Json;

namespace MastodonStats
{
    [JsonObject("account")]
    class RegisteredAccount
    {
        [JsonProperty("acc")]
        public Account Account { get; set; }
        [JsonProperty("count")]
        public long TootsToday { get; set; }
        public RegisteredAccount(Account account)
        {
            Account = account;
            TootsToday = 1;
        }
        public long AddStatus(Status s)
        {
            return ++TootsToday;
        }
    }
}
