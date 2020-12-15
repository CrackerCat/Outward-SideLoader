﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SideLoader
{
    public class SL_Ammunition : SL_Weapon
    {
        // public SL_Transform ProjectileFXPrefab;
        public int? PoolCapacity;

        public override void ApplyToItem(Item item)
        {
            base.ApplyToItem(item);

            if (this.PoolCapacity != null)
            {
                (item as Ammunition).PoolCapacity = (int)this.PoolCapacity;
            }
        }

        public override void SerializeItem(Item item)
        {
            base.SerializeItem(item);

            PoolCapacity = (item as Ammunition).PoolCapacity;
        }
    }
}
