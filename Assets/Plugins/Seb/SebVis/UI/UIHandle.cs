using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Seb.Vis.UI
{
    public readonly struct UIHandle : IEquatable<UIHandle>
    {
        public readonly string stringID;
        public readonly int intID;

        public UIHandle(string stringID, int intID)
        {
            this.stringID = stringID;
            this.intID = intID;
        }

        public UIHandle(string stringID)
        {
            this.stringID = stringID;
            this.intID = 0;
        }

        public bool Equals(UIHandle other)
        {
            return intID == other.intID && string.Equals(stringID, other.stringID, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return stringID.GetHashCode() + intID;
        }

    }
}