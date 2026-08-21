// SPDX-License-Identifier: MIT
// MaqUI v2 — MaquiComponentLint. Static string-scanner rejecting BUG.1
// regressions in LLM-authored component source.

using System;
using System.Collections.Generic;

namespace Maqui.Tools
{
    public enum LintSeverity : byte
    {
        Warning = 1,
        Error = 2,
    }

    public readonly struct LintIssue
    {
        public string RuleId { get; }
        public LintSeverity Severity { get; }
        public int LineNumber { get; }
        public string Message { get; }

        public LintIssue(string ruleId, LintSeverity severity, int lineNumber, string message)
        {
            RuleId = ruleId;
            Severity = severity;
            LineNumber = lineNumber;
            Message = message ?? string.Empty;
        }

        public override string ToString() =>
            $"[{Severity}][{RuleId}] line {LineNumber}: {Message}";
    }

    /// <summary>
    /// Lint rules for Maqui component source — mitigates BUG.1 drift back
    /// to prefab/MVPa authoring. Rules:
    ///
    /// <list type="bullet">
    ///   <item><b>BUG.1-a</b> — no <c>Resources.Load</c> (asset loading is the
    ///   backend's job; component takes texture keys via params).</item>
    ///   <item><b>BUG.1-b</b> — no <c>GameObject.Instantiate</c> (component
    ///   composes primitives; not a prefab spawner).</item>
    ///   <item><b>BUG.1-c</b> — no <c>[AddComponentMenu]</c> attribute
    ///   (component is a method, not a MonoBehaviour).</item>
    /// </list>
    ///
    /// <para>The scanner is intentionally a string matcher, not a Roslyn
    /// analyzer — runs in &lt;5ms on a 1000-line file, ships in the
    /// standalone csproj, no Unity dependency. Trade-off: comments mentioning
    /// the forbidden symbol are flagged; use <c>// maqui-lint:ignore-next-line</c>
    /// to silence a single line when the rule is wrong.</para>
    /// </summary>
    public static class MaquiComponentLint
    {
        private static readonly (string ruleId, string pattern, string message)[] s_rules = new[]
        {
            ("BUG.1-a", "Resources.Load", "Resources.Load is forbidden — pass asset key via param; backend resolves."),
            ("BUG.1-b", "GameObject.Instantiate", "GameObject.Instantiate is forbidden — component composes primitives, not prefabs."),
            ("BUG.1-c", "[AddComponentMenu", "[AddComponentMenu] is forbidden — component is a method, not a MonoBehaviour."),
        };

        private const string IgnoreNextLine = "maqui-lint:ignore-next-line";

        /// <summary>
        /// Scan <paramref name="source"/> for lint issues. Returns an empty
        /// list if clean.
        /// </summary>
        public static IReadOnlyList<LintIssue> Check(string source)
        {
            var issues = new List<LintIssue>();
            if (string.IsNullOrEmpty(source)) return issues;

            string[] lines = source.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                // Skip if previous line carries the ignore-next directive.
                if (i > 0 && lines[i - 1].IndexOf(IgnoreNextLine, StringComparison.Ordinal) >= 0)
                    continue;

                foreach (var rule in s_rules)
                {
                    int idx = line.IndexOf(rule.pattern, StringComparison.Ordinal);
                    if (idx < 0) continue;
                    // Detect the rule's own message body in this file (would
                    // self-flag the lint definitions). Skip if the line looks
                    // like part of a string literal containing the rule id.
                    if (line.IndexOf(rule.ruleId, StringComparison.Ordinal) >= 0) continue;
                    issues.Add(new LintIssue(rule.ruleId, LintSeverity.Error, i + 1, rule.message));
                }
            }

            return issues;
        }

        /// <summary>
        /// Convenience: true if <see cref="Check"/> returns no Error-level issues.
        /// </summary>
        public static bool IsClean(string source)
        {
            var issues = Check(source);
            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].Severity == LintSeverity.Error) return false;
            }
            return true;
        }
    }
}
