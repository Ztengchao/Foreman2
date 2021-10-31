﻿using System;
using System.Collections.Generic;
using System.Drawing;

namespace Foreman
{
	public interface Item : DataObjectBase
	{
		Subgroup MySubgroup { get; }

		IReadOnlyCollection<Recipe> ProductionRecipes { get; }
		IReadOnlyCollection<Recipe> ConsumptionRecipes { get; }
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
			FuelValue = 0;
			IsTemperatureDependent = false;
			IsMissing = isMissing;
		}

		public override string ToString() { return String.Format("Item: {0}", Name); }

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
	}
}
