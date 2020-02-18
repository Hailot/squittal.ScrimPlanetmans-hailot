﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace squittal.ScrimPlanetmans.ScrimMatch.Models
{
    public class Ruleset
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public DateTime DateCreated { get; set; } // TODO: Date of first save (?)

        public DateTime? DateLastModified { get; set; }
        
        public IEnumerable<RulesetItemCategoryRule> ItemCategoryRules { get; set; }
        public IEnumerable<RulesetActionRule> ActionRules { get; set; }

    }
}