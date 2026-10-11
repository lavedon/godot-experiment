using Godot;

/// <summary>Jump-dust encounter checks, kept separate from the hero/looks tests.</summary>
public partial class Main
{
    async Task JumpDustComesFromInput()
    {
        StartGame();
        QuietRoad();
        await Press("jump");
        await Release("jump");
        Check(jumpDust.Count == 0, "ordinary running keeps the usual jump puff without boss bait");

        GoToTitle();
        var vroomba = StartVroombaFight("TDustInput");
        vroomba.SetProcess(false); // leave him entering while we check the jump controls
        await Press("jump");
        await Release("jump");
        Check(jumpDust.Count == 1, "an accepted ground jump leaves one dust cloud");
        float takeoff = jumpDust[0].Position.X;
        testMoveAxis = 1;
        await Press("jump");
        await Release("jump");
        Check(jumpDust.Count == 2, "the double jump also leaves dust in the air");
        await Press("jump");
        await Release("jump");
        Check(jumpDust.Count == 2, "a rejected third jump leaves no dust");
        await Wait(0.4);
        testMoveAxis = 0;
        Check(Mathf.IsEqualApprox(jumpDust[0].Position.X, takeoff) && robot.Position.X > takeoff + 100,
              "dust stays at takeoff while the hero rides away");
        await Wait(1);
        Check(jumpDust.All(dust => Mathf.IsEqualApprox(dust.Position.Y, GroundY - 12)), "airborne dust settles on the floor");
        await Wait(JumpDust.Lifetime);
        Check(jumpDust.Count == 0, "unused dust expires even though the arena is stopped");

        for (int i = 0; i < MaxJumpDust + 3; i++)
        {
            robot.Reset();
            await Press("jump");
            await Release("jump");
        }
        Check(jumpDust.Count == MaxJumpDust, "the trail has a fixed size limit");
        GoToTitle();
        Check(jumpDust.Count == 0, "returning to the menu clears every puff");
        StartBossFight("TDustRusty");
        await Press("jump");
        await Release("jump");
        Check(jumpDust.Count == 0, "Big Rusty's fight is unchanged");
    }

    async Task JumpDustLuresAndClogs()
    {
        for (int phase = 1; phase <= 3; phase++)
        {
            GoToTitle();
            var vroomba = StartVroombaFight("TDustLure" + phase);
            Check(await WaitUntil(() => vroomba.CurrentMove == "Revving", 4), "VROOMBA is ready to attack");
            if (phase > 1)
            {
                vroomba.LoseHealth((phase - 1) * 2);
                Check(await WaitUntil(() => vroomba.CurrentMove == "Spitting", 4), "the angrier phase starts its attack");
            }
            // Exercise the lure from either side and from both revving and sucking.
            bool fromLeft = phase == 2;
            vroomba.Position = new Vector2(fromLeft ? 175 : 1105, GroundY);
            robot.Position = new Vector2(fromLeft ? 850 : 430, GroundY);
            if (phase == 3) vroomba.DoAttack(VroombaAttack.Suck);
            int health = vroomba.Health;
            int clogs = 0;
            vroomba.DustClogged += () => clogs++;
            await Press("jump");
            Check(await WaitUntil(() => vroomba.ChasingDust, 0.2), $"phase {phase}: jump dust distracts him");
            var first = jumpDust[0];
            float startX = vroomba.Position.X;
            robot.Position = new Vector2(fromLeft ? 350 : 850, robot.Position.Y);
            await Release("jump");
            await Press("jump");
            await Release("jump");
            await Wait(0.55);
            Check((vroomba.Position.X - startX) * (fromLeft ? 1 : -1) > 40,
                  "he follows the first puff instead of switching to the moving hero or a newer puff");
            Check(vroomba.WindDrawn && sounds.IsPlaying(Sfx.VacuumHum), "the dust chase draws wind and hums");
            Check(await WaitUntil(() => vroomba.CleaningDust, 4), "he reaches the puff and vacuums it");
            Check(!vroomba.IsDangerous && !vroomba.CanBeStomped, "cleaning is harmless; the stomp window starts when clogged");
            Check(await WaitUntil(() => vroomba.CanBeStomped, 1.2), "vacuuming dust clogs him and opens a stomp window");
            Check(first.Consumed && clogs == 1 && vroomba.Health == health, "dust is eaten once and does not deal automatic damage");
            Check(!sounds.IsPlaying(Sfx.VacuumHum) && popups.Texts.Contains("CLOGGED! STOMP!"), "the vacuum stops and the stomp cue appears");
            Check(!vroomba.CanChaseDust, "more dust cannot steal the dizzy window");
            int scoreBefore = bonusPoints;
            Check(await StompFromAbove(vroomba), "the hero can actually land a stomp after luring him");
            Check(vroomba.Health == health - 1 && bonusPoints == scoreBefore + PointsPerBossHit,
                  "the stomp uses normal boss damage and scoring");
            Check(!vroomba.CanChaseDust && clogs == 1, "the lure cooldown prevents an immediate second clog");
        }
    }

