# Stamina
A game made on-stream, in one stream!

## Control Binds
| Bind  | Action |
| ---- | ---- |
| Menu  | Esc  |
| Move  | WASD  |
| Jump  | Space  |
| Respawn  | R  |

## Troubleshooting

### Graphics Driver Issue? (Game Looks Empty and Stuff Is Floating)
AMD's 25.8.1 driver has critical errors in it which causes it to fail to load some games, including Godot games. The below issue describes it. Interestingly, this is not the first time AMD has done this.
https://github.com/godotengine/godot/issues/109378

Don't worry though, because you can simply revert to a previous driver version and still run the game just fine.
Download a different driver here: <https://www.amd.com/en/support/download/drivers.html>

If you don't want to download a different driver version, you can alternatively launch the game with a different driver altogether. Here's how to do that on Windows:

- 1.) Open cmd (NOT POWERSHELL) by typing `cmd` into your search bar and clicking on it
- 2.) Right-click on the executable and select Properties
- 3.) Select *all* of the Location property (should be something like `C:\Users\scale\Desktop`)
- 4.) Type into the Command Prompt `cd`, space, and then the Location you just copied. Ex: `cd C:\Users\scale\Desktop`
- 5.) Enter the executable file, add a space, and add `--help`
- 6.) Look through the list that prints out until you find the `Run options` section. Then look for the `--display-driver <driver>` command. Then look beside it to the right for its description which will have a bunch of different `display driver` options. Copy the text of whichever one you want to try - I recommend `opengl3` if you have it (Vulkan is my first choice but that not working with AMD is why we're here).
- 7.) PRESS ENTER TO EXIT OUT OF THE HELP SECTION
- 8.) Type the executable name, space, and then the driver argument you chose. Ex: `godot.exe --rendering-driver opengl3` or `godot.exe --rendering-driver d3d12`. Be careful to type rendering driver here, not display driver. This will launch the game!
Note that it may take longer than usual to launch this way

### Sound Driver Issue? (Sound is Crackling)
We've had one person have a crackling issue when using 16-bit sound, and the problem went away when they switched to 24-bit sound. While everyone else who playtested with 16-bit did not have this issue, if you experience it you can change your bitrate this way:

- 1.) Right-click on the Windows sound icon in the system tray
- 2.) Select Sound settings
- 3.) Open the Sound Control Panel...
- 1. If Windows 11: Scroll down to the Advanced section and select More sound settings
- 2. If Windows 10: Click on Sound Control Panel
- 4.) Double-click on your sound device from the Playback tab
- 5.) Go to the Advanced tab
- 6.) Under Default Format, open the sample rate drop-down and choose `24-bit, 48000 Hz (Studio Quality)`

## Licence
    Stamina
    A game made on-stream, in one stream!
    Copyright (C) 2025 ScaleShift

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.