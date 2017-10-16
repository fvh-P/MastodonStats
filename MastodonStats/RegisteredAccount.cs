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
        public int TootsToday { get; set; }
        public RegisteredAccount(Account account)
        {
            Account = account;
            TootsToday = 0;
        }
        public void AddStatus(Status s)
        {
            TootsToday++;
        }
    }
}