    async Task JumpDustCannotTrapBoss()
    {
        var vroomba = StartVroombaFight("TDustGuards");
        await Press("jump");
        await Release("jump");
        Check(!vroomba.ChasingDust && vroomba.CurrentMove == "Entering", "dust never interrupts his entrance");
        Check(await WaitUntil(() => vroomba.ChasingDust, 4), "he notices the waiting dust when ready");
        jumpDust[0].Update(JumpDust.Lifetime); // target vanishes while he's following it
        await Wait(0.1);
        Check(!vroomba.ChasingDust && !vroomba.CleaningDust && !sounds.IsPlaying(Sfx.VacuumHum), "expired bait releases his target and switches off the vacuum");
        Check(await WaitUntil(() => vroomba.CurrentMove == "Zooming", 2), "he resumes his ordinary attack after losing the target");
        await WaitUntil(() => robot.OnGround, 2);
        await Press("jump");
        await Release("jump");
        Check(!vroomba.ChasingDust, "a new puff cannot redirect a committed zoom");
        Check(await WaitUntil(() => vroomba.CanBeStomped, 3), "the zoom still bonks and gives its normal stomp chance");
        Check(!vroomba.ChaseDust(jumpDust[0]) && vroomba.CanBeStomped, "a normal dizzy window cannot be interrupted by dust");
        // Remove all bait, then check that attacks continue without needing another player action.
        ClearJumpDust();
        Check(await WaitUntil(() => vroomba.Sucking, 5), "without a stomp or more dust he recovers and continues his plan");

        await WaitUntil(() => robot.OnGround, 2);
        await Press("jump");
        await Release("jump");
        Check(await WaitUntil(() => vroomba.ChasingDust, 1), "a fresh lure can interrupt his regular vacuum");
        StartRebuild();
        await Wait(0.2);
        Check(jumpDust.Count == 0 && !sounds.IsPlaying(Sfx.VacuumHum), "a crash clears the bait and stops the dust vacuum");
        Check(await WaitUntil(() => state == GameState.Playing, 5), "the hero rebuilds normally");
        Check(!vroomba.ChasingDust && !vroomba.CanChaseDust, "dust cannot bypass the rebuild grace period");

        // A new encounter must not inherit either targets or cooldowns from the old one.
        GoToTitle();
        vroomba = StartVroombaFight("TDustRestart");
        Check(jumpDust.Count == 0, "restarting clears the previous encounter");
        await Press("jump");
        await Release("jump");
        Check(await WaitUntil(() => vroomba.ChasingDust, 4), "the new boss can notice fresh dust");
        vroomba.LoseHealth(BossBase.MaxHealth);
        await Wait(0.1);
        Check(jumpDust.Count == 0 && !sounds.IsPlaying(Sfx.VacuumHum), "defeat clears dust and its looping sound");

        GoToTitle();
        vroomba = StartVroombaFight("TDustGameOver");
        await Press("jump");
        await Release("jump");
        Check(await WaitUntil(() => vroomba.ChasingDust, 4), "dust chase starts before game over");
        GameOver();
        await Wait(0.1);
        Check(jumpDust.Count == 0 && vroomba.CurrentMove == "FlyingAway" && !sounds.IsPlaying(Sfx.VacuumHum),
              "game over clears dust and lets VROOMBA leave");
    }

    async Task JumpDustAtArenaEdges()
    {
        // Dust under his body near either wall needs him to turn his mouth toward the wall to reach it.
        foreach (float x in new[] { Robot.LeftEdge, 180f, 1100f, Robot.RightEdge })
        {
            GoToTitle();
            var vroomba = StartVroombaFight("TDustEdge");
            await WaitUntil(() => vroomba.CurrentMove == "Revving", 4);
            vroomba.Position = new Vector2(x < 640 ? Vroomba.LeftSpot : Vroomba.RightSpot, GroundY);
            robot.Position = new Vector2(x, GroundY);
            vroomba.DoAttack(VroombaAttack.Suck); // capture the test position instead of revving at his old spot
            await Press("jump");
            await Release("jump");
            robot.Position = new Vector2(640, robot.Position.Y);
            bool clogged = await WaitUntil(() => vroomba.CanBeStomped, 3);
            Check(clogged, $"dust near the wall at x={x} can be reached and clogs him ({vroomba.CurrentMove}, x={vroomba.Position.X:0})");
            Check(vroomba.MovesDrawn.Contains("DustVacuum") && vroomba.Position.X >= Vroomba.HalfWidth
                  && vroomba.Position.X <= ScreenWidth - Vroomba.HalfWidth, "he cleans it while staying inside the arena");
        }
    }
}
