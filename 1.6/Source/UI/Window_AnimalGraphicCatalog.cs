using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace SurveillanceShift
{
	[HotSwappable]
	public class Window_AnimalGraphicCatalog : Window
	{
		private const float HeaderHeight = 70f;
		private const float CardHeight = 205f;
		private const float CardGap = 8f;
		private const float IconSize = 92f;
		private const int Columns = 5;

		private readonly List<ThingDef> animals;
		private Vector2 scrollPosition;
		private string search = string.Empty;

		public override Vector2 InitialSize => new Vector2(900f, 700f);

		public Window_AnimalGraphicCatalog()
		{
			doCloseX = true;
			closeOnClickedOutside = false;
			absorbInputAroundWindow = false;
			resizeable = true;
			animals = DefDatabase<ThingDef>.AllDefsListForReading
				.Where(def => def.category == ThingCategory.Pawn
					&& def.race != null
					&& def.race.Animal
					&& (def.thingClass == null || !typeof(Corpse).IsAssignableFrom(def.thingClass)))
				.OrderBy(def => def.LabelCap.ToString())
				.ThenBy(def => def.defName)
				.ToList();
		}

		public override void DoWindowContents(Rect inRect)
		{
			Text.Font = GameFont.Medium;
			Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 30f), "Loaded animals — south-facing graphics");
			Text.Font = GameFont.Small;

			var searchRect = new Rect(inRect.x, inRect.y + 36f, 330f, 28f);
			search = Widgets.TextField(searchRect, search);

			var filtered = animals.Where(MatchesSearch).ToList();
			Text.Anchor = TextAnchor.MiddleRight;
			Widgets.Label(new Rect(searchRect.xMax + 12f, searchRect.y, inRect.xMax - searchRect.xMax - 12f, searchRect.height),
				$"{filtered.Count} of {animals.Count} animal races");
			Text.Anchor = TextAnchor.UpperLeft;

			var outRect = new Rect(inRect.x, inRect.y + HeaderHeight, inRect.width, inRect.height - HeaderHeight);
			var viewWidth = outRect.width - 16f;
			var cardWidth = (viewWidth - (Columns - 1) * CardGap) / Columns;
			var rows = Mathf.CeilToInt(filtered.Count / (float)Columns);
			var viewRect = new Rect(0f, 0f, viewWidth, Math.Max(outRect.height, rows * (CardHeight + CardGap)));

			Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
			for (var i = 0; i < filtered.Count; i++)
			{
				var animal = filtered[i];
				var card = new Rect((i % Columns) * (cardWidth + CardGap), (i / Columns) * (CardHeight + CardGap), cardWidth, CardHeight);
				Widgets.DrawMenuSection(card);
				if (Mouse.IsOver(card))
				{
					Widgets.DrawHighlight(card);
				}

				var iconRect = new Rect(card.x + (card.width - IconSize) / 2f, card.y + 5f, IconSize, IconSize);
				Widgets.ThingIcon(iconRect, animal);

				Text.Anchor = TextAnchor.UpperCenter;
				Text.Font = GameFont.Small;
				Widgets.Label(new Rect(card.x + 4f, iconRect.yMax, card.width - 8f, 22f), animal.LabelCap);
				GUI.color = ColoredText.SubtleGrayColor;
				Text.Font = GameFont.Tiny;
				Widgets.Label(new Rect(card.x + 4f, iconRect.yMax + 21f, card.width - 8f, 18f), animal.defName);
				GUI.color = Color.white;

				Text.Anchor = TextAnchor.UpperLeft;
				var products = ButcherProductsLabel(animal);
				Widgets.Label(new Rect(card.x + 7f, iconRect.yMax + 42f, card.width - 14f, 52f), products);
				TooltipHandler.TipRegion(card, $"{animal.LabelCap}\n{animal.defName}\n\n{products}\n\nClick to arm the spawn tool.");

				if (Widgets.ButtonInvisible(card))
				{
					QueueSpawnTool(animal);
					Close();
				}
			}

			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.UpperLeft;
			Widgets.EndScrollView();
		}

		private bool MatchesSearch(ThingDef animal)
		{
			if (search.NullOrEmpty())
			{
				return true;
			}

			return animal.LabelCap.ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
				|| animal.defName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private static string ButcherProductsLabel(ThingDef animal)
		{
			var products = new List<string>();
			var meatAmount = Mathf.RoundToInt(animal.GetStatValueAbstract(StatDefOf.MeatAmount));
			if (animal.race.meatDef != null && meatAmount > 0)
			{
				products.Add($"{animal.race.meatDef.LabelCap} ×{meatAmount}");
			}

			var leatherAmount = Mathf.RoundToInt(animal.GetStatValueAbstract(StatDefOf.LeatherAmount));
			if (animal.race.leatherDef != null && leatherAmount > 0)
			{
				products.Add($"{animal.race.leatherDef.LabelCap} ×{leatherAmount}");
			}

			if (animal.butcherProducts != null)
			{
				products.AddRange(animal.butcherProducts.Select(product => $"{product.thingDef.LabelCap} ×{product.count}"));
			}

			return products.Count > 0 ? "Butcher: " + string.Join(", ", products) : "Butcher: no products";
		}

		private static void QueueSpawnTool(ThingDef animal)
		{
			var kind = animal.race.AnyPawnKind;
			if (kind == null)
			{
				Messages.Message($"No PawnKindDef is available for {animal.LabelCap}.", MessageTypeDefOf.RejectInput, false);
				return;
			}

			DebugTools.curTool = new DebugTool($"Spawn {animal.LabelCap}", delegate
			{
				var map = Find.CurrentMap;
				var cell = UI.MouseCell();
				if (map == null || !cell.InBounds(map))
				{
					return;
				}

				var pawn = PawnGenerator.GeneratePawn(kind);
				GenSpawn.Spawn(pawn, cell, map);
				pawn.Rotation = Rot4.South;
			});

			Messages.Message($"Spawn {animal.LabelCap} armed. Click a map cell to spawn it.", MessageTypeDefOf.TaskCompletion, false);
		}
	}

	public static class DebugActions_AnimalGraphicCatalog
	{
		[DebugAction("Surveillance Shift", "Open animal graphic catalog (south)", allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void OpenAnimalGraphicCatalog()
		{
			Find.WindowStack.Add(new Window_AnimalGraphicCatalog());
		}
	}
}
