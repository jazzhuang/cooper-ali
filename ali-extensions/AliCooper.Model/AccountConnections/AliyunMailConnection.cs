using Cooper.Model.Accounts;

namespace AliCooper.Model.Accounts
{
    /// <summary>阿里云邮箱帐号连接
    /// </summary>
    public class AliyunMailConnection : AccountConnection
    {
        protected AliyunMailConnection() : base() { }//由于NH
        public AliyunMailConnection(string name, string token, Account account) : base(name, token, account) { }
    }
}