# MissilePrediction

A library mod for [Phantom Brigade (Experimental)](https://braceyourselfgames.com/phantom-brigade/) that shows missile trajectories in the planning phase.

It is compatible with game version **1.0.5-5828E_Steam**. That is an **EXPERIMENTAL** release. All library mods are fragile and susceptible to breakage whenever a new version is released.

This mod is a proof-of-concept to show missile trajectories in the planning phase just as unit movement is shown. The code is a work in progress with at least the following known issues:

- It is fragile and crashes fairly often
- It is unoptimized and severely drags down performance
- It causes the timeline to do weird things while it works out the trajectories
- The graphics are placeholders
- Glitches happen where missiles will shoot sideways or disappear
- It doesn't play nicely with other mods

I'm not sure that showing missile trajectories in the planning phase is a good idea for two reasons.

1. It gets confusing when there are lot of missiles in-flight. There may be ways to mitigate this, perhaps by adding some UI to show/hide missiles per unit.
2. It probably destroys the game balance established by the game designers.

To give an idea of what this mod does, here's a short clip from one of my battles. I have a unit that's being pursued by a pack of missiles and I'm able to time a dash around a hill so that the missiles get confused but don't have enough time to recover and turn around.

![planning a dash around hill to escape missiles](Media/evasion_planning.mp4)

Here's what it looks like in the execution phase. The missiles do indeed miss and crash into the ground. However, you'll notice that the prediction is not 100% accurate. The prediction shows all the missiles clearing the hilltop but during execution, a couple of missiles clip the hill.

![seeing the escape in execution](Media/evasion_execution.mp4)

This is a lengthy readme so feel free to jump to the section that interests you.

- [Background](#background) : why I started this mod
- [Samples and Simulation](#samples-and-simulation) : a look at how missiles are different than bullets and what that means for the code
- [Tinkering with the Timeline](#tinkering-with-the-timeline) : why the timeline seems to glitch out when missiles are launched in planning phase
- [Hit Detection, or Lack Thereof](#hit-detection) : why the predicted missiles go through buildings
- [Game Design](#game-design) : missiles are powerful and with great power comes great responsibility
- [Technical Notes](#technical-notes) : explanation of the technique I'm using to get the popups into replay

## Background

The planning phase does not show any projectiles. This is not much of a problem for ballistic and beam weapons because you can get a good-enough estimate from the projected line that's drawn when a unit fires a weapon of those types. Missiles are different. They are guided and can adjust their trajectories to match the movement of your units. A single enemy unit equipped with a missile launcher is a nuisance. The fun stops when every enemy unit has a missile launcher.

The problem is that you can't see the missiles so you can't really plan any evasive actions. Some missiles are slower than others, some turn faster, some stay alive longer. The core mechanic of the game is that you can see what the enemy units are doing and plan around their actions. Missiles toss that mechanic out the window.

I thought I'd take a look at what it would take in the code to make missiles work with the prediction mechanic and, more importantly, how the game would play when you can see how in-flight missiles are tracking your units.

## Samples and Simulation

The execution phase can be seen as a physics simulation where the time interval for computing the next step in the simulation is the duration of the frame. Ballistic projectiles like bullets have a relatively simple closed form time-based equation which allows for quick computation of the position and velocity of the bullet at any moment in time. Missiles form a dynamic system with their targets and the easiest way to solve that system is with an iterative algorithm which works nicely with the underlying physics simulation.

In the planning phase, the physics simulation is suspended. This isn't a problem for ballistic projectiles because the underlying equation can be solved for arbitrary time without resorting to the physics engine. It is a problem, however, for missiles which do use the physics engine to calculate their flight characteristics. One lucky break is that Phantom Brigade does not run the Unity physics engine in real-time. Instead, it explicitly turns the crank on the engine. This is how you're able to slow down the speed in the execution phase.

Another difference between the planning phase and the execution phase is that time in the execution phase is monotonic. That is, it flows in one direction only. You can slow down or stop time but you can't go backward in time. The planning phase, though is all about going forward and backward in time. Worse, you can jump about in an arbitrary fashion. This creates all sorts of headaches if you were planning on driving the physics engine directly in the planning phase. The engine doesn't appear to be designed with the idea that time can flow backward.

What I've chosen to do is use a sampling strategy to cache values of the iterative missile algorithm as I run the physics engine forward in time, from the start to the end of the turn. This needs to account for missiles that exist when the turn starts as well as those launched during the turn. I make a large lookup table for each sample time point and store the physical properties of each missile at each time point. There is an inherent inaccuracy with this method because I have to arbitrarily pick a sampling rate which may not match the actual frame rate during the execution phase of the turn.

If the target unit has its movement path changed, the missile has to be resampled to guarantee that it uses the updated coordinates for the targeted unit.


## Tinkering with the Timeline

There is no error correction the iterative algorithm. Errors can build up quickly and one of the most important sources of error is the initial starting position of the missile. The projected attack lines are drawn from the center point of the unit. However, this is not where projectiles of any type are fired from. It's good enough for ballistic projectiles but for missiles you really need the actual firing point. Here's an example showing why it matters.

![mech firing missile over shoulder of another mech](Media/over_the_shoulder.mp4)

If the center point of the mech with the missile launcher were used, the missile would blast mech on the left square in the back instead of shooting over its left shoulder. You can see the difference between where the projected firing line is drawn and the actual trajectory of the missile matters in a case like this.

The catch is that you can't get this firing point without running the mech animation routine. The animation routine is the most complicated piece of code in the whole game and it's written in a monolithic style. That means there's no easy way to lift it out or call into it from another routine. However, it is easily controllable indirectly by changing the prediction time.

The side effect is that the timeline jumps about and the projected units on the screen jerk about as I get the firing points for each missile launched during the turn. If either the launching unit or the target unit changes its movement path during the planning phase, the firing points have to be resampled as the launching unit may alter where it's pointing the launcher at each launch time.

## Hit Detection

Or, more appropriately, the lack of hit detection. I intentionally do not do hit detection with buildings, environment props or units other than the targeted one. These objects may not actually be there in the execution phase so I error on the side of least surprise. Here's an example showing the missiles going through a building in the planning phase.

![missiles sailing right through a building](Media/collision_planning.mp4)

In the execution phase, though, the missiles explode on contact with the building.

![missiles are destroyed instead in the execution phase](Media/collision_execution.mp4)

If the missiles have a large impact value, the leading missiles may blow a hole large enough in the building for the trailing missiles to pass through.

On the flip side, some missiles explode on proximity and even if your unit evades a missile, it may still incur damage from the blast. I'm still working on how to reliably detect and show both.

## Game Design

Missiles have the most knobs to tune of all the weapons in the game. They have knobs for speed, agility, lifetime, tracking fidelity, trajectory shaping, proximity detection, damage and blast radius to name a few. They are a modder's dream but a designer's nightmare. Balancing missiles so that they're not either overpowered or Nerf darts is very difficult.

I conjecture that part of the problem of balancing them comes down to the fact that they're invisible in the planning phase. There's no way to form an effective evasion plan so all you can do is hope your units get lucky and survive whatever hail of missiles the enemy has sent your way. You may as well chuck out your prediction gadget for all the good it does you.

Consequently, the designers have been a bit handcuffed with missiles. Unless you play a completely boring run-away style, the chances are pretty high all 10 missiles from an ML10 Starburst will hit your unit and it wouldn't be any fun if the enemy could one-shot your units without you being able to do anything about it. The designers balanced the damage so that a unit has a chance to survive if they run through a full volley.

I predict that this mod, if it works as I envision, will make missiles a bit underpowered. If player units can effectively evade most of the missiles in a volley, the damage per missile should be dialed up somewhat to compensate.

## Technical Notes

The code is a bit edgy. It's still in an exploratory form. Some areas I don't fully understand and I'm still researching. There are parts that I don't know how to do so I've been trying out various solutions. And then there are a boatload of glitches that need instrumentation and lots of logging to chase down.

I'm putting this up as a work in progress in the hopes that it inspires someone, either at BYG or in the modding community, to take a crack at this, especially from a designer/game-play perspective.
