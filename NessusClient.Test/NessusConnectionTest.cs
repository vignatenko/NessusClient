using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NessusClient;
using NUnit.Framework;

namespace VirtualAuditor.NessusClient.Test
{
    [TestFixture]
    public class NessusConnectionTest
    {
        [Test]
        public  void OpenAsync_ShouldLogin()
        {
            var pwd = new SecureString();
            foreach (var c in "1")
            {
                pwd.AppendChar(c);
            }
            var conn = new NessusConnection("w2012r2-dc", 8834, "admin", pwd);
            conn.OpenAsync(new CancellationTokenSource().Token).Wait();
        }
    }
}
