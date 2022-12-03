using System;
using MCGalaxy.Events.PlayerEvents;

namespace MCGalaxy.Games
{
    public sealed partial class ParkourGame : RoundsGame
    {

        protected override void HookEventHandlers()
        {
            OnPlayerConnectEvent.Register(HandlePlayerConnect, Priority.High);
            OnPlayerMoveEvent.Register(HandlePlayerMove, Priority.High);
            OnJoinedLevelEvent.Register(HandleJoinedLevel, Priority.High);
            OnPlayerChatEvent.Register(HandlePlayerChat, Priority.High);
            OnPlayerDisconnectEvent.Register(HandlePlayerDisconnect, Priority.High);

            base.HookEventHandlers();
        }

        protected override void UnhookEventHandlers()
        {
            OnPlayerConnectEvent.Unregister(HandlePlayerConnect);
            OnPlayerDisconnectEvent.Unregister(HandlePlayerDisconnect);
            OnPlayerMoveEvent.Unregister(HandlePlayerMove);
            OnJoinedLevelEvent.Unregister(HandleJoinedLevel);
            OnPlayerChatEvent.Unregister(HandlePlayerChat);

            base.UnhookEventHandlers();
        }

        void HandlePlayerConnect(Player p)
        {
            if (GetConfig().SetMainLevel) return;
            if (!RoundInProgress || p.level != Map) return;
            p.Extras["stopwatch"] = new Stopwatch(p);
            p.Extras["runrecorder"] = new RunRecorder(p);
        }

        void HandlePlayerMove(Player p, Position next, byte rotX, byte rotY, ref bool cancel)
        {
            if (!RoundInProgress || p.level != Map) return;

            // TODO: Maybe tidy this up?
            if (p.Game.Noclip == null) p.Game.Noclip = new NoclipDetector(p);
            if (p.Game.Speed == null) p.Game.Speed = new SpeedhackDetector(p);

            bool reverted = p.Game.Noclip.Detect(next) || p.Game.Speed.Detect(next, Config.MaxMoveDist);
            if (reverted) cancel = true;
        }

        void HandleJoinedLevel(Player p, Level prevLevel, Level level, ref bool announce)
        {
            HandleJoinedCommon(p, prevLevel, level, ref announce);

            p.SetPrefix(); // TODO: Kinda hacky, not sure if needed 
            if (level != Map)
            {
                PlayerLeftGame(p);
                return;
            }

            if (!ParkourGame.Instance.Running)
            {
                ParkourGame.Instance.Picker.SendVoteMessage(p);
            }

            p.SetPrefix();
            p.Extras["stopwatch"] = new Stopwatch(p);
            p.Extras["runrecorder"] = new RunRecorder(p);

            if (RoundInProgress)
            {
                ReplaceGates(Block.Air);
            } else
            {
                ReplaceGates(Block.Glass);
            }

            double startLeft = (RoundStart - DateTime.UtcNow).TotalSeconds;
            if (startLeft >= 0)
            {
                p.Message("&a{0} &Sseconds left until the round starts!", (int)startLeft);
            }

            MessageMapInfo(p);

            // Finally, CmdAKA-based trick to prevent weird spawning problems
            foreach (Player other in GetPlayers())
            {
                if (other.level != p.level || p == other || !p.CanSeeEntity(other)) continue;

                ParkourData data_ = ParkourGame.Get(p);

                Entities.Despawn(p, other);
                Entities.Spawn(p, other);
                data_.VisibilityToggled = false;
            }
            TabList.Add(p, p, Entities.SelfID);
        }

        void HandlePlayerChat(Player p, string message)
        {
            if (p.level != Map || message.Length <= 1) return;

            if (message[0] == '-')
            {
                if (p.Game.Team == null)
                {
                    p.Message("You are not on a team, so cannot send a team message.");
                }
                else
                {
                    p.Game.Team.Message(p, message.Substring(1));
                }
                p.cancelchat = true;
            }
            if (message == ".")
            {
                p.HandleCommand("TimeLeft", null, new CommandData());
            }
        }

        void HandlePlayerDisconnect(Player p, string message)
        {
            PlayerLeftGame(p);
        }

        protected void MessageMapInfo(Player p)
        {
            float rating = 0.0f;
            if (Map.Config.Likes + Map.Config.Dislikes != 0)    // if it has been rated
            {
                rating = 5 * (float)Map.Config.Likes / ((float)Map.Config.Likes + (float)Map.Config.Dislikes);
            }
            p.Message(String.Format("This map has an average rating of &a{0:0.00} out of 5 stars", rating));

            string[] authors = Map.Config.Authors.SplitComma();
            if (authors.Length == 0) return;

            p.Message("It was created by {0}", authors.Join(n => p.FormatNick(n)));
        }
    }
}
