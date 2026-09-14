// lint-sample-path: Packages/com.dreamtech.liveops/Runtime/Core/Calendar/Document/NewerSyntax.cs
// Mẫu vi phạm: cú pháp sau C# 9 / không chạy trên 2022.3.
// lint-expect: csharp-version
// lint-expect: csharp-version
// lint-expect: csharp-version
// lint-expect: csharp-version
global using System;

namespace DreamTech.LiveOps;

public sealed record LiveEventRecordSample(string Identifier);

public sealed class InitSample
{
    public string Name { get; init; }
}
