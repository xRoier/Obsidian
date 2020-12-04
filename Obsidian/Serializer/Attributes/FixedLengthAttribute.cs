﻿using System;

namespace Obsidian.Serializer.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class FixedLengthAttribute : Attribute
    {
        public int Length { get; private set; }

        public FixedLengthAttribute(int length)
        {
            Length = length;
        }
    }
}
