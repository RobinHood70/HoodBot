namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon;

	public class PageMover : PageMoverJob
	{
		#region Constructors
		[JobInfo("Page Mover")]
		public PageMover(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
			this.EditSummaryMove = "Undo erroneous rename";
			this.MoveAction = MoveAction.RenameOnly;
			this.FollowUpActions = FollowUpActions.EmitReport;
			DeleteFiles();
		}
		#endregion

		#region Protected Override Methods
		protected override void PopulateReplacements()
		{
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Pack Spider 02.jpg", "File:DB-creature-Pack Spider 02.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Old Salty.jpg", "File:DB-creature-Old Salty.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Oil Spider.jpg", "File:DB-creature-Oil Spider.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Netch.jpg", "File:DB-creature-Netch.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Millius.jpg", "File:DB-creature-Millius.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Maximian Axius.jpg", "File:DB-creature-Maximian Axius.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Lurker.jpg", "File:DB-creature-Lurker.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Lurker Sentinel.jpg", "File:DB-creature-Lurker Sentinel.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Lord Tusk.jpg", "File:DB-creature-Lord Tusk.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Kruziikrel.jpg", "File:DB-creature-Kruziikrel.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Krosulhah.jpg", "File:DB-creature-Krosulhah.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Karstaag.jpg", "File:DB-creature-Karstaag.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Jumping Flame Spider.jpg", "File:DB-creature-Jumping Flame Spider.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Isobel.jpg", "File:DB-creature-Isobel.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Hulking Draugr.jpg", "File:DB-creature-Hulking Draugr.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Gratian Caerellius.jpg", "File:DB-creature-Gratian Caerellius.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Gatekeeper.jpg", "File:DB-creature-Gatekeeper.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Fire Wyrm.jpg", "File:DB-creature-Fire Wyrm.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Felsaad Tern.jpg", "File:DB-creature-Felsaad Tern.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Fallaise.jpg", "File:DB-creature-Fallaise.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Ettiene.jpg", "File:DB-creature-Ettiene.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Dwarven Ballista.jpg", "File:DB-creature-Dwarven Ballista.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Dukaan.jpg", "File:DB-creature-Dukaan.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Corrupted Shade.jpg", "File:DB-creature-Corrupted Shade.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Burnt Spriggan.jpg", "File:DB-creature-Burnt Spriggan.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Bull Netch.jpg", "File:DB-creature-Bull Netch.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Bull Netch 02.jpg", "File:DB-creature-Bull Netch 02.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Bristleback.jpg", "File:DB-creature-Bristleback.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Bilgemuck.jpg", "File:DB-creature-Bilgemuck.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Betty Netch.jpg", "File:DB-creature-Betty Netch.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Ash Spawn2.png", "File:DB-creature-Ash Spawn2.png"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Ash Spawn.jpg", "File:DB-creature-Ash Spawn.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Ash Spawn 2.jpg", "File:DB-creature-Ash Spawn 2.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Ash Spawn 02.jpg", "File:DB-creature-Ash Spawn 02.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Ash Hopper.jpg", "File:DB-creature-Ash Hopper.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Ash Guardian.jpg", "File:DB-creature-Ash Guardian.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Albino Spider.jpg", "File:DB-creature-Albino Spider.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Creature-Ahzidal.jpg", "File:DB-creature-Ahzidal.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Cover-Dragonborn Box Art.jpg", "File:DB-cover-Dragonborn Box Art.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Book-Stalhrim Source Map.png", "File:DB-book-Stalhrim Source Map.png"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Book-Spider Experiment Notes.png", "File:DB-book-Spider Experiment Notes.png"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Book-GratianSword.png", "File:DB-book-GratianSword.png"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Book-GratianSlash.png", "File:DB-book-GratianSlash.png"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Book-GratianDoor.png", "File:DB-book-GratianDoor.png"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Book-GratianBoat.png", "File:DB-book-GratianBoat.png"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Book-Fahlbtharzjournal03.png", "File:DB-book-Fahlbtharzjournal03.png"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Book-Fahlbtharzjournal02.png", "File:DB-book-Fahlbtharzjournal02.png"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Book-Fahlbtharzjournal.png", "File:DB-book-Fahlbtharzjournal.png"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Banner-Trader.png", "File:DB-banner-Trader.png"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Banner-The New Temple.png", "File:DB-banner-The New Temple.png"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Banner-House Telvanni.png", "File:DB-banner-House Telvanni.png"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Banner-House Redoran.png", "File:DB-banner-House Redoran.png"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Banner-Apothecary.png", "File:DB-banner-Apothecary.png"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Activity-Staff Enchanter.jpg", "File:DB-activity-Staff Enchanter.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Activity-Mining (Stalhrim).jpg", "File:DB-activity-Mining (Stalhrim).jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Activity-Mining (Stalhrim (depleted)).jpg", "File:DB-activity-Mining (Stalhrim (depleted)).jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Activity-Imbuing Chamber.jpg", "File:DB-activity-Imbuing Chamber.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Activity-Dragon Riding.jpg", "File:DB-activity-Dragon Riding.jpg"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Achievement-The Temple of Miraak.png", "File:DB-achievement-The Temple of Miraak.png"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Achievement-The Path of Knowledge.png", "File:DB-achievement-The Path of Knowledge.png"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Achievement-Stalhrim Crafter.png", "File:DB-achievement-Stalhrim Crafter.png"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Achievement-Solstheim Explorer.png", "File:DB-achievement-Solstheim Explorer.png"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Achievement-Raven Rock Owner.png", "File:DB-achievement-Raven Rock Owner.png"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Achievement-Outlander.png", "File:DB-achievement-Outlander.png"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Achievement-Hidden Knowledge.png", "File:DB-achievement-Hidden Knowledge.png"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Achievement-Dragonrider.png", "File:DB-achievement-Dragonrider.png"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Achievement-Dragon Aspect.png", "File:DB-achievement-Dragon Aspect.png"));
			this.Replacements.Add(new Replacement(this.Site, "File:DB-Achievement-At the Summit of Apocrypha.png", "File:DB-achievement-At the Summit of Apocrypha.png"));
		}
		#endregion
	}
}