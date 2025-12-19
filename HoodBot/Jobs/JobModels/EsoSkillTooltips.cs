namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Diagnostics;
using System.Globalization;

/// <summary>This class is a modified version of the EsoSkillTooltips class from the ESO Log project (esoSkillTooltips.js), adapted for use in HoodBot.</summary>
/// <remarks>While not all functions are replicated, and those that are aren't necessarily completely identical (even allowing for language differences), any function that's essentially the same uses the same name as the original for easy comparison.</remarks>
internal static partial class EsoSkillTooltips
{
	/* Static class for now, may be better as instantiable with parameters moved out of individual functions. */

	public static string ComputeEsoSkillTooltipCoefDescription2(Coefficient coef, EsoSkillInputValues inputValues)
	{
		// All initialization code in the original function is either not necessary or has been moved closer to where it's actually used.
		double retval;
		switch (coef.CoefficientType)
		{
			case CoefficientTypes.HealthOld:
			case CoefficientTypes.Health:
				retval = Math.Floor(coef.A * inputValues.Health) + coef.C;
				break;
			case CoefficientTypes.MagickaOld:
			case CoefficientTypes.Magicka:
				retval = Math.Floor(coef.A * inputValues.Magicka) + Math.Floor(coef.B * inputValues.SpellDamage) + coef.C;
				break;
			case CoefficientTypes.StaminaOld:
			case CoefficientTypes.Stamina:
				retval = Math.Floor(coef.A * inputValues.Stamina) + Math.Floor(coef.B * inputValues.WeaponDamage) + coef.C;
				break;
			case CoefficientTypes.UltimateOld:
			case CoefficientTypes.Ultimate:
				retval =
					Math.Floor(coef.A * Math.Max(inputValues.Magicka, inputValues.Stamina)) +
					Math.Floor(coef.B * Math.Max(inputValues.SpellDamage, inputValues.WeaponDamage)) +
					coef.C;
				break;
			case CoefficientTypes.SoulTether:
				retval =
					Math.Floor(coef.A * Math.Max(inputValues.Magicka, inputValues.Stamina)) +
					Math.Floor(coef.B * inputValues.SpellDamage) +
					coef.C;
				break;
			case CoefficientTypes.LightArmor:
				if (inputValues.LightArmor is int lightArmor)
				{
					retval = coef.A * lightArmor + coef.C;
					break;
				}

				return coef.C == 0
					? $"({coef.A:g5} * LightArmor)"
					: $"({coef.A:g5} * LightArmor + {coef.C:g5})";
			case CoefficientTypes.MediumArmor:
				if (inputValues.MediumArmor is int mediumArmor)
				{
					retval = coef.A * mediumArmor + coef.C;
					break;
				}

				return coef.C == 0
					? $"({coef.A:g5} * MediumArmor)"
					: $"({coef.A:g5} * MediumArmor + {coef.C:g5})";
			case CoefficientTypes.HeavyArmor: // Heavy Armor
				if (inputValues.HeavyArmor is int heavyArmor)
				{
					retval = coef.A * heavyArmor + coef.C;
					break;
				}

				return coef.C == 0
					? $"({coef.A:g5} * HeavyArmor)"
					: $"({coef.A:g5} * HeavyArmor + {coef.C:g5})";
			case CoefficientTypes.WeaponDagger:
				if (inputValues.DaggerWeapon is int daggerWeapon)
				{
					retval = coef.A * daggerWeapon;
					break;
				}

				return $"({coef.A:g5} * Dagger)";
			case CoefficientTypes.ArmorType:
				if (inputValues.ArmorTypes is int armorTypes)
				{
					retval = coef.A * armorTypes;
					break;
				}

				return $"({coef.A:g5} * ArmorTypes)";
			case CoefficientTypes.Damage:
				retval = Math.Floor(coef.A * inputValues.SpellDamage) + Math.Floor(coef.B * inputValues.WeaponDamage) + coef.C;
				break;
			case CoefficientTypes.Assassination:
				retval = coef.A * inputValues.AssassinSkills;
				break;
			case CoefficientTypes.FightersGuild:
				retval = coef.A * inputValues.FightersGuildSkills;
				break;
			case CoefficientTypes.DraconicPower:
				retval = coef.A * inputValues.DraconicPowerSkills;
				break;
			case CoefficientTypes.Shadow:
				retval = coef.A * inputValues.ShadowSkills;
				break;
			case CoefficientTypes.Siphoning:
				retval = coef.A * inputValues.SiphoningSkills;
				break;
			case CoefficientTypes.Sorcerer:
				retval = coef.A * inputValues.SorcererSkills;
				break;
			case CoefficientTypes.MagesGuild:
				retval = coef.A * inputValues.MagesGuildSkills;
				break;
			case CoefficientTypes.Support:
				retval = coef.A * inputValues.SupportSkills;
				break;
			case CoefficientTypes.AnimalCompanion:
				retval = coef.A * inputValues.AnimalCompanionSkills;
				break;
			case CoefficientTypes.GreenBalance:
				retval = coef.A * inputValues.GreenBalanceSkills;
				break;
			case CoefficientTypes.WintersEmbrace:
				retval = coef.A * inputValues.WintersEmbraceSkills;
				break;
			case CoefficientTypes.MagicHealthCapped:
				retval = Math.Min(Math.Floor(coef.A * inputValues.Magicka), Math.Floor(coef.B * inputValues.Health));
				break;
			case CoefficientTypes.BoneTyrant:
				retval = coef.A * inputValues.BoneTyrantSkills;
				break;
			case CoefficientTypes.GraveLord:
				retval = coef.A * inputValues.GraveLordSkills;
				break;
			case CoefficientTypes.SpellDamageCapped:
				retval = Math.Min(Math.Floor(coef.A * inputValues.SpellDamage) + coef.B, coef.C);
				break;
			case CoefficientTypes.MagickaWeaponDamage:
				retval = Math.Floor(coef.A * inputValues.Magicka) + Math.Floor(coef.B * inputValues.WeaponDamage) + coef.C;
				break;
			case CoefficientTypes.MagickaSpellDamageCapped:
				var halfMax = coef.C / 2;
				retval = Math.Min(
					Math.Min(Math.Floor(coef.A * inputValues.Magicka), halfMax) +
					Math.Min(Math.Floor(coef.B * inputValues.SpellDamage), halfMax),
					coef.C);
				break;
			case CoefficientTypes.WeaponPower:
				retval = Math.Floor(coef.A * inputValues.WeaponPower) + coef.C;
				break;
			case CoefficientTypes.ConstantValue:
				if (coef.RawType == RawTypes.HealOverTime)
				{
					Debug.WriteLine(coef.Value);
					retval = double.Parse(coef.Value, CultureInfo.InvariantCulture);
					break;
				}

				return coef.Value;
			case CoefficientTypes.HealthOrSpellDamage:
				retval = Math.Max(
					Math.Floor(coef.A * inputValues.SpellDamage),
					Math.Floor(coef.B * inputValues.Health)) +
					coef.C;
				break;
			case CoefficientTypes.Resistance:
				retval = Math.Floor(coef.A * Math.Max(inputValues.SpellResist, inputValues.PhysicalResist)) + coef.C;
				break;
			case CoefficientTypes.MagickaLightArmor:
				if (inputValues.LightArmor is null)
				{
					retval = Math.Min(Math.Floor(coef.A * inputValues.Magicka), coef.C * inputValues.Health);
				}
				else
				{
					retval = Math.Min(Math.Floor(coef.A * inputValues.Magicka) * (1.0 + coef.B * inputValues.LightArmor.Value), coef.C * inputValues.Health);  // TODO: Check rounding order
				}

				break;
			case CoefficientTypes.HealthOrDamage:
				retval = Math.Max(
					Math.Floor(coef.A * inputValues.SpellDamage) + Math.Floor(coef.B * inputValues.WeaponDamage),
					Math.Floor(coef.C * inputValues.Health));
				break;
			case CoefficientTypes.HeraldOfTheTome:
				retval = coef.A * inputValues.HeraldoftheTomeSkills;
				break;
			case CoefficientTypes.SoldierOfApocrypha:
				retval = coef.A * inputValues.SoldierofApocryphaSkills;
				break;
			case CoefficientTypes.HealthOrMagickaCapped:
				retval = Math.Max(
					Math.Floor(coef.A * inputValues.Magicka),
					Math.Floor(coef.B * inputValues.Health));
				var maxValue = Math.Floor(coef.C * inputValues.Health);
				if (maxValue > 0)
				{
					retval = Math.Max(retval, maxValue);
				}

				break;
			case CoefficientTypes.Invalid:
			case CoefficientTypes.Werewolf:
			case CoefficientTypes.MountStamina:
			case CoefficientTypes.Daedric:
				throw new InvalidOperationException($"Unhandled coefficient type {coef.CoefficientType}");
			default:
				throw new InvalidOperationException($"Unrecognized coefficient type {coef.CoefficientType}");
		}

		// Debug.WriteLine($"{coef.AbilityId} / {coef.Index} (rank {rank}) / {coef.RawType}: {retval} * {coef.Factor} = {Math.Floor(retval * coef.Factor)}");
		retval = Math.Floor(retval * coef.Factor);
		retval = (true || coef.RawType == RawTypes.DerivedOneDec)
			? Math.Floor(retval * 10) / 10
			: Math.Floor(retval);

		if (retval < 0)
		{
			throw new InvalidOperationException("Return value less than zero.");
		}

		if ((coef.RawType == RawTypes.DamageOverTime || coef.RawType == RawTypes.HealOverTime) && coef.Duration > 0)
		{
			var dur = coef.Duration;

			double dotFactor;
			if (coef.NewTicks != -1)
			{
				// TODO: Figure out what this was supposed to do. Right now, newTicks is never set. Looks like this code is incomplete.
				dotFactor = coef.NewTicks;
				coef.NewTicks = -1;
			}
			else
			{
				dotFactor = coef.TickTime > 0
					? (dur + coef.TickTime) / coef.TickTime
					: dur / 1000;
			}

			if (dotFactor != 1)
			{
				retval = Math.Floor(Math.Floor(retval) * dotFactor);
			}
		}

		return $"{retval:g5}";
	}

	/*
		// Intended for Skill Browser's raw output, which we don't use.
		public static void SetEsoSkillTooltipRawOutputValue(Skill skill, int index, string key, string value)
		{
		}
	*/
}