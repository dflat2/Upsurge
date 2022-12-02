/*

// Add the systems.data.SQLite dll as a reference if System.Data.SQLite isn't showing up

using MCGalaxy.Games;
using System.Data.SQLite;
using System.IO;
using System.Text;
using System.Threading;

namespace MCGalaxy
{
    public sealed class CmdToggleVisibility : Command2
    {
        static string mapBuildPath = @"/home/opapinguin/MapBuild";        // Note: for Windows you use forward slashes instead! Also needs escaping then
        static string mainServerPath = @"/home/opapinguin/Opa";

        public override string name { get { return "ExportToParkour"; } }
        public override string type { get { return CommandTypes.Moderation; } }
        public override string shortcut { get { return "ETP"; } }
        public override bool museumUsable { get { return false; } }
        public override bool SuperUseable { get { return false; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }

        public override void Use(Player p, string message)
        {
            if (p.Extras.Contains("exportingtoparkour"))
            {
                ExportHandler(p, message, (ushort)p.Extras["exportingtoparkour"]);
            }
            else
            {
                ExportHandler(p, message, 0);
            }
        }

        private void ExportHandler(Player p, string message, ushort exportCode)
        {
            string name = "";
            string authors = "";
            string motd = "";
            string roundtime = "";
            switch (exportCode)
            {
                case 0: // command set at start
                    p.Message("What name do you want to give this map?");
                    p.Extras["exportingtoparkour"] = 1;
                    break;
                case 1:
                    name = message;

                    p.Message("Who are the authors of this map?");
                    p.Extras["exportingtoparkour"] = 2;
                    break;
                case 2:
                    authors = message;

                    p.Message("What motd do you want to give this map?");
                    p.Extras["exportingtoparkour"] = 3;
                    break;

                case 3:
                    motd = message;

                    p.Message("What is the roundtime of this map?");
                    p.Extras["exportingtoparkour"] = 4;
                    break;

                case 4:
                    roundtime = message;

                    p.Message("Now exporting to parkour...");
                    p.Extras["exportingtoparkour"] = 0;
                    break;

                default:
                    break;
            }
        }

        private void export(Player p, string name, string authors, string motd, string roundtime)
        {
            string map = p.level.MapName;

            string SourceConnectionString = string.Format("Data Source={0}/MCGalaxy.db;Version=3;UseUTF16Encoding=True;", mapBuildPath);

            StringBuilder strSql = new StringBuilder();
            strSql.Append(string.Format("ATTACH '{0}/MCGalaxy.db' AS DestDB;", mainServerPath));

            // Now move level info in the database
            using (SQLiteConnection con = new SQLiteConnection(SourceConnectionString))
            {
                con.Open();

                using (SQLiteCommand cmd = new SQLiteCommand(con))
                {
                    strSql.Append(string.Format("DROP TABLE IF EXISTS DestDB.Messages{0};", name));
                    strSql.Append(string.Format("DROP TABLE IF EXISTS DestDB.Portals{0};", name));

                    if (MCGalaxy.SQL.Database.TableExists(string.Format("Messages{0}", map)))
                    {
                        strSql.Append(string.Format("CREATE TABLE DestDB.Messages{0} (X SMALLINT UNSIGNED, Y SMALLINT UNSIGNED, Z SMALLINT UNSIGNED, Message CHAR(255));", name));
                        strSql.Append(string.Format("INSERT INTO DestDB.Messages{0} SELECT * FROM Messages{0};", name));
                    }

                    if (MCGalaxy.SQL.Database.TableExists(string.Format("Portals{0}", map)))
                    {
                        strSql.Append(string.Format("CREATE TABLE DestDB.Portals{0} (EntryX SMALLINT UNSIGNED, EntryY SMALLINT UNSIGNED, EntryZ SMALLINT UNSIGNED, ExitMap CHAR(20), ExitX SMALLINT UNSIGNED, ExitY SMALLINT UNSIGNED, ExitZ SMALLINT UNSIGNED);", name));
                        strSql.Append(string.Format("INSERT INTO DestDB.Portals{0} SELECT * FROM Portals{0};", name));
                    }

                    cmd.CommandText = strSql.ToString();
                    int i = cmd.ExecuteNonQuery();
                    if (i > 0)
                    {
                        p.Message("Data dumped successfully ...!!!");
                    }
                    else
                    {
                        p.Message("Problem dumping data. See logs for more information.");
                    }
                }
                con.Close();
            }

            // Now move additional level info outside of the SQL database
            try
            {
                File.Copy(mapBuildPath + @"/levels/" + p.level.MapName + ".lvl", mainServerPath + @"/levels/" + p.Level.MapName + ".lvl", true);
                if (File.Exists(mapBuildPath + @"/levels/level properties/" + p.level.MapName + ".properties"))
                {
                    File.Copy(mapBuildPath + @"/levels/level properties/" + p.level.MapName + ".properties", mainServerPath + @"/levels/level properties/" + name + ".properties", true);
                }
                if (File.Exists(mapBuildPath + @"/blockdb/" + p.level.MapName + ".cbdb"))
                {
                    File.Copy(mapBuildPath + @"/blockdb/" + p.level.MapName + ".cbdb", mainServerPath + @"/blockdb/" + name + ".cbdb", true);
                }
                if (File.Exists(mapBuildPath + @"/blockdefs/lvl_" + p.level.MapName + ".json"))
                {
                    File.Copy(mapBuildPath + @"/blockdefs/lvl_" + p.level.MapName + ".json", mainServerPath + @"/blockdefs/lvl_" + name + ".json", true);
                }
                if (File.Exists(mapBuildPath + @"/blockprops/_" + p.level.MapName + ".txt"))
                {
                    File.Copy(mapBuildPath + @"/blockprops/_" + p.level.MapName + ".txt", mainServerPath + @"/blockprops/_" + name + ".txt", true);
                }
                if (File.Exists(mapBuildPath + @"/extra/bots/" + p.level.MapName + ".json"))
                {
                    File.Copy(mapBuildPath + @"/extra/bots/" + p.level.MapName + ".json", mainServerPath + @"/extra/bots/" + name + ".json", true);
                }
            }
            catch (IOException e)
            {
                Logger.Log(LogType.Error, e.StackTrace);
            }

            // Now add to the parkour maps


            // Now set the roundtime

        }
        public override void Help(Player p)
        {
            p.Message("Exports the map you're currently on to the parkour server, along with all the special blocks.");
            p.Message("/ExportToParkour or /ETP");
        }
    }
}


*/