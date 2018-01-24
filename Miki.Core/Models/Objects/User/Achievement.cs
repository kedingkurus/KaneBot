﻿using System;

namespace Miki.Models
{
    public class Achievement
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public short Rank { get; set; }

		public DateTime UnlockedAt { get; set; }

		public User User { get; set; }
    }
}