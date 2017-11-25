using Mastonet.Entities;
using System.Collections.Generic;
using System.Linq;

namespace MastodonStats
{
    class RegisteredAccountList : List<RegisteredAccount>
    {
        public RegisteredAccountList() : base() { }
        public RegisteredAccountList(int capacity): base(capacity) { }
        public void Update(Status status)
        {
            if (Find(x => x.Account.AccountName == status.Account.AccountName)?.AddStatus(status) == null)
            {
                Add(new RegisteredAccount(status.Account));
            }
        }
    }
}
