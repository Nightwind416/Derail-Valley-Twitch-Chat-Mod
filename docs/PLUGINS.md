# Writing a panel for TwitchChat

TwitchChat puts world-space panels in the locomotive cab and on the back of a VR hand. They are the
only comfortable way to read anything at all in Derail Valley VR, and there is no reason they should
only ever show Twitch chat. This is how to put your own content on them.

A plugin is a small assembly that implements one interface. It does not reference `TwitchChat.dll`,
only `TwitchChat.Api.dll`, which ships beside it in the mod's folder.

## The shortest possible plugin

```csharp
using TwitchChat.Api;
using UnityEngine;

public class SpeedPlugin : ITwitchChatPlugin
{
    public string Id => "MyMod.Speed";   // stable: it is saved as "the panel this display shows"
    public string Title => "Speed";      // the title row, and the button in the Mods menu
    public bool IsAvailable => true;     // false to leave the panel out entirely

    public IPanelContent CreateContent(IPanelSurface surface) => new SpeedContent(surface);
}

public class SpeedContent : IPanelContent
{
    private readonly Text readout;
    private float next;

    public SpeedContent(IPanelSurface surface)
    {
        surface.Widgets.CreateSection(surface.Root, "Speed", 35, 40);
        readout = surface.Widgets.CreateLabel(surface.Root, "-- km/h", 15, 55);
    }

    public void OnShow() { }
    public void OnHide() { }
    public void OnResize(Vector2 size) { }

    public void Tick(float deltaTime)
    {
        // Four times a second is plenty. Tick runs every frame, and a panel is not worth a frame.
        next -= deltaTime;
        if (next > 0f) return;
        next = 0.25f;

        TrainCar? car = PlayerManager.Car;
        readout.text = car == null ? "not aboard" : $"{car.GetForwardSpeed() * 3.6f:F0} km/h";
    }
}
```

Build it against `TwitchChat.Api.dll` and drop the result in
`Derail Valley/Mods/TwitchChat/Plugins/`. It appears on the **Mods** panel, reachable from the Main
panel of every display.

## Getting registered

There are two routes and they end in the same place.

**Drop it in the Plugins folder.** TwitchChat scans `Mods/TwitchChat/Plugins/*.dll` during load and
instantiates every public, non-abstract `ITwitchChatPlugin` with a parameterless constructor. Nothing
else is needed, and the mod you are reporting on need not know you exist. This is how the panels
shipped with TwitchChat get in.

**Or register from your own mod.** If you already have a mod, reference `TwitchChat.Api.dll` and call
this from your `Load`:

```csharp
TwitchChatPlugins.Register(new SpeedPlugin());
```

Load order does not matter. TwitchChat picks up whatever registered before it started and is told
about anything that registers afterwards. If TwitchChat is not installed at all, the call is simply
never reached, because the assembly it lives in is never loaded — guard it with a check for the mod
if you would rather be explicit.

## Where to put things

`IPanelSurface` gives you two parents, matching how the mod's own panels are built:

- **`Root`** — the panel itself. Widgets here are positioned by hand, in canvas units measured down
  from the top, and stay put. The title row takes roughly the first 30 units. Use this for a header,
  a fixed set of rows, or a row of buttons.
- **`Content`** — the scrolling area. Children are laid out top to bottom and the area scrolls once
  they no longer fit. Use this for a list whose length you do not control.

Build with `surface.Widgets` rather than raw uGUI. You get the same fonts and spacing as the rest of
the mod, VR poke colliders on anything clickable, and your panel is recoloured along with everything
else when the player changes the colour settings.

## Rules worth knowing

**Everything is on Unity's main thread.** `CreateContent`, `Tick`, `OnShow`, `OnHide` and `OnResize`
are all called from the game loop. If you fetch something on a background thread, marshal the result
back yourself before touching a widget.

**But do not assume the mod you are reading wants to be called from there.** A method that looks
synchronous may hand its work to that mod's own main-thread pump and wait for the answer, because
the callers it was written for are worker threads — an HTTP handler, say. Call such a method from
`Tick` and the main thread waits for something only the main thread can do, which is a deadlock that
throws nothing, logs nothing, and freezes the whole game. This is not hypothetical: it is exactly
what the bundled Dispatch Map panel did before it was moved off the game loop.

If a mod's API is built for worker threads, read it on one and collect the answer a frame or two
later:

```csharp
private Task<Thing>? request;

public void Tick(float deltaTime)
{
    request ??= Task.Run(() => ReadTheOtherMod());   // never on this thread
    if (!request.IsCompleted) return;                // still out; last frame's picture still stands

    Thing thing = request.Result;
    request = null;
    ShowIt(thing);                                   // back on the main thread, safe for widgets
}
```

Keep only one request in flight, do no Unity work inside it, and let the result be plain data.
A read that throws out there is only a failed read; the same read on the game loop can be the end of
the session.

**Any collider you add must be a trigger.** A solid collider on a panel parented to a locomotive gets
folded into that locomotive's rigidbody, shifts its mass and shoves it around the track. The widget
factory already does the right thing; this matters only if you add colliders of your own.

**Panels have no size of their own.** A display is whatever size the player has dragged it to, and
your panel gets that size whichever it is. Anchor things rather than assuming a width, and use
`OnResize` if you draw something that has to be regenerated.

**You get one content object per display.** A player can have up to five displays in a locomotive
plus the wrist panel, so `CreateContent` may be called six times and all six results are alive at
once. Keep per-panel state in the content object, not in the plugin.

**Read other mods by reflection, not by reference.** `ModBinder` in the API assembly does the caching
and the swallowing of failures. Referencing another mod's assembly directly means your plugin fails
to load whenever that mod is absent, and ties you to its version:

```csharp
private readonly ModBinder ai = new("AITraffic");

public bool IsAvailable => ai.IsModPresent;

// ...
Type? manager = ai.Type("AITraffic.Core.TrafficManager");
object? instance = ai.Invoke(ai.Property(manager, "Instance")?.GetGetMethod(), null);
int trains = ai.ReadOr(instance, "ActiveTrainCount", 0);
```

`ModBinder` returns null rather than throwing, and stops for good after the first hard failure so a
mod that has changed shape is reported once instead of every frame.

**Failing is survivable.** If your content throws, TwitchChat stops driving it, writes the exception
to the mod's log, and puts a message on the panel. It does not take the display, the other panels, or
the frame down with it. This is a safety net, not a licence — catch what you can predict.

## Versioning

`TwitchChatPlugins.ApiVersion` and the assembly version of `TwitchChat.Api.dll` move together. A
plugin built against a different **major** version is refused at load with a line in the log saying
so. Additive changes bump the minor and existing plugins keep working.

## Settings and identity

`Id` is written into the player's settings as the panel a display was last showing, and it is the key
the on/off switch on the Mods panel uses. Changing it after release makes every display that was
showing your panel fall back to the Main menu, and forgets whether the player had switched it off.
Prefix it with something of your own — `MyMod.Speed` rather than `Speed` — so two plugins cannot
collide. The first plugin to claim an id keeps it; a second is refused with a line in the log.

Your own configuration is your own business: keep it in your mod's settings, not TwitchChat's.
