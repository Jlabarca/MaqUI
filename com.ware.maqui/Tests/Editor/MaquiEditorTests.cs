using NUnit.Framework;
using UnityEditor;

namespace Maqui.Tests.Editor
{
    public class MaquiEditorTests
    {
        [Test]
        public void PackageJson_DisplayName_IsMaqui()
        {
            // Verifies the package is correctly registered with Unity
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.ware.maqui");
            Assert.IsNotNull(packageInfo, "com.ware.maqui package should be installed.");
            Assert.AreEqual("Maqui", packageInfo.displayName);
        }
    }
}
