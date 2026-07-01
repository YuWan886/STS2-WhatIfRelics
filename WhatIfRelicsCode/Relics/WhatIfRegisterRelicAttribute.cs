namespace WhatIfRelics.WhatIfRelicsCode.Relics;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class WhatIfRegisterRelicAttribute(Type poolType) : Attribute
{
    public Type PoolType { get; } = poolType;

    public int Order { get; init; }

    public string? StableEntryStem { get; init; }

    public string? FullPublicEntry { get; init; }
}
