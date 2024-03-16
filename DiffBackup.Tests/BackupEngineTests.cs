using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DiffBackup.Tests
{
    [TestClass]
    public class BackupEngineTests
    {
        [TestMethod]
        public void TestBackupEngineUniqueSystemMutex()
        {
            bool mutexCreated;
            for (int c = 0; c < 10; c++)
            {
                using (Mutex globalMutex = new Mutex(false, "TestDiffBackupMutex", out mutexCreated))
                {
                    Assert.IsTrue(mutexCreated);
                    globalMutex.Dispose();
                }
            }
        }
    }
}
