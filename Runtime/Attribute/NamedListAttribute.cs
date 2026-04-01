using System;
using UnityEngine;

namespace Extensions.Layout.Attribute
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class NamedListAttribute : PropertyAttribute { }
}