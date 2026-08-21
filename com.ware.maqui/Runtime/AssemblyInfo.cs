// SPDX-License-Identifier: MIT
// MaqUI v2 — expose internals to the test assemblies.
//
// Two test surfaces:
//   - Maqui.Tests           — standalone xUnit, runs in `dotnet test`
//                                against a Unity-shimmed compile of these sources.
//                                Default for everything testable without Unity.
//   - Maqui.Tests.Editor    — NUnit harness, only used for Unity-only tests
//                                (PlayMode, render, UnityEngine.Object lifecycle).
//                                Re-created when P3/P4/P5/P6 surface such tests.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Maqui.Tests")]
[assembly: InternalsVisibleTo("Maqui.Tests.Editor")]
[assembly: InternalsVisibleTo("Maqui.Tests.Runtime")]
