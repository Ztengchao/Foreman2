﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Foreman
{
	public interface Item : DataObjectBase
	{
		Subgroup MySubgroup { get; }

		IReadOnlyCollection<Recipe> ProductionRecipes { get; }
		IReadOnlyCollection<Recipe> ConsumptionRecipes { get; }

		IReadOnlyCollection<Recipe> AvailableProductionRecipes { get; }
		IReadOnlyCollection<Recipe> AvailableConsumptionRecipes { get; }

		bool IsMissing { get; }
		bool IsFluid { get; }
		bool IsTemperatureDependent { get; }
		double DefaultTemperature { get; }

		float FuelValue { get; }
		Item BurnResult { get; }
		IReadOnlyCollection<Assembler> FuelsAssemblers { get; }

		string GetTemperatureRangeFriendlyName(fRange tempRange);
	}

	public class ItemPrototype : DataObjectBasePrototype, Item
	{
		public Subgroup MySubgroup { get { return mySubgroup; } }

		public IReadOnlyCollection<Recipe> ProductionRecipes { get { return productionRecipes; } }
		public IReadOnlyCollection<Recipe> ConsumptionRecipes { get { return consumptionRecipes; } }
		public IReadOnlyCollection<Recipe> AvailableProductionRecipes { get; private set; }
		public IReadOnlyCollection<Recipe> AvailableConsumptionRecipes { get; private set; }

		public bool IsMissing { get; private set; }

		public bool IsFluid { get; private set; }
		public bool IsTemperatureDependent { get; internal set; } //while building the recipes, if we notice any product fluid NOT producted at its default temperature, we mark that fluid as temperature dependent
		public double DefaultTemperature { get; internal set; } //for liquids

		public float FuelValue { get; internal set; }
		public Item BurnResult { get; internal set; }
		public IReadOnlyCollection<Assembler> FuelsAssemblers { get { return fuelsAssemblers; } }

		internal SubgroupPrototype mySubgroup;

		internal HashSet<RecipePrototype> productionRecipes { get; private set; }
		internal HashSet<RecipePrototype> consumptionRecipes { get; private set; }
		internal HashSet<AssemblerPrototype> fuelsAssemblers { get; private set; }

		public ItemPrototype(DataCache dCache, string name, string friendlyName, bool isfluid, SubgroupPrototype subgroup, string order, bool isMissing = false) : base(dCache, name, friendlyName, order)
		{
			mySubgroup = subgroup;
			subgroup.items.Add(this);

			productionRecipes = new HashSet<RecipePrototype>();
			consumptionRecipes = new HashSet<RecipePrototype>();
			fuelsAssemblers = new HashSet<AssemblerPrototype>();

			IsFluid = isfluid;
			DefaultTemperature = 0;
			FuelValue = 1; //useful for preventing overlow issues when using missing items / non-fuel items (loading with wrong mods / importing from alt mod group can cause this)
			IsTemperatureDependent = false;
			IsMissing = isMissing;
		}

		internal void UpdateAvailabilities()
		{
			AvailableConsumptionRecipes = new HashSet<Recipe>(consumptionRecipes.Where(r => r.Available));
			AvailableProductionRecipes = new HashSet<Recipe>(productionRecipes.Where(r => r.Available));
		}

		public string GetTemperatureRangeFriendlyName(fRange tempRange)
		{
			if (tempRange.Ignore)
				return this.FriendlyName;

			string name = this.FriendlyName;
			bool includeMin = tempRange.Min >= float.MinValue; //== float.NegativeInfinity;
			bool includeMax = tempRange.Max <= float.MaxValue; //== float.PositiveInfinity;

			if (tempRange.Min == tempRange.Max)
				name += " (" + tempRange.Min.ToString("0") + "°)";
			else if (includeMin && includeMax)
				name += " (" + tempRange.Min.ToString("0") + "°-" + tempRange.Max.ToString("0") + "°)";
			else if (includeMin)
				name += " (min " + tempRange.Min.ToString("0") + "°)";
			else if (includeMax)
				name += " (max " + tempRange.Max.ToString("0") + "°)";
			else
				name += "(any°)";

			return name;
		}

		public override string ToString() { return string.Format("Item: {0}", Name); }
	}
}
