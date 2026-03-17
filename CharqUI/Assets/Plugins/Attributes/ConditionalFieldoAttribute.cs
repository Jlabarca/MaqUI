using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class ConditionalFieldoAttribute : PropertyAttribute
{
    public string ConditionFieldName { get; }
    public bool Inverse { get; }

    public ConditionalFieldoAttribute(string conditionFieldName, bool inverse = false)
    {
        ConditionFieldName = conditionFieldName;
        Inverse = inverse;
    }
}