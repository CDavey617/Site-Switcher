using NINA.Core.Utility;
using NUnit.Framework;
using System.IO;

namespace CraigDavey.Nina.Sites.Tests {
    [SetUpFixture]
    public class TestEnvironment {
        private string previousRoot;

        [OneTimeSetUp]
        public void Start() {
            previousRoot = CoreUtil.APPLICATIONTEMPPATH;
            var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "runtime", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            CoreUtil.APPLICATIONTEMPPATH = root;
        }

        [OneTimeTearDown]
        public void Stop() {
            CoreUtil.APPLICATIONTEMPPATH = previousRoot;
        }
    }
}
