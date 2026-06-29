namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;

[method: JobInfo("One-Off Job")]
internal sealed class OneOffJob(JobManager jobManager) : WikiJob(jobManager, JobType.ReadOnly)
{
	protected override void Main()
	{
		var valid = new HashSet<string>(StringComparer.Ordinal)
		{
			"Alit Bite", "Armor Eater", "Aryon's Rest", "Ash Curse: Fatigue", "Ash Curse: Health", "Ash Curse: Spell Points", "Ash Feast", "Black Hand", "Blessings of the Fourth Corner", "Blood Despair", "Blood Sacrifice", "Burning Touch", "Clench", "Clumsiness", "Clumsy Touch", "Cruel Firebloom", "Curse Agility", "Curse Endurance", "Curse Fatigue", "Curse Health", "Curse Intelligence", "Curse Luck", "Curse Personality", "Curse Speed", "Curse Spell Points", "Curse Strength", "Curse Willpower", "Daedric Bite", "Dagoth's Bosom", "Damage Attribute", "Damage Fatigue", "Damage Health", "Damage Magicka", "Deadly Poison", "Deadly Poison [Ranged]", "Dire Viper", "Dire Weakness to Fire", "Dire Weakness to Frost", "Dire Weakness to Magicka", "Dire Weakness to Poison", "Dire Weakness to Shock", "Disintegrate Armor", "Disintegrate Armor spell", "Disintegrate Weapon", "Disintegrate Weapon spell", "Distracting Touch", "Distraction", "Doze", "Drain Acrobatics spell", "Drain Alchemy spell", "Drain Alteration spell", "Drain Armorer spell", "Drain Athletics", "Drain Attribute", "Drain Axe spell", "Drain Block", "Drain Blood", "Drain Blunt Weapon spell", "Drain Conjuration spell", "Drain Destruction", "Drain Enchant spell", "Drain Fatigue", "Drain Hand-to-Hand spell", "Drain Health", "Drain Heavy Armor spell", "Drain Illusion spell", "Drain Light Armor spell", "Drain Long Blade spell", "Drain Magicka", "Drain Marksman spell", "Drain Medium Armor spell", "Drain Mercantile spell", "Drain Mysticism spell", "Drain Restoration spell", "Drain Security spell", "Drain Short Blade spell", "Drain Skill", "Drain Sneak spell", "Drain Spear spell", "Drain Speechcraft spell", "Drain Unarmored spell", "Dratha's Spite", "Dread Curse: Agility", "Dread Curse: Endurance", "Dread Curse: Fatigue", "Dread Curse: Health", "Dread Curse: Intelligence", "Dread Curse: Luck", "Dread Curse: Personality", "Dread Curse: Speed", "Dread Curse: Spell Points", "Dread Curse: Strength", "Dread Curse: Willpower", "Electrical Discharge", "Emasculate", "Enervate", "Enervating Touch", "Evil Eye", "Exhausting Touch", "Exhaustion", "Fire Bite", "Fire Damage", "Fire Storm", "Fire Trap", "Fireball", "Firebloom", "Firefist", "Five Fingers of Pain", "Flame", "Flamebolt spell", "Flay Spirit", "Flay Spirit [Ranged]", "Fleabite", "Force Bolt", "Freezing Touch", "Frost Bolt", "Frost Damage", "Frost Storm", "Frost Trap", "Frostball", "Frostbite", "Frostbloom", "Frostfist", "Fuddle", "Gash Spirit", "Gash Spirit Ranged", "Ghost Curse", "Ghost Snake", "Glyph of Weakness", "God's Fire", "God's Frost", "God's Spark", "Gothren's Gout", "Grave Curse: Agilitiy", "Grave Curse: Endurance", "Grave Curse: Fatigue", "Grave Curse: Health", "Grave Curse: Intelligence", "Grave Curse: Luck", "Grave Curse: Magicka", "Grave Curse: Personality", "Grave Curse: Speed", "Grave Curse: Strength", "Grave Curse: Willpower", "Greater Fireball", "Greater Frostball", "Greater Shockball", "Gripes", "Hand of Araynys", "Hand of Dagoth", "Hand of Decay", "Hand of Endus", "Hand of Gilvoth", "Hand of Odros", "Hand of Sleep", "Hand of Tureynul", "Hand of Uthol", "Hand of Vemyn", "Hand of Vivec", "Heartbite", "Hex", "Hornhand", "Ironhand", "Knuckle Luck", "Kwama Poison", "Lifeforce Trap", "Lightning Bolt", "Lightning Storm", "Lustidrike Cocktail", "Magicka Leech", "Misfortunate Touch", "Misfortune", "Ordeal of St. Olms", "Poison", "Poison Trap", "Poison spell", "Poisonbloom", "Poisonous Touch", "Potent Poison", "Potent Poison [Ranged]", "Scourge Blade", "See Also", "Shalk's Fire Bite", "Shard", "Shock", "Shock Damage", "Shock Trap", "Shockball", "Shockbite", "Shockbloom", "Shocking Touch", "Sleep", "Smite the Ungodly", "Soulpinch", "Spark", "Spell Drain", "Sphere of Negation", "Spirit Knife", "Spite", "Spite Touch", "Star-Curse", "Sting", "Stormhand", "Strain", "Straining Touch", "Strength Leech", "Stumble", "Temptation", "Tempting Touch", "Test Spell", "Torpor", "Torpor Touch", "TouchDrain Acrobatics", "TouchDrain Alchemy", "TouchDrain Alteration", "TouchDrain Armorer", "TouchDrain Athletics", "TouchDrain Axe", "TouchDrain Block", "TouchDrain Blunt Weapon", "TouchDrain Conjuration", "TouchDrain Destruction", "TouchDrain Enchant", "TouchDrain Hand to Hand", "TouchDrain Heavy Armor", "TouchDrain Illusion", "TouchDrain Light Armor", "TouchDrain Long Blade", "TouchDrain Marksman", "TouchDrain Medium Armor", "TouchDrain Mercantile", "TouchDrain Mysticism", "TouchDrain Restoration", "TouchDrain Security", "TouchDrain Short Blade", "TouchDrain Sneak", "TouchDrain Spear", "TouchDrain Speechcraft", "TouchDrain Unarmored", "Toxic Cloud", "Unavailable Spells", "Vampire's Kiss", "Viper", "Viperbite", "Viperbolt", "Vivec's Wrath", "Weakening Touch", "Weakness", "Weakness to Blight Disease spell", "Weakness to Common Disease", "Weakness to Common Disease spell", "Weakness to Corprus Disease spell", "Weakness to Corpus Disease", "Weakness to Fire", "Weakness to Fire spell", "Weakness to Frost", "Weakness to Frost spell", "Weakness to Magicka", "Weakness to Magicka spell", "Weakness to Poison", "Weakness to Poison spell", "Weakness to Shock", "Weakness to Shock spell", "Weapon Eater", "Weariness", "Wearying Touch", "Weeping Wound", "Wild Clumsiness", "Wild Distraction", "Wild Exhaustion", "Wild Flay Spirit", "Wild Misfortune", "Wild Shockbloom", "Wild Spite", "Wild Strain", "Wild Temptation", "Wild Torpor", "Wild Weakness", "Wild Weeping Wound", "Wizard's Fire", "Wizard Rend", "Woe", "Wound", "Wounding Touch", "Wrath of Araynys", "Wrath of Dagoth", "Wrath of Endus", "Wrath of Gilvoth", "Wrath of Odros", "Wrath of Tureynul", "Wrath of Uthol", "Wrath of Vemyn", "Wrath of Vivec", "note Fire Bite start", "note Greater Shockball note", "suicide", "uesp D 1", "uesp D 2", "uesp D 3"
		};
		var pages = new PageCollection(this.Site);
		pages.GetBacklinks("Morrowind:Destruction Spells", BacklinksTypes.Backlinks, true, Filter.Only);
		foreach (var page in pages)
		{
			if (!page.IsRedirect)
			{
				throw new InvalidOperationException($"WTF on {page.Title}");
			}

			var parser = new SiteParser(page);
			var link = parser.LinkNodes.First();
			if (link is null)
			{
				throw new InvalidOperationException();
			}

			var siteLink = SiteLink.FromLinkNode(this.Site, link);
			if (siteLink.Fragment is not null && !valid.Contains(siteLink.Fragment))
			{
				Debug.WriteLine($"{siteLink} not found on {page.Title}");
			}
		}
	}
}