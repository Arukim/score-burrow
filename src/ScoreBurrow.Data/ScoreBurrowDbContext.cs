using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ScoreBurrow.Data.Configurations;
using ScoreBurrow.Data.Entities;

namespace ScoreBurrow.Data;

public class ScoreBurrowDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly string? _currentUserId;

    public ScoreBurrowDbContext(DbContextOptions<ScoreBurrowDbContext> options, string? currentUserId = null)
        : base(options)
    {
        _currentUserId = currentUserId;
    }

    public DbSet<League> Leagues => Set<League>();
    public DbSet<LeagueMembership> LeagueMemberships => Set<LeagueMembership>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<GameParticipant> GameParticipants => Set<GameParticipant>();
    public DbSet<Town> Towns => Set<Town>();
    public DbSet<Hero> Heroes => Set<Hero>();
    public DbSet<PlayerStatistics> PlayerStatistics => Set<PlayerStatistics>();
    public DbSet<RatingHistory> RatingHistory => Set<RatingHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations
        modelBuilder.ApplyConfiguration(new LeagueConfiguration());
        modelBuilder.ApplyConfiguration(new LeagueMembershipConfiguration());
        modelBuilder.ApplyConfiguration(new GameConfiguration());
        modelBuilder.ApplyConfiguration(new GameParticipantConfiguration());
        modelBuilder.ApplyConfiguration(new TownConfiguration());
        modelBuilder.ApplyConfiguration(new HeroConfiguration());
        modelBuilder.ApplyConfiguration(new PlayerStatisticsConfiguration());
        modelBuilder.ApplyConfiguration(new RatingHistoryConfiguration());

        // Seed Towns data
        SeedTowns(modelBuilder);
        
        // Seed Heroes data
        SeedHeroes(modelBuilder);
    }

    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries<IAuditableEntity>();
        var now = DateTime.UtcNow;
        var userId = _currentUserId ?? "System";

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedBy = userId;
                entry.Entity.CreatedOn = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedBy = userId;
                entry.Entity.ModifiedOn = now;
            }
        }
    }

    private void SeedTowns(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Town>().HasData(
            new Town { Id = 1, Name = "Castle", Description = "The home of Knights and Clerics" },
            new Town { Id = 2, Name = "Rampart", Description = "The home of Rangers and Druids" },
            new Town { Id = 3, Name = "Tower", Description = "The home of Alchemists and Wizards" },
            new Town { Id = 4, Name = "Inferno", Description = "The home of Demoniac and Heretics" },
            new Town { Id = 5, Name = "Necropolis", Description = "The home of Death Knights and Necromancers" },
            new Town { Id = 6, Name = "Dungeon", Description = "The home of Overlords and Warlocks" },
            new Town { Id = 7, Name = "Stronghold", Description = "The home of Barbarians and Battle Mages" },
            new Town { Id = 8, Name = "Fortress", Description = "The home of Beastmasters and Witches" },
            new Town { Id = 9, Name = "Conflux", Description = "The home of Planeswalkers and Elementalists" },
            new Town { Id = 10, Name = "Cove", Description = "The home of Captains and Navigators" },
            new Town { Id = 11, Name = "Factory", Description = "The home of Artificiers and Mercenaries" }
        );
    }

    private void SeedHeroes(ModelBuilder modelBuilder)
    {
        var heroes = new List<Hero>
        {
            // Castle Heroes (1-16)
            new Hero { Id = 1, Name = "Orrin", TownId = 1, HeroClass = "Knight" },
            new Hero { Id = 2, Name = "Valeska", TownId = 1, HeroClass = "Knight" },
            new Hero { Id = 3, Name = "Edric", TownId = 1, HeroClass = "Knight" },
            new Hero { Id = 4, Name = "Sylvia", TownId = 1, HeroClass = "Knight" },
            new Hero { Id = 5, Name = "Lord Haart", TownId = 1, HeroClass = "Knight" },
            new Hero { Id = 6, Name = "Sorsha", TownId = 1, HeroClass = "Knight" },
            new Hero { Id = 7, Name = "Christian", TownId = 1, HeroClass = "Knight" },
            new Hero { Id = 8, Name = "Tyris", TownId = 1, HeroClass = "Knight" },
            new Hero { Id = 9, Name = "Rion", TownId = 1, HeroClass = "Cleric" },
            new Hero { Id = 10, Name = "Adela", TownId = 1, HeroClass = "Cleric" },
            new Hero { Id = 11, Name = "Cuthbert", TownId = 1, HeroClass = "Cleric" },
            new Hero { Id = 12, Name = "Adelaide", TownId = 1, HeroClass = "Cleric" },
            new Hero { Id = 13, Name = "Ingham", TownId = 1, HeroClass = "Cleric" },
            new Hero { Id = 14, Name = "Sanya", TownId = 1, HeroClass = "Cleric" },
            new Hero { Id = 15, Name = "Loynis", TownId = 1, HeroClass = "Cleric" },
            new Hero { Id = 16, Name = "Caitlin", TownId = 1, HeroClass = "Cleric" },

            // Rampart Heroes (17-32)
            new Hero { Id = 17, Name = "Mephala", TownId = 2, HeroClass = "Ranger" },
            new Hero { Id = 18, Name = "Ufretin", TownId = 2, HeroClass = "Ranger" },
            new Hero { Id = 19, Name = "Jenova", TownId = 2, HeroClass = "Ranger" },
            new Hero { Id = 20, Name = "Ryland", TownId = 2, HeroClass = "Ranger" },
            new Hero { Id = 21, Name = "Thorgrim", TownId = 2, HeroClass = "Ranger" },
            new Hero { Id = 22, Name = "Ivor", TownId = 2, HeroClass = "Ranger" },
            new Hero { Id = 23, Name = "Clancy", TownId = 2, HeroClass = "Ranger" },
            new Hero { Id = 24, Name = "Kyrre", TownId = 2, HeroClass = "Ranger" },
            new Hero { Id = 25, Name = "Coronius", TownId = 2, HeroClass = "Druid" },
            new Hero { Id = 26, Name = "Uland", TownId = 2, HeroClass = "Druid" },
            new Hero { Id = 27, Name = "Elleshar", TownId = 2, HeroClass = "Druid" },
            new Hero { Id = 28, Name = "Gem", TownId = 2, HeroClass = "Druid" },
            new Hero { Id = 29, Name = "Malcom", TownId = 2, HeroClass = "Druid" },
            new Hero { Id = 30, Name = "Melodia", TownId = 2, HeroClass = "Druid" },
            new Hero { Id = 31, Name = "Alagar", TownId = 2, HeroClass = "Druid" },
            new Hero { Id = 32, Name = "Aeris", TownId = 2, HeroClass = "Druid" },

            // Tower Heroes (33-48)
            new Hero { Id = 33, Name = "Piquedram", TownId = 3, HeroClass = "Alchemist" },
            new Hero { Id = 34, Name = "Thane", TownId = 3, HeroClass = "Alchemist" },
            new Hero { Id = 35, Name = "Josephine", TownId = 3, HeroClass = "Alchemist" },
            new Hero { Id = 36, Name = "Neela", TownId = 3, HeroClass = "Alchemist" },
            new Hero { Id = 37, Name = "Torosar", TownId = 3, HeroClass = "Alchemist" },
            new Hero { Id = 38, Name = "Fafner", TownId = 3, HeroClass = "Alchemist" },
            new Hero { Id = 39, Name = "Rissa", TownId = 3, HeroClass = "Alchemist" },
            new Hero { Id = 40, Name = "Iona", TownId = 3, HeroClass = "Alchemist" },
            new Hero { Id = 41, Name = "Astral", TownId = 3, HeroClass = "Wizard" },
            new Hero { Id = 42, Name = "Halon", TownId = 3, HeroClass = "Wizard" },
            new Hero { Id = 43, Name = "Serena", TownId = 3, HeroClass = "Wizard" },
            new Hero { Id = 44, Name = "Daremyth", TownId = 3, HeroClass = "Wizard" },
            new Hero { Id = 45, Name = "Theodorus", TownId = 3, HeroClass = "Wizard" },
            new Hero { Id = 46, Name = "Solmyr", TownId = 3, HeroClass = "Wizard" },
            new Hero { Id = 47, Name = "Cyra", TownId = 3, HeroClass = "Wizard" },
            new Hero { Id = 48, Name = "Aine", TownId = 3, HeroClass = "Wizard" },

            // Inferno Heroes (49-64)
            new Hero { Id = 49, Name = "Fiona", TownId = 4, HeroClass = "Demoniac" },
            new Hero { Id = 50, Name = "Rashka", TownId = 4, HeroClass = "Demoniac" },
            new Hero { Id = 51, Name = "Marius", TownId = 4, HeroClass = "Demoniac" },
            new Hero { Id = 52, Name = "Ignatius", TownId = 4, HeroClass = "Demoniac" },
            new Hero { Id = 53, Name = "Octavia", TownId = 4, HeroClass = "Demoniac" },
            new Hero { Id = 54, Name = "Calh", TownId = 4, HeroClass = "Demoniac" },
            new Hero { Id = 55, Name = "Pyre", TownId = 4, HeroClass = "Demoniac" },
            new Hero { Id = 56, Name = "Nymus", TownId = 4, HeroClass = "Demoniac" },
            new Hero { Id = 57, Name = "Ayden", TownId = 4, HeroClass = "Heretic" },
            new Hero { Id = 58, Name = "Xyron", TownId = 4, HeroClass = "Heretic" },
            new Hero { Id = 59, Name = "Axsis", TownId = 4, HeroClass = "Heretic" },
            new Hero { Id = 60, Name = "Olema", TownId = 4, HeroClass = "Heretic" },
            new Hero { Id = 61, Name = "Calid", TownId = 4, HeroClass = "Heretic" },
            new Hero { Id = 62, Name = "Ash", TownId = 4, HeroClass = "Heretic" },
            new Hero { Id = 63, Name = "Zydar", TownId = 4, HeroClass = "Heretic" },
            new Hero { Id = 64, Name = "Xarfax", TownId = 4, HeroClass = "Heretic" },

            // Necropolis Heroes (65-80)
            new Hero { Id = 65, Name = "Straker", TownId = 5, HeroClass = "Death Knight" },
            new Hero { Id = 66, Name = "Vokial", TownId = 5, HeroClass = "Death Knight" },
            new Hero { Id = 67, Name = "Moandor", TownId = 5, HeroClass = "Death Knight" },
            new Hero { Id = 68, Name = "Charna", TownId = 5, HeroClass = "Death Knight" },
            new Hero { Id = 69, Name = "Tamika", TownId = 5, HeroClass = "Death Knight" },
            new Hero { Id = 70, Name = "Isra", TownId = 5, HeroClass = "Death Knight" },
            new Hero { Id = 71, Name = "Clavius", TownId = 5, HeroClass = "Death Knight" },
            new Hero { Id = 72, Name = "Galthran", TownId = 5, HeroClass = "Death Knight" },
            new Hero { Id = 73, Name = "Septienna", TownId = 5, HeroClass = "Necromancer" },
            new Hero { Id = 74, Name = "Aislinn", TownId = 5, HeroClass = "Necromancer" },
            new Hero { Id = 75, Name = "Sandro", TownId = 5, HeroClass = "Necromancer" },
            new Hero { Id = 76, Name = "Nimbus", TownId = 5, HeroClass = "Necromancer" },
            new Hero { Id = 77, Name = "Thant", TownId = 5, HeroClass = "Necromancer" },
            new Hero { Id = 78, Name = "Xsi", TownId = 5, HeroClass = "Necromancer" },
            new Hero { Id = 79, Name = "Vidomina", TownId = 5, HeroClass = "Necromancer" },
            new Hero { Id = 80, Name = "Nagash", TownId = 5, HeroClass = "Necromancer" },

            // Dungeon Heroes (81-96)
            new Hero { Id = 81, Name = "Lorelei", TownId = 6, HeroClass = "Overlord" },
            new Hero { Id = 82, Name = "Arlach", TownId = 6, HeroClass = "Overlord" },
            new Hero { Id = 83, Name = "Dace", TownId = 6, HeroClass = "Overlord" },
            new Hero { Id = 84, Name = "Ajit", TownId = 6, HeroClass = "Overlord" },
            new Hero { Id = 85, Name = "Damacon", TownId = 6, HeroClass = "Overlord" },
            new Hero { Id = 86, Name = "Gunnar", TownId = 6, HeroClass = "Overlord" },
            new Hero { Id = 87, Name = "Synca", TownId = 6, HeroClass = "Overlord" },
            new Hero { Id = 88, Name = "Shakti", TownId = 6, HeroClass = "Overlord" },
            new Hero { Id = 89, Name = "Alamar", TownId = 6, HeroClass = "Warlock" },
            new Hero { Id = 90, Name = "Jaegar", TownId = 6, HeroClass = "Warlock" },
            new Hero { Id = 91, Name = "Malekith", TownId = 6, HeroClass = "Warlock" },
            new Hero { Id = 92, Name = "Jeddite", TownId = 6, HeroClass = "Warlock" },
            new Hero { Id = 93, Name = "Geon", TownId = 6, HeroClass = "Warlock" },
            new Hero { Id = 94, Name = "Deemer", TownId = 6, HeroClass = "Warlock" },
            new Hero { Id = 95, Name = "Sephinroth", TownId = 6, HeroClass = "Warlock" },
            new Hero { Id = 96, Name = "Darkstorn", TownId = 6, HeroClass = "Warlock" },

            // Stronghold Heroes (97-112)
            new Hero { Id = 97, Name = "Yog", TownId = 7, HeroClass = "Barbarian" },
            new Hero { Id = 98, Name = "Gurnisson", TownId = 7, HeroClass = "Barbarian" },
            new Hero { Id = 99, Name = "Jabarkas", TownId = 7, HeroClass = "Barbarian" },
            new Hero { Id = 100, Name = "Shiva", TownId = 7, HeroClass = "Barbarian" },
            new Hero { Id = 101, Name = "Gretchin", TownId = 7, HeroClass = "Barbarian" },
            new Hero { Id = 102, Name = "Krellion", TownId = 7, HeroClass = "Barbarian" },
            new Hero { Id = 103, Name = "Crag Hack", TownId = 7, HeroClass = "Barbarian" },
            new Hero { Id = 104, Name = "Tyraxor", TownId = 7, HeroClass = "Barbarian" },
            new Hero { Id = 105, Name = "Gird", TownId = 7, HeroClass = "Battle Mage" },
            new Hero { Id = 106, Name = "Vey", TownId = 7, HeroClass = "Battle Mage" },
            new Hero { Id = 107, Name = "Dessa", TownId = 7, HeroClass = "Battle Mage" },
            new Hero { Id = 108, Name = "Terek", TownId = 7, HeroClass = "Battle Mage" },
            new Hero { Id = 109, Name = "Zubin", TownId = 7, HeroClass = "Battle Mage" },
            new Hero { Id = 110, Name = "Gundula", TownId = 7, HeroClass = "Battle Mage" },
            new Hero { Id = 111, Name = "Oris", TownId = 7, HeroClass = "Battle Mage" },
            new Hero { Id = 112, Name = "Saurug", TownId = 7, HeroClass = "Battle Mage" },

            // Fortress Heroes (113-128)
            new Hero { Id = 113, Name = "Bron", TownId = 8, HeroClass = "Beastmaster" },
            new Hero { Id = 114, Name = "Drakon", TownId = 8, HeroClass = "Beastmaster" },
            new Hero { Id = 115, Name = "Wystan", TownId = 8, HeroClass = "Beastmaster" },
            new Hero { Id = 116, Name = "Tazar", TownId = 8, HeroClass = "Beastmaster" },
            new Hero { Id = 117, Name = "Alkin", TownId = 8, HeroClass = "Beastmaster" },
            new Hero { Id = 118, Name = "Korbac", TownId = 8, HeroClass = "Beastmaster" },
            new Hero { Id = 119, Name = "Gerwulf", TownId = 8, HeroClass = "Beastmaster" },
            new Hero { Id = 120, Name = "Broghild", TownId = 8, HeroClass = "Beastmaster" },
            new Hero { Id = 121, Name = "Mirlanda", TownId = 8, HeroClass = "Witch" },
            new Hero { Id = 122, Name = "Rosic", TownId = 8, HeroClass = "Witch" },
            new Hero { Id = 123, Name = "Voy", TownId = 8, HeroClass = "Witch" },
            new Hero { Id = 124, Name = "Verdish", TownId = 8, HeroClass = "Witch" },
            new Hero { Id = 125, Name = "Merist", TownId = 8, HeroClass = "Witch" },
            new Hero { Id = 126, Name = "Styg", TownId = 8, HeroClass = "Witch" },
            new Hero { Id = 127, Name = "Andra", TownId = 8, HeroClass = "Witch" },
            new Hero { Id = 128, Name = "Tiva", TownId = 8, HeroClass = "Witch" },

            // Conflux Heroes (129-144)
            new Hero { Id = 129, Name = "Pasis", TownId = 9, HeroClass = "Planeswalker" },
            new Hero { Id = 130, Name = "Thunar", TownId = 9, HeroClass = "Planeswalker" },
            new Hero { Id = 131, Name = "Ignissa", TownId = 9, HeroClass = "Planeswalker" },
            new Hero { Id = 132, Name = "Lacus", TownId = 9, HeroClass = "Planeswalker" },
            new Hero { Id = 133, Name = "Monere", TownId = 9, HeroClass = "Planeswalker" },
            new Hero { Id = 134, Name = "Erdamon", TownId = 9, HeroClass = "Planeswalker" },
            new Hero { Id = 135, Name = "Fiur", TownId = 9, HeroClass = "Planeswalker" },
            new Hero { Id = 136, Name = "Kalt", TownId = 9, HeroClass = "Planeswalker" },
            new Hero { Id = 137, Name = "Luna", TownId = 9, HeroClass = "Elementalist" },
            new Hero { Id = 138, Name = "Brissa", TownId = 9, HeroClass = "Elementalist" },
            new Hero { Id = 139, Name = "Ciele", TownId = 9, HeroClass = "Elementalist" },
            new Hero { Id = 140, Name = "Labetha", TownId = 9, HeroClass = "Elementalist" },
            new Hero { Id = 141, Name = "Inteus", TownId = 9, HeroClass = "Elementalist" },
            new Hero { Id = 142, Name = "Aenain", TownId = 9, HeroClass = "Elementalist" },
            new Hero { Id = 143, Name = "Gelare", TownId = 9, HeroClass = "Elementalist" },
            new Hero { Id = 144, Name = "Grindan", TownId = 9, HeroClass = "Elementalist" },
            // HotA Heroes

            // Cove Captain Heroes
            new Hero { Id = 145, Name = "Corkes", TownId = 10, HeroClass = "Captain" },
            new Hero { Id = 146, Name = "Jeremy", TownId = 10, HeroClass = "Captain" },
            new Hero { Id = 147, Name = "Illor", TownId = 10, HeroClass = "Captain" },
            new Hero { Id = 148, Name = "Derek", TownId = 10, HeroClass = "Captain" },
            new Hero { Id = 149, Name = "Elmore", TownId = 10, HeroClass = "Captain" },
            new Hero { Id = 150, Name = "Leena", TownId = 10, HeroClass = "Captain" },
            new Hero { Id = 151, Name = "Anabel", TownId = 10, HeroClass = "Captain" },
            new Hero { Id = 152, Name = "Cassiopeia", TownId = 10, HeroClass = "Captain" },
            new Hero { Id = 153, Name = "Miriam", TownId = 10, HeroClass = "Captain" },
            new Hero { Id = 154, Name = "Tark", TownId = 10, HeroClass = "Captain" },

            // Cove Navigator Heroes
            new Hero { Id = 155, Name = "Manfred", TownId = 10, HeroClass = "Navigator" },
            new Hero { Id = 156, Name = "Zilare", TownId = 10, HeroClass = "Navigator" },
            new Hero { Id = 157, Name = "Astra", TownId = 10, HeroClass = "Navigator" },
            new Hero { Id = 158, Name = "Casmetra", TownId = 10, HeroClass = "Navigator" },
            new Hero { Id = 159, Name = "Dargem", TownId = 10, HeroClass = "Navigator" },
            new Hero { Id = 160, Name = "Andal", TownId = 10, HeroClass = "Navigator" },
            new Hero { Id = 161, Name = "Eovacius", TownId = 10, HeroClass = "Navigator" },
            new Hero { Id = 162, Name = "Spint", TownId = 10, HeroClass = "Navigator" },

            // Factory Mercenary Heroes
            new Hero { Id = 163, Name = "Henrietta", TownId = 11, HeroClass = "Mercenary" },
            new Hero { Id = 164, Name = "Sam", TownId = 11, HeroClass = "Mercenary" },
            new Hero { Id = 165, Name = "Tancred", TownId = 11, HeroClass = "Mercenary" },
            new Hero { Id = 166, Name = "Dury", TownId = 11, HeroClass = "Mercenary" },
            new Hero { Id = 167, Name = "Morton", TownId = 11, HeroClass = "Mercenary" },
            new Hero { Id = 168, Name = "Tavin", TownId = 11, HeroClass = "Mercenary" },
            new Hero { Id = 169, Name = "Murdoch", TownId = 11, HeroClass = "Mercenary" },

            // Factory Artificer Heroes
            new Hero { Id = 170, Name = "Melchior", TownId = 11, HeroClass = "Artificer" },
            new Hero { Id = 171, Name = "Floribert", TownId = 11, HeroClass = "Artificer" },
            new Hero { Id = 172, Name = "Wynona", TownId = 11, HeroClass = "Artificer" },
            new Hero { Id = 173, Name = "Todd", TownId = 11, HeroClass = "Artificer" },
            new Hero { Id = 174, Name = "Agar", TownId = 11, HeroClass = "Artificer" },
            new Hero { Id = 175, Name = "Bertram", TownId = 11, HeroClass = "Artificer" },
            new Hero { Id = 176, Name = "Wrathmont", TownId = 11, HeroClass = "Artificer" },
            new Hero { Id = 177, Name = "Ziph", TownId = 11, HeroClass = "Artificer" },
            new Hero { Id = 178, Name = "Victoria", TownId = 11, HeroClass = "Artificer" },
            new Hero { Id = 179, Name = "Eanswythe", TownId = 11, HeroClass = "Artificer" },
            new Hero { Id = 180, Name = "Frederick", TownId = 11, HeroClass = "Artificer" },
        };

        modelBuilder.Entity<Hero>().HasData(heroes);
    }
}
