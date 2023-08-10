using System;
using System.Collections.Generic;
using System.Data;
using MCGalaxy.DB;
using MCGalaxy.SQL;

namespace MCGalaxy.Games
{

    public sealed partial class ParkourGame : RoundsGame
    {
        struct ParkourStats { public int TotalRounds, TotalRoundsFinished, MaxRoundsFinished, BronzeMedals, SilverMedals, GoldMedals, AuthorTimes; }

        static OfflineStatPrinter offlineParkourStats;
        static OnlineStatPrinter onlineParkourStats;
        static ChatToken finishedToken;

        static List<DBTopStat> parkourStats = new List<DBTopStat>();

        static bool AlreadyHooked => TopStat.Find("TotalRoundsPlayed") != null;

        static void HookStats()
        {
            if (AlreadyHooked) return;

            parkourStats.Add(new DBTopStat(
                identifier: "TotalRoundsPlayed",
                title: "Total rounds finished",
                table: "ParkourStats",
                column: "TotalRounds",
                formatter: TopStat.FormatInteger));

            parkourStats.Add(new DBTopStat(
                identifier: "TotalRoundsFinished",
                title: "Total rounds finished",
                table: "ParkourStats",
                column: "TotalRoundsFinished",
                formatter: TopStat.FormatInteger));

            parkourStats.Add(new DBTopStat(
                identifier: "ConsecutiveMaxFinished",
                title: "Most consecutive rounds finished",
                table: "ParkourStats",
                column: "MaxRoundsFinished",
                formatter: TopStat.FormatInteger));

            parkourStats.Add(new DBTopStat(
                identifier: "BronzeMedals",
                title: "Number of bronze medals",
                table: "ParkourStats",
                column: "BronzeMedals",
                formatter: TopStat.FormatInteger));

            parkourStats.Add(new DBTopStat(
                identifier: "SilverMedals",
                title: "Number of silver medals",
                table: "ParkourStats",
                column: "SilverMedals",
                formatter: TopStat.FormatInteger));

            parkourStats.Add(new DBTopStat(
                identifier: "GoldMedals",
                title: "Number of gold medals",
                table: "ParkourStats",
                column: "GoldMedals",
                formatter: TopStat.FormatInteger));

            parkourStats.Add(new DBTopStat(
                identifier: "AuthorTimes",
                title: "Number of author times beaten",
                table: "ParkourStats",
                column: "AuthorTimes",
                formatter: TopStat.FormatInteger));

            finishedToken = new ChatToken("$finished", "Total number of rounds finished", p => Get(p).TotalRoundsFinished.ToString());

            offlineParkourStats = PrintOfflineParkourStats;
            onlineParkourStats = PrintOnlineParkourStats;
            OfflineStat.Stats.Add(offlineParkourStats);
            OnlineStat.Stats.Add(onlineParkourStats);
            ChatTokens.Standard.Add(finishedToken);

            foreach (TopStat stat in parkourStats) {
                TopStat.Register(stat);
	        }
        }

        static void UnhookStats()
        {
            OfflineStat.Stats.Remove(offlineParkourStats);
            OnlineStat.Stats.Remove(onlineParkourStats);
            ChatTokens.Standard.Remove(finishedToken);

            foreach (TopStat stat in parkourStats) {
                TopStat.Unregister(stat);
	        }

            parkourStats.Clear();
        }

        // Don't know why MCGalaxy defaults to distinguishing online and offline stats?
        static void PrintOnlineParkourStats(Player p, Player who)
        {
            ParkourData data = Get(who);
            ParkourStats stats = LoadStats(who.name);
            PrintParkourStats(p, data.TotalRounds, data.TotalRoundsFinished, data.MaxRoundsFinished, stats.BronzeMedals, stats.SilverMedals, stats.GoldMedals, stats.AuthorTimes);
        }

        static void PrintOfflineParkourStats(Player p, PlayerData who)
        {
            ParkourStats stats = LoadStats(who.Name);
            PrintParkourStats(p, stats.TotalRounds, stats.TotalRoundsFinished, stats.MaxRoundsFinished, stats.BronzeMedals, stats.SilverMedals, stats.GoldMedals, stats.AuthorTimes);
        }

        static void PrintParkourStats(Player p, int totalRounds, int totalFinished, int maxFinished, int bronzes, int silvers, int golds, int authorTimes)
        {
            p.Message("Played a total of &a{0} &Srounds, beating &a{1}&S author times", totalRounds, authorTimes);
            p.Message("Finished &a{0} &Srounds (max &e{1}&S)", totalFinished, maxFinished);
            p.Message("{0} bronze medals, {1} silver medals, {2} gold medals", bronzes, silvers, golds);
        }


        static ColumnDesc[] parkourTable = new ColumnDesc[] {
            new ColumnDesc("ID", ColumnType.Integer, priKey: true, autoInc: true, notNull: true),
            new ColumnDesc("Name", ColumnType.Char, 20),
            new ColumnDesc("TotalRounds", ColumnType.Int32),
            new ColumnDesc("TotalRoundsFinished", ColumnType.Int32),
            new ColumnDesc("MaxRoundsFinished", ColumnType.Int32),
            new ColumnDesc("BronzeMedals", ColumnType.Int32),
            new ColumnDesc("SilverMedals", ColumnType.Int32),
            new ColumnDesc("GoldMedals", ColumnType.Int32),
            new ColumnDesc("AuthorTimes", ColumnType.Int32),
        };

        static ParkourStats ParseStats(ISqlRecord record)
        {
            ParkourStats stats;
            stats.TotalRounds = record.GetInt("TotalRounds");
            stats.TotalRoundsFinished = record.GetInt("TotalRoundsFinished");
            stats.MaxRoundsFinished = record.GetInt("MaxRoundsFinished");
            stats.BronzeMedals = record.GetInt("BronzeMedals");
            stats.SilverMedals = record.GetInt("SilverMedals");
            stats.GoldMedals = record.GetInt("GoldMedals");
            stats.AuthorTimes = record.GetInt("AuthorTimes");

            return stats;
        }

        static ParkourStats LoadStats(string name)
        {
            ParkourStats stats = default(ParkourStats);
            Database.ReadRows("ParkourStats", "*", record => stats = ParseStats(record),
                "WHERE Name=@0", name);
            return stats;
        }

        protected override void SaveStats(Player p)
        {
            ParkourData data = TryGet(p);

            if (data == null || (data.TotalRoundsFinished == 0)) return;

            int count = Database.CountRows("ParkourStats", "WHERE Name=@0", p.name);
            if (count == 0)
            {
                Database.AddRow("ParkourStats", "TotalRounds, TotalRoundsFinished, MaxRoundsFinished, BronzeMedals, SilverMedals, GoldMedals, AuthorTimes, Name",
                                data.TotalRounds, data.TotalRoundsFinished, data.MaxRoundsFinished, data.BronzeMedals, data.SilverMedals, data.GoldMedals, data.AuthorTimes, p.name);
            }
            else
            {
                Database.UpdateRows("ParkourStats", "TotalRounds=@0, TotalRoundsFinished=@1, MaxRoundsFinished=@2, BronzeMedals=@3, SilverMedals=@4, GoldMedals=@5, AuthorTimes=@6",
                                    "WHERE Name=@7", data.TotalRounds, data.TotalRoundsFinished, data.MaxRoundsFinished, data.BronzeMedals, data.SilverMedals, data.GoldMedals, data.AuthorTimes, p.name);
            }
        }
    }
}
