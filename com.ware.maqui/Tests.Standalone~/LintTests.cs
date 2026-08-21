// SPDX-License-Identifier: MIT
// MaqUI v2 — xUnit tests for MaquiComponentLint. P5.4.

using System.Linq;
using Maqui.Tools;
using Xunit;

namespace Maqui.Tests
{
    public class MaquiComponentLintTests
    {
        [Fact]
        public void Check_CleanSource_ReturnsEmpty()
        {
            string src = @"
namespace Maqui.Components {
    public static partial class MaquiComponents {
        public static bool Button(this Gui gui, string label) {
            var node = gui.Box();
            gui.DrawText(label);
            return node.OnClick();
        }
    }
}";
            var issues = MaquiComponentLint.Check(src);
            Assert.Empty(issues);
            Assert.True(MaquiComponentLint.IsClean(src));
        }

        [Fact]
        public void Check_ResourcesLoad_Flags()
        {
            string src = "var tex = Resources.Load<Texture>(\"hover\");";
            var issues = MaquiComponentLint.Check(src);
            Assert.Single(issues);
            Assert.Equal("BUG.1-a", issues[0].RuleId);
            Assert.Equal(LintSeverity.Error, issues[0].Severity);
            Assert.Equal(1, issues[0].LineNumber);
        }

        [Fact]
        public void Check_GameObjectInstantiate_Flags()
        {
            string src = "var go = GameObject.Instantiate(prefab);";
            var issues = MaquiComponentLint.Check(src);
            Assert.Single(issues);
            Assert.Equal("BUG.1-b", issues[0].RuleId);
        }

        [Fact]
        public void Check_AddComponentMenuAttribute_Flags()
        {
            string src = "[AddComponentMenu(\"Maqui/Foo\")]";
            var issues = MaquiComponentLint.Check(src);
            Assert.Single(issues);
            Assert.Equal("BUG.1-c", issues[0].RuleId);
        }

        [Fact]
        public void Check_MultipleViolations_AllFlagged()
        {
            string src = @"
Resources.Load<X>(""a"");
GameObject.Instantiate(p);
[AddComponentMenu(""x"")]
";
            var issues = MaquiComponentLint.Check(src);
            Assert.Equal(3, issues.Count);
            Assert.Contains(issues, i => i.RuleId == "BUG.1-a");
            Assert.Contains(issues, i => i.RuleId == "BUG.1-b");
            Assert.Contains(issues, i => i.RuleId == "BUG.1-c");
        }

        [Fact]
        public void Check_IgnoreNextLine_SilencesFollowingLine()
        {
            string src = @"
// maqui-lint:ignore-next-line — testing escape hatch
Resources.Load<X>(""a"");
";
            var issues = MaquiComponentLint.Check(src);
            Assert.Empty(issues);
        }

        [Fact]
        public void Check_IgnoreNextLine_DoesNotSilenceLaterLines()
        {
            string src = @"
// maqui-lint:ignore-next-line
Resources.Load<X>(""a"");
GameObject.Instantiate(p);
";
            var issues = MaquiComponentLint.Check(src);
            Assert.Single(issues);
            Assert.Equal("BUG.1-b", issues[0].RuleId);
        }

        [Fact]
        public void Check_EmptySource_ReturnsEmpty()
        {
            Assert.Empty(MaquiComponentLint.Check(""));
            Assert.Empty(MaquiComponentLint.Check(null));
        }

        [Fact]
        public void Check_LineNumbers_AreOneBased()
        {
            string src = "// line 1\n// line 2\nResources.Load<X>(\"a\");\n// line 4";
            var issues = MaquiComponentLint.Check(src);
            Assert.Single(issues);
            Assert.Equal(3, issues[0].LineNumber);
        }

        [Fact]
        public void IsClean_FalseWhenAnyError()
        {
            string src = "Resources.Load<X>(\"a\");";
            Assert.False(MaquiComponentLint.IsClean(src));
        }

        [Fact]
        public void IsClean_TrueWhenNoIssues()
        {
            string src = "var node = gui.Box();";
            Assert.True(MaquiComponentLint.IsClean(src));
        }

        [Fact]
        public void ToString_IncludesRuleAndLineAndMessage()
        {
            string src = "Resources.Load<X>(\"a\");";
            var issue = MaquiComponentLint.Check(src).First();
            string s = issue.ToString();
            Assert.Contains("BUG.1-a", s);
            Assert.Contains("line 1", s);
        }
    }
}
